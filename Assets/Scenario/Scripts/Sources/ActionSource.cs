using System;
using UnityEngine;

namespace ScenarioBuilder
{
    /// Источник действия пользователя. Raise() можно вызвать из любого UnityEvent в инспекторе
    /// (Button.onClick, Toggle.onValueChanged, собственные события) — это и есть способ подключить свой источник.
    public class ActionSource : MonoBehaviour
    {
        public static event Action<ActionSource> Fired;

        [Tooltip("Название действия в журнале. Пусто — имя объекта.")]
        [SerializeField] string actionName;

        public string ActionName => string.IsNullOrEmpty(actionName) ? name : actionName;

        public void Raise() => Fired?.Invoke(this);
    }
}
