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
        [Tooltip("Порядок активации: шаги с галочкой «вместе с предыдущим» открываются одной пачкой; следующая пачка — после завершения текущей")]
        public List<StepEntry> steps = new();
    }

    /// Режим прохождения: раскладка шагов по группам/пачкам и правила реакции на нарушения.
    /// Группы идут последовательно, внутри группы — пачки одновременно активных шагов.
    /// Новый режим = наследник с собственным OnViolation.
    public abstract class ScenarioMode : MonoBehaviour
    {
        public string title;
        public List<StepGroup> groups = new();
        [Tooltip("Что делать с действием шага, который ещё не активен")]
        public OutOfTurnPolicy outOfTurn = OutOfTurnPolicy.Violation;
        [Tooltip("Анализатор, которым оценивается прохождение в этом режиме")]
        public ResultAnalyzer analyzer;

        protected ScenarioRunner Runner { get; private set; }
        protected int GroupViolations { get; private set; }

        readonly Dictionary<Step, StepStatus> status = new();
        int group, batchStart, batchEnd;

        public bool IsRunning { get; private set; }
        public int CurrentGroupIndex => group;
        public StepGroup CurrentGroup => group >= 0 && group < groups.Count ? groups[group] : null;
        public StepStatus StatusOf(Step step) => status.TryGetValue(step, out var s) ? s : StepStatus.Pending;

        public void Begin(ScenarioRunner runner)
        {
            Runner = runner;
            status.Clear();
            foreach (var e in groups.SelectMany(g => g.steps).Where(e => e.step)) status[e.step] = StepStatus.Pending;
            IsRunning = true;
            group = -1;
            Runner.Log(LogTypes.ScenarioStarted, null, title, new LogField("steps", status.Count));
            NextGroup();
        }

        public void Handle(ActionSource action)
        {
            if (!IsRunning) return;
            bool concerned = false;
            foreach (var step in CurrentBatch().ToList())
            {
                if (StatusOf(step) != StepStatus.Active) continue; // могли пропустить из-за порога
                switch (step.Handle(action, out var reason))
                {
                    case StepVerdict.Accepted: concerned = true; break;
                    case StepVerdict.Completed: concerned = true; SetResult(step, StepStatus.Completed); break;
                    case StepVerdict.Violation: concerned = true; Violate(step, reason); break;
                }
            }
            if (!concerned && outOfTurn == OutOfTurnPolicy.Violation)
                foreach (var step in status.Keys.Where(s => status[s] == StepStatus.Pending && s.Concerns(action)).ToList())
                    if (IsRunning && StatusOf(step) == StepStatus.Pending) Violate(step, "Шаг ещё не доступен");

            if (IsRunning && !CurrentBatch().Any(s => StatusOf(s) == StepStatus.Active)) NextBatch();
        }

        /// Реакция режима на нарушение (запись Violation уже сделана).
        protected abstract void OnViolation(Step step, string reason);

        protected void SetResult(Step step, StepStatus result)
        {
            status[step] = result;
            Runner.Log(LogTypes.StepFinished, step, result.ToString(), new LogField("group", CurrentGroup?.name));
        }

        /// Пропустить незавершённые шаги текущей группы и перейти к следующей.
        protected void SkipRestOfGroup()
        {
            foreach (var e in CurrentGroup.steps)
                if (e.step && StatusOf(e.step) is StepStatus.Pending or StepStatus.Active) SetResult(e.step, StepStatus.Skipped);
            NextGroup();
        }

        void Violate(Step step, string reason)
        {
            GroupViolations++;
            Runner.Log(LogTypes.Violation, step, reason);
            OnViolation(step, reason);
        }

        IEnumerable<Step> CurrentBatch()
        {
            if (CurrentGroup == null) yield break;
            for (int i = batchStart; i < batchEnd; i++)
                if (CurrentGroup.steps[i].step) yield return CurrentGroup.steps[i].step;
        }

        void NextGroup()
        {
            if (++group >= groups.Count)
            {
                IsRunning = false;
                Runner.Log(LogTypes.ScenarioFinished, null, title);
                Runner.NotifyFinished();
                return;
            }
            GroupViolations = 0;
            batchEnd = 0;
            Runner.Log(LogTypes.GroupStarted, null, CurrentGroup.name);
            NextBatch();
        }

        void NextBatch()
        {
            var entries = CurrentGroup.steps;
            do
            {
                if (batchEnd >= entries.Count) { NextGroup(); return; }
                batchStart = batchEnd++;
                while (batchEnd < entries.Count && entries[batchEnd].withPrevious) batchEnd++;
                foreach (var step in CurrentBatch().Where(s => StatusOf(s) == StepStatus.Pending))
                {
                    status[step] = StepStatus.Active;
                    step.ResetProgress();
                    Runner.Log(LogTypes.StepActivated, step);
                }
            } while (!CurrentBatch().Any(s => StatusOf(s) == StepStatus.Active)); // вся пачка уже провалена вне очереди
        }
    }
}
