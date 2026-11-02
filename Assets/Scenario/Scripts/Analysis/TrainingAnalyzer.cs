using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// Баллы за шаг зависят от попытки, с которой он выполнен (по умолчанию 10 / 5 / 0).
    /// Попытка = нарушение по шагу + финальное успешное выполнение.
    public class TrainingAnalyzer : ResultAnalyzer
    {
        [Tooltip("Баллы за выполнение с 1-й, 2-й, … попытки. Дальше последней — 0. Первое значение — максимум за шаг.")]
        public int[] pointsByAttempt = { 10, 5 };

        public override AnalysisResult Analyze(IReadOnlyList<LogEntry> journal)
        {
            var r = new AnalysisResult();
            int max = pointsByAttempt.Length > 0 ? pointsByAttempt[0] : 0;
            r.maxScore = max * StepCount(journal, r);
            foreach (var id in StepIds(journal))
            {
                int violations = Violations(journal, id);
                bool done = LastResult(journal, id) == nameof(StepStatus.Completed);
                int points = done && violations < pointsByAttempt.Length ? pointsByAttempt[violations] : 0;
                r.score += points;
                r.lines.Add($"{id}: {(done ? $"попыток {violations + 1}" : "не выполнен")}, нарушений {violations} → {points}");
            }
            return r;
        }
    }
}
