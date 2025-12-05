using UnityEditor;
using UnityEngine;

namespace Poke.UI
{
    [CustomPropertyDrawer(typeof(RangeInt))]
    public class RangeInt_PropertyDrawer : PropertyDrawer
    {
        private readonly float labelWidth = 32;
        private readonly float gap = 24;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty min = property.FindPropertyRelative("min");
            SerializedProperty max = property.FindPropertyRelative("max");

            Rect r = GUILayoutUtility.GetLastRect();
            GUI.Label(
                new Rect(r.position.x, r.position.y, r.width * 0.35f, r.height),
                label
            );
            
            float offset = r.width * 0.35f + 16;
            float leftover = r.width - offset;
            float dist = (leftover - gap) / 2;
            
            GUI.Label(
                new Rect(r.position.x + offset, r.position.y, labelWidth, r.height),
                "Min"
            );

            offset += labelWidth;

            min.intValue = EditorGUI.IntField(
                new Rect(r.position.x + offset, r.position.y, dist/2, r.height),
                min.intValue
            );

            offset += dist/2 + gap;

            GUI.Label(
                new Rect(r.position.x + offset, r.position.y, labelWidth, r.height),
                "Max"
            );

            offset += 32;

            max.intValue = EditorGUI.IntField(
                new Rect(r.position.x + offset, r.position.y, dist/2, r.height),
                max.intValue
            );
        }
    }
}
