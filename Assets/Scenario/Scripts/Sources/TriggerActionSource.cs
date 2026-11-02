using UnityEngine;

namespace ScenarioBuilder
{
    /// Действие = объект с нужным тегом вошёл в триггер.
    [RequireComponent(typeof(Collider))]
    public class TriggerActionSource : ActionSource
    {
        [Tooltip("Реагировать только на объекты с этим тегом. Пусто — на любые.")]
        public string requiredTag = "Player";

        void OnTriggerEnter(Collider other)
        {
            if (string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag)) Raise();
        }
    }
}
