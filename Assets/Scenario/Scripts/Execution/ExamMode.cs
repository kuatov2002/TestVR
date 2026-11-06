using UnityEngine;

namespace ScenarioBuilder
{
    /// Экзамен: нарушенный шаг проваливается без повтора; при достижении порога нарушений
    /// в группе её незавершённые шаги пропускаются, а повторная ошибка на проваленном шаге проваливает группу.
    public class ExamMode : ScenarioMode
    {
        [Tooltip("Сколько нарушений в группе завершают её досрочно")]
        [Min(1)] public int violationThreshold = 2;
        [Tooltip("Повторная ошибка на уже проваленном шаге проваливает всю группу: её незавершённые шаги получают Failed")]
        public bool failGroupOnRepeat = true;

        protected override void OnViolation(Step step, string reason, StepGroup group)
        {
            SetResult(step, StepStatus.Failed);
            if (ViolationsIn(group) < violationThreshold)
            {
                Runner.Message($"{step.title}: {reason}. Шаг провален.");
                return;
            }
            FinishRestOfGroup(group, StepStatus.Skipped);
            Runner.Message($"{step.title}: {reason}. Шаг провален. Порог нарушений ({violationThreshold}) в группе «{group.name}» достигнут — остальные шаги группы пропущены.");
        }

        protected override void OnRepeatViolation(Step step, string reason, StepGroup group)
        {
            if (failGroupOnRepeat && FinishRestOfGroup(group, StepStatus.Failed))
                Runner.Message($"{step.title}: {reason}. Повторная ошибка на проваленном шаге — группа «{group.name}» провалена.");
            else
                Runner.Message($"{step.title}: {reason}. Шаг уже провален.");
        }
    }
}
