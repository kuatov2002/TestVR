using UnityEngine;

namespace ScenarioBuilder
{
    /// Пишет каждую запись журнала в консоль Unity.
    public class ConsoleLogger : ScenarioLogger
    {
        public override void Write(LogEntry entry) => Debug.Log("[Scenario] " + entry);
    }
}
