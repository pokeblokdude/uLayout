using UnityEditor;
using UnityEngine;

namespace Poke.UI
{
    [CustomEditor(typeof(LayoutItem))]
    public class LayoutItem_Editor : Editor
    {
        private SerializedProperty _ignoreLayout;
        private SerializedProperty _sizing;
        private SerializedProperty _offset;
        private SerializedProperty _rotation;
        private SerializedProperty _scale;

        protected virtual void Awake() {
            _ignoreLayout = serializedObject.FindProperty("m_ignoreLayout");
            _sizing = serializedObject.FindProperty("m_sizing");
            _offset = serializedObject.FindProperty("m_offset");
            _rotation = serializedObject.FindProperty("m_rotation");
            _scale = serializedObject.FindProperty("m_scale");
        }

        public override void OnInspectorGUI() {
            EditorGUILayout.PropertyField(_ignoreLayout);
            
            // disable sizing options if ignoreLayout is true
            GUI.enabled = !_ignoreLayout.boolValue;
            EditorGUILayout.PropertyField(_sizing);
            EditorGUILayout.PropertyField(_offset);
            EditorGUILayout.PropertyField(_rotation);
            EditorGUILayout.PropertyField(_scale);
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}