using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    public class AnalysisResult
    {
        public float score, maxScore;
        public List<string> lines = new();
        public List<string> warnings = new();

        public override string ToString() =>
            $"Итог: {score} из {maxScore}\n" + string.Join("\n", lines) +
            (warnings.Count > 0 ? "\nПредупреждения:\n" + string.Join("\n", warnings.Select(w => "• " + w)) : "");
    }

    /// Оценка прохождения только по журналу — без доступа к сцене и исполнению,
    /// поэтому любой анализатор можно применить к любому завершённому прохождению.
    /// Новый анализатор = наследник, добавленный на объект сценария.
    public abstract class ResultAnalyzer : MonoBehaviour
    {
        public string title;

        public abstract AnalysisResult Analyze(IReadOnlyList<LogEntry> journal);

        /// Шаги в порядке сценария (поле order), без него — в порядке активации, затем неактивированные.
        protected static List<string> StepIds(IReadOnlyList<LogEntry> journal)
        {
            var order = journal.FirstOrDefault(e => e.type == LogTypes.ScenarioStarted)?.Get("order");
            var mentioned = journal.Where(e => !string.IsNullOrEmpty(e.stepId))
                .OrderBy(e => e.type == LogTypes.StepActivated ? 0 : 1) // OrderBy стабилен — внутри групп сохраняется порядок журнала
                .Select(e => e.stepId);
            return (string.IsNullOrEmpty(order) ? mentioned : order.Split(',').Concat(mentioned)).Distinct().ToList();
        }

        /// Предупреждения о неполном журнале, общие для всех анализаторов.
        protected static void CheckJournal(IReadOnlyList<LogEntry> journal, AnalysisResult result)
        {
            if (journal.Count == 0) result.warnings.Add("Журнал пуст — оценивать нечего.");
            else if (!journal.Any(e => e.type == LogTypes.ScenarioFinished))
                result.warnings.Add("Прохождение не завершено — оценка по неполному журналу.");
        }

        /// Общее число шагов сценария; если в журнале его нет — число упомянутых шагов и предупреждение.
        protected static int StepCount(IReadOnlyList<LogEntry> journal, AnalysisResult result)
        {
            var started = journal.FirstOrDefault(e => e.type == LogTypes.ScenarioStarted);
            if (started != null && int.TryParse(started.Get("steps"), out var n)) return n;
            result.warnings.Add("В журнале нет общего числа шагов — учитываются только упомянутые в нём шаги.");
            return StepIds(journal).Count;
        }

        protected static string LastResult(IReadOnlyList<LogEntry> journal, string stepId) =>
            journal.LastOrDefault(e => e.stepId == stepId && e.type == LogTypes.StepFinished)?.result;

        protected static int Violations(IReadOnlyList<LogEntry> journal, string stepId) =>
            journal.Count(e => e.stepId == stepId && e.type == LogTypes.Violation);

        [ContextMenu("Оценить сохранённый журнал (JsonFileLogger)")]
        void AnalyzeSavedLog() => Debug.Log($"{title}\n{Analyze(JsonFileLogger.Load())}");
    }
}
