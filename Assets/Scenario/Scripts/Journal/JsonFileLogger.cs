using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScenarioBuilder
{
    /// Пишет журнал в файл JSON Lines (одна запись — одна строка), чтобы его можно было
    /// заново оценить после выхода из Play Mode (контекстное меню анализатора).
    public class JsonFileLogger : ScenarioLogger
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath, "scenario_log.jsonl");

        public override void Begin() => File.WriteAllText(FilePath, "");

        public override void Write(LogEntry entry) => File.AppendAllText(FilePath, JsonUtility.ToJson(entry) + "\n");

        public static List<LogEntry> Load()
        {
            var entries = new List<LogEntry>();
            if (!File.Exists(FilePath))
            {
                Debug.LogWarning($"Журнал не найден: {FilePath}");
                return entries;
            }
            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { entries.Add(JsonUtility.FromJson<LogEntry>(line)); }
                catch (System.Exception e) { Debug.LogWarning($"Пропущена повреждённая запись журнала: {e.Message}"); }
            }
            return entries;
        }
    }
}
