using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// Шаг из одного или нескольких действий, выполняемых строго по порядку,
    /// с запрещёнными действиями и необязательным условием на состояние другого объекта.
    public class ActionStep : Step
    {
        [Tooltip("Действия, которые нужно выполнить по порядку. Одно действие — простой шаг.")]
        public List<ActionSource> actions = new();
        public string orderMessage = "Нарушен порядок действий";

        [Tooltip("Действия, которые считаются нарушением, пока шаг активен")]
        public List<ActionSource> forbidden = new();
        public string forbiddenMessage = "Запрещённое действие";

        [Header("Условие: состояние другого объекта")]
        [Tooltip("Если задан — действия шага засчитываются, только когда объект в нужном состоянии")]
        public GameObject stateObject;
        public bool stateObjectActive = true;
        public string stateMessage = "Неподходящее состояние объекта";

        int progress;

        public override void ResetProgress() => progress = 0;

        public override bool Concerns(ActionSource action) => actions.Contains(action);

        public override StepVerdict Handle(ActionSource action, out string reason)
        {
            reason = null;
            if (forbidden.Contains(action))
            {
                reason = forbiddenMessage;
                return StepVerdict.Violation;
            }
            if (progress < actions.Count && actions[progress] == action)
            {
                if (stateObject && stateObject.activeInHierarchy != stateObjectActive)
                {
                    reason = stateMessage;
                    return StepVerdict.Violation;
                }
                return ++progress == actions.Count ? StepVerdict.Completed : StepVerdict.Accepted;
            }
            // повторное срабатывание последнего выполненного действия (например, снова вошли в тот же триггер)
            if (progress > 0 && actions[progress - 1] == action) return StepVerdict.Accepted;
            if (!actions.Contains(action)) return StepVerdict.Ignored;
            reason = orderMessage;
            return StepVerdict.Violation;
        }
    }
}
