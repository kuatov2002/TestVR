namespace ScenarioBuilder
{
    /// Тренировка: при нарушении — сообщение и повтор того же шага, группы не пропускаются.
    public class TrainingMode : ScenarioMode
    {
        protected override void OnViolation(Step step, string reason, StepGroup group)
        {
            step.ResetProgress();
            Runner.Message($"{step.title}: {reason}. Повторите шаг.");
        }
    }
}
