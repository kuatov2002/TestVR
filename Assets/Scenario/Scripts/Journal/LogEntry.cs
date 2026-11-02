using System;
using System.Collections.Generic;

namespace ScenarioBuilder
{
    /// Минимальный контракт записи журнала. Тип — строка, а не enum: новые типы записей
    /// и дополнительные поля (data) не ломают анализаторы, которые их не знают.
    [Serializable]
    public class LogEntry
    {
        public float time;
        public string stepId;
        public string type;
        public string result;
        public List<LogField> data = new();

        public string Get(string key) => data?.Find(f => f.key == key).value;

        public override string ToString()
        {
            var extra = data == null || data.Count == 0 ? "" : " {" + string.Join(", ", data.ConvertAll(f => $"{f.key}={f.value}")) + "}";
            return $"[{time,7:0.00}] {type,-16} {stepId,-4} {result}{extra}";
        }
    }

    [Serializable]
    public struct LogField
    {
        public string key;
        public string value;

        public LogField(string key, object value)
        {
            this.key = key;
            this.value = value?.ToString();
        }
    }

    /// Типы записей, которые пишет базовое исполнение. Свои типы можно добавлять где угодно.
    public static class LogTypes
    {
        public const string ScenarioStarted = "ScenarioStarted"; // result = режим, data: steps, startedAt
        public const string GroupStarted = "GroupStarted";       // result = группа
        public const string Action = "Action";                   // result = название действия
        public const string StepActivated = "StepActivated";
        public const string Violation = "Violation";             // result = причина, data: group, attempt (номер неудачной попытки)
        public const string StepFinished = "StepFinished";       // result = Completed | Failed | Skipped, data: group, violations
        public const string ScenarioFinished = "ScenarioFinished";
    }
}
