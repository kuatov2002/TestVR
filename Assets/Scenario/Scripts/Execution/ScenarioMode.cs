using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    public enum StepStatus { Pending, Active, Completed, Failed, Skipped }

    public enum OutOfTurnPolicy
    {
        [InspectorName("Нарушение")] Violation,
        [InspectorName("Игнорировать (шаг неактивен)")] Ignore,
    }

    [Serializable]
    public class StepEntry
    {
        public Step step;
        [Tooltip("Активировать одновременно с предыдущим шагом списка")]
        public bool withPrevious;
    }

    [Serializable]
    public class StepGroup
    {
        public string name;
        [Tooltip("Начать одновременно с предыдущей группой (иначе — после её завершения)")]
        public bool withPrevious;
        [Tooltip("Порядок активации: шаги с галочкой «вместе с предыдущим» открываются одной пачкой; следующая пачка — после завершения текущей")]
        public List<StepEntry> steps = new();
    }

    /// Режим прохождения: раскладка шагов по группам/пачкам и правила реакции на нарушения.
    /// Группы идут по очереди (или пачкой, если отмечены «вместе с предыдущей»),
    /// внутри группы — пачки одновременно активных шагов.
    /// Новый режим = наследник с собственным OnViolation.
    public abstract class ScenarioMode : MonoBehaviour
    {
        public string title;
        public List<StepGroup> groups = new();
        [Tooltip("Что делать с действием шага, который ещё не активен")]
        public OutOfTurnPolicy outOfTurn = OutOfTurnPolicy.Violation;
        [Tooltip("Анализатор, которым оценивается прохождение в этом режиме")]
        public ResultAnalyzer analyzer;
        [Tooltip("Действие, после которого прохождение заканчивается досрочно (например, выход из цеха). Незавершённые шаги получают Skipped. Пусто — не используется.")]
        public ActionSource finishAction;

        protected ScenarioRunner Runner { get; private set; }

        class GroupRun
        {
            public StepGroup group;
            public int batchStart, batchEnd;
        }

        readonly Dictionary<Step, StepStatus> status = new();
        readonly Dictionary<Step, StepGroup> groupOf = new();
        readonly Dictionary<StepGroup, int> violations = new();
        readonly Dictionary<Step, int> stepViolations = new();
        readonly List<GroupRun> running = new();
        int nextGroup;

        public bool IsRunning { get; private set; }
        public bool IsGroupRunning(StepGroup group) => running.Any(r => r.group == group);
        public StepStatus StatusOf(Step step) => status.TryGetValue(step, out var s) ? s : StepStatus.Pending;
        protected StepGroup GroupOf(Step step) => groupOf[step];
        protected int ViolationsIn(StepGroup group) => violations.TryGetValue(group, out var n) ? n : 0;

        public void Begin(ScenarioRunner runner)
        {
            Runner = runner;
            status.Clear();
            groupOf.Clear();
            violations.Clear();
            stepViolations.Clear();
            running.Clear();
            foreach (var g in groups)
                foreach (var e in g.steps.Where(e => e.step))
                {
                    status[e.step] = StepStatus.Pending;
                    groupOf[e.step] = g;
                }
            IsRunning = true;
            nextGroup = 0;
            Runner.Log(LogTypes.ScenarioStarted, null, title,
                new LogField("steps", status.Count), new LogField("startedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            Advance();
        }

        public void Handle(ActionSource action)
        {
            if (!IsRunning) return;
            bool concerned = false;
            foreach (var step in running.SelectMany(Batch).ToList())
            {
                if (StatusOf(step) != StepStatus.Active) continue; // могли пропустить из-за порога
                switch (step.Handle(action, out var reason))
                {
                    case StepVerdict.Accepted: concerned = true; break;
                    case StepVerdict.Completed: concerned = true; SetResult(step, StepStatus.Completed); break;
                    case StepVerdict.Violation: concerned = true; Violate(step, reason); break;
                }
            }
            if (finishAction && action == finishAction) FinishEarly();
            else if (!concerned && outOfTurn == OutOfTurnPolicy.Violation)
                foreach (var step in status.Keys.Where(s => status[s] == StepStatus.Pending && s.Concerns(action)).ToList())
                    if (StatusOf(step) == StepStatus.Pending) Violate(step, "Шаг ещё не доступен");
            Advance();
        }

        /// Реакция режима на нарушение (запись Violation уже сделана). group — группа, которой засчитано нарушение.
        protected abstract void OnViolation(Step step, string reason, StepGroup group);

        protected void SetResult(Step step, StepStatus result)
        {
            status[step] = result;
            Runner.Log(LogTypes.StepFinished, step, result.ToString(),
                new LogField("group", groupOf[step].name), new LogField("violations", stepViolations.GetValueOrDefault(step)));
            if (result == StepStatus.Completed) Runner.Message($"Выполнено: {step.title}", false);
        }

        /// Пропустить незавершённые шаги группы.
        protected void SkipRestOfGroup(StepGroup group)
        {
            foreach (var e in group.steps)
                if (e.step && StatusOf(e.step) is StepStatus.Pending or StepStatus.Active) SetResult(e.step, StepStatus.Skipped);
        }

        /// Нарушение засчитывается группе, которая идёт сейчас: своей, если группа шага уже идёт,
        /// иначе первой из идущих — будущая группа не страдает до своего начала.
        void Violate(Step step, string reason)
        {
            var group = IsGroupRunning(groupOf[step]) || running.Count == 0 ? groupOf[step] : running[0].group;
            violations[group] = ViolationsIn(group) + 1;
            stepViolations[step] = stepViolations.GetValueOrDefault(step) + 1;
            Runner.Log(LogTypes.Violation, step, reason,
                new LogField("group", group.name), new LogField("attempt", stepViolations[step]));
            OnViolation(step, reason, group);
        }

        void FinishEarly()
        {
            var unfinished = status.Keys.Where(s => status[s] is StepStatus.Pending or StepStatus.Active).ToList();
            if (unfinished.Count == 0) return; // этим же действием выполнен последний шаг — обычное завершение
            foreach (var step in unfinished) SetResult(step, StepStatus.Skipped);
            running.Clear();
            nextGroup = groups.Count;
            Runner.Message($"{title}: прохождение завершено досрочно, незавершённые шаги пропущены.");
        }

        IEnumerable<Step> Batch(GroupRun run)
        {
            for (int i = run.batchStart; i < run.batchEnd; i++)
                if (run.group.steps[i].step) yield return run.group.steps[i].step;
        }

        /// Открывает следующие пачки и группы, пока есть что выполнять; иначе завершает сценарий.
        void Advance()
        {
            while (IsRunning)
            {
                foreach (var run in running.ToList())
                    if (!Batch(run).Any(s => StatusOf(s) == StepStatus.Active)) NextBatch(run);
                if (running.Count > 0) return;
                if (nextGroup >= groups.Count)
                {
                    IsRunning = false;
                    Runner.Log(LogTypes.ScenarioFinished, null, title);
                    Runner.NotifyFinished();
                    return;
                }
                do
                {
                    var group = groups[nextGroup++];
                    Runner.Log(LogTypes.GroupStarted, null, group.name);
                    running.Add(new GroupRun { group = group });
                } while (nextGroup < groups.Count && groups[nextGroup].withPrevious);
            }
        }

        void NextBatch(GroupRun run)
        {
            var entries = run.group.steps;
            do
            {
                if (run.batchEnd >= entries.Count)
                {
                    running.Remove(run);
                    return;
                }
                run.batchStart = run.batchEnd++;
                while (run.batchEnd < entries.Count && entries[run.batchEnd].withPrevious) run.batchEnd++;
                foreach (var step in Batch(run).Where(s => StatusOf(s) == StepStatus.Pending))
                {
                    status[step] = StepStatus.Active;
                    step.ResetProgress();
                    Runner.Log(LogTypes.StepActivated, step);
                }
            } while (!Batch(run).Any(s => StatusOf(s) == StepStatus.Active)); // вся пачка уже провалена или пропущена
        }
    }
}
