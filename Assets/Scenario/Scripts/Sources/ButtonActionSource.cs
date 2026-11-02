using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder
{
    /// Действие = нажатие UI-кнопки на этом объекте.
    [RequireComponent(typeof(Button))]
    public class ButtonActionSource : ActionSource
    {
        void Awake() => GetComponent<Button>().onClick.AddListener(Raise);
    }
}
