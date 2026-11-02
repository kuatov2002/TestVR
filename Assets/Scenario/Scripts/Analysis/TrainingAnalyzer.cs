using System.Collections.Generic;

namespace ScenarioBuilder
{
    /// 10 баллов за шаг с первой попытки, 5 — со второй, 0 — с третьей и далее или если не выполнен.
    /// Попытка = нарушение по шагу + финальное успешное выполнение.
    public class TrainingAnalyzer : ResultAnalyzer
    {
        public override AnalysisResult Analyze(IReadOnlyList<LogEntry> journal)
        {
            var r = new AnalysisResult();
            r.maxScore = 10 * StepCount(journal, r);
            foreach (var id in StepIds(journal))
            {
                int violations = Violations(journal, id);
                bool done = LastResult(journal, id) == nameof(StepStatus.Completed);
                int points = !done ? 0 : violations == 0 ? 10 : violations == 1 ? 5 : 0;
                r.score += points;
                r.lines.Add($"{id}: {(done ? $"попыток {violations + 1}" : "не выполнен")}, нарушений {violations} → {points}");
            }
            return r;
        }
    }
}
