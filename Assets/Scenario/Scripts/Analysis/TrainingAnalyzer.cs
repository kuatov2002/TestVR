using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    /// Баллы за шаг зависят от попытки, с которой он выполнен (по умолчанию 10 / 5 / 0).
    /// Попытка = нарушение по шагу + финальное успешное выполнение.
    public class TrainingAnalyzer : ResultAnalyzer
    {
        [Tooltip("Баллы за выполнение с 1-й, 2-й, … попытки. Дальше последней — 0. Наибольшее значение — максимум за шаг.")]
        public int[] pointsByAttempt = { 10, 5 };

        public override AnalysisResult Analyze(IReadOnlyList<LogEntry> journal)
        {
            var r = new AnalysisResult();
            CheckJournal(journal, r);
            r.maxScore = pointsByAttempt.DefaultIfEmpty(0).Max() * StepCount(journal, r);
            foreach (var id in StepIds(journal))
            {
                int violations = Violations(journal, id);
                var last = LastResult(journal, id);
                bool done = last == nameof(StepStatus.Completed);
                int points = done && violations < pointsByAttempt.Length ? pointsByAttempt[violations] : 0;
                r.score += points;
                r.lines.Add($"{id}: {(done ? $"попыток {violations + 1}" : $"не выполнен ({last ?? "нет итога"})")}, нарушений {violations} → {points}");
            }
            return r;
        }
    }
}
