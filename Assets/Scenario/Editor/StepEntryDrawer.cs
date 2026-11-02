using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.EditorTools
{
    /// Элемент списка шагов в одну строку: [шаг] [☐ вместе с предыдущим] — удобно перетаскивать.
    [CustomPropertyDrawer(typeof(StepEntry))]
    public class StepEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float toggleWidth = 170;
            var withPrevious = property.FindPropertyRelative(nameof(StepEntry.withPrevious));
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - toggleWidth - 4, rect.height),
                property.FindPropertyRelative(nameof(StepEntry.step)), GUIContent.none);
            withPrevious.boolValue = EditorGUI.ToggleLeft(new Rect(rect.xMax - toggleWidth, rect.y, toggleWidth, rect.height),
                "вместе с предыдущим", withPrevious.boolValue);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }
}
