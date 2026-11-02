namespace ScenarioBuilder
{
    /// Тренировка: при нарушении — сообщение и повтор того же шага, группы не пропускаются.
    public class TrainingMode : ScenarioMode
    {
        protected override void OnViolation(Step step, string reason, StepGroup group)
        {
            step.ResetProgress();
            Runner.Message(StatusOf(step) == StepStatus.Active
                ? $"{step.title}: {reason}. Повторите шаг."
                : $"{step.title}: {reason} — засчитано как неудачная попытка этого шага.");
        }
    }
}
