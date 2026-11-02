using UnityEngine;

namespace ScenarioBuilder
{
    /// Действие = столкновение с объектом, у которого нужный тег.
    [RequireComponent(typeof(Collider))]
    public class CollisionActionSource : ActionSource
    {
        [Tooltip("Реагировать только на объекты с этим тегом. Пусто — на любые.")]
        public string requiredTag = "Player";

        void OnCollisionEnter(Collision collision)
        {
            if (string.IsNullOrEmpty(requiredTag) || collision.gameObject.CompareTag(requiredTag)) Raise();
        }
    }
}
