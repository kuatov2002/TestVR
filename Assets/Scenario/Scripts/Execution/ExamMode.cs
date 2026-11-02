using UnityEngine;

namespace ScenarioBuilder
{
    /// Экзамен: нарушенный шаг проваливается без повтора; при достижении порога нарушений
    /// в группе её незавершённые шаги пропускаются.
    public class ExamMode : ScenarioMode
    {
        [Tooltip("Сколько нарушений в группе завершают её досрочно")]
        [Min(1)] public int violationThreshold = 2;

        protected override void OnViolation(Step step, string reason, StepGroup group)
        {
            SetResult(step, StepStatus.Failed);
            if (ViolationsIn(group) < violationThreshold)
            {
                Runner.Message($"{step.title}: {reason}. Шаг провален.");
                return;
            }
            SkipRestOfGroup(group);
            Runner.Message($"{step.title}: {reason}. Шаг провален. Порог нарушений ({violationThreshold}) в группе «{group.name}» достигнут — остальные шаги группы пропущены.");
        }
    }
}
