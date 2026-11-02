using UnityEngine;

namespace ScenarioBuilder
{
    public enum StepVerdict { Ignored, Accepted, Completed, Violation }

    /// Переиспользуемый шаг сценария. Знает только свою логику выполнения;
    /// когда он активен и что делать с нарушением — решает режим (ScenarioMode).
    /// Новый тип шага = наследник с собственными Concerns/Handle.
    public abstract class Step : MonoBehaviour
    {
        [Tooltip("Идентификатор в журнале. Пусто — имя объекта.")]
        [SerializeField] string id;

        [Tooltip("Текст задания, который видит пользователь")]
        public string title;

        public string Id => string.IsNullOrEmpty(id) ? name : id;

        /// Сброс промежуточного прогресса: при активации и после нарушения.
        public virtual void ResetProgress() { }

        /// Относится ли действие к выполнению шага (нужно для нарушений «вне очереди»).
        public abstract bool Concerns(ActionSource action);

        /// Реакция активного шага на действие; reason заполняется при Violation.
        public abstract StepVerdict Handle(ActionSource action, out string reason);
    }
}
