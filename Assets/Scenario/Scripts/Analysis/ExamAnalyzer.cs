using System.Collections.Generic;
using System.Linq;

namespace ScenarioBuilder
{
    /// 100 − штрафы: шаг с нарушением или Failed — 10, пропущенный (Skipped) — 5.
    /// На журнале тренировки шаг с нарушением тоже считается проваленным — как было бы на экзамене.
    public class ExamAnalyzer : ResultAnalyzer
    {
        public int failedPenalty = 10;
        public int skippedPenalty = 5;

        public override AnalysisResult Analyze(IReadOnlyList<LogEntry> journal)
        {
            var r = new AnalysisResult { maxScore = 100 };
            var ids = StepIds(journal);
            int penalty = 0;
            var byGroup = new Dictionary<string, int>();
            bool noGroups = false;

            foreach (var id in ids)
            {
                var last = LastResult(journal, id);
                bool failed = Violations(journal, id) > 0 || last == nameof(StepStatus.Failed);
                int p = failed ? failedPenalty : last == nameof(StepStatus.Completed) ? 0 : skippedPenalty;
                string state = failed ? nameof(StepStatus.Failed) : last ?? "нет итога";
                if (!failed && last == null) r.warnings.Add($"{id}: нет итогового статуса — засчитан как пропущенный.");
                penalty += p;
                r.lines.Add($"{id}: {state} → −{p}");

                var group = journal.LastOrDefault(e => e.stepId == id && e.type == LogTypes.StepFinished)?.Get("group");
                if (group == null) noGroups = true;
                else byGroup[group] = (byGroup.TryGetValue(group, out var g) ? g : 0) + p;
            }

            int missing = StepCount(journal, r) - ids.Count;
            if (missing > 0)
            {
                r.warnings.Add($"{missing} шаг(ов) не встречаются в журнале — засчитаны как пропущенные.");
                penalty += missing * skippedPenalty;
            }
            if (noGroups) r.warnings.Add("Не у всех шагов указана группа — разбивка по группам неполная.");
            r.lines.AddRange(byGroup.Select(g => $"Группа «{g.Key}»: штраф {g.Value}"));
            r.score = System.Math.Max(0, 100 - penalty);
            return r;
        }
    }
}
