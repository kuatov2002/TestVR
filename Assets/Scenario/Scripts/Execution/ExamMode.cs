using UnityEngine;

namespace ScenarioBuilder
{
    /// Экзамен: нарушенный шаг проваливается без повтора; при достижении порога нарушений
    /// в группе её незавершённые шаги пропускаются и начинается следующая группа.
    public class ExamMode : ScenarioMode
    {
        [Tooltip("Сколько нарушений в группе завершают её досрочно")]
        [Min(1)] public int violationThreshold = 2;

        protected override void OnViolation(Step step, string reason)
        {
            SetResult(step, StepStatus.Failed);
            Runner.Message($"{step.title}: {reason}. Шаг провален.");
            if (GroupViolations < violationThreshold) return;
            Runner.Message($"Порог нарушений ({violationThreshold}) в группе «{CurrentGroup.name}» достигнут — оставшиеся шаги пропущены.");
            SkipRestOfGroup();
        }
    }
}
