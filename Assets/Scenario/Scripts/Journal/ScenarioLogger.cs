using UnityEngine;

namespace ScenarioBuilder
{
    /// Приёмник записей журнала. Новый логгер = наследник, добавленный в список loggers у ScenarioRunner.
    public abstract class ScenarioLogger : MonoBehaviour
    {
        /// Начало нового прохождения.
        public virtual void Begin() { }

        public abstract void Write(LogEntry entry);
    }
}
