/*
    Copyright (C) 2025  Alex Howe

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using UnityEditor;
using UnityEngine;

namespace Poke.UI
{
    [
        CustomEditor(typeof(Layout)),
        CanEditMultipleObjects
    ]
    public class Layout_Editor : LayoutItem_Editor
    {
        private Layout _layout;
        private SerializedProperty _padding;
        private SerializedProperty _direction;
        private SerializedProperty _justifyContent;
        private SerializedProperty _alignContent;
        private SerializedProperty _innerSpacing;
        
        protected override void Awake() {
            base.Awake();
            _layout = target as Layout;

            _padding = serializedObject.FindProperty("m_padding");
            _direction = serializedObject.FindProperty("m_direction");
            _justifyContent = serializedObject.FindProperty("m_justifyContent");
            _alignContent = serializedObject.FindProperty("m_alignContent");
            _innerSpacing = serializedObject.FindProperty("m_innerSpacing");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_justifyContent);
            EditorGUILayout.PropertyField(_alignContent);
            EditorGUILayout.PropertyField(_innerSpacing);
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Tracking {_layout.ChildCount} layout elements." + (_layout.GrowChildCount > 0 ? $"\n({_layout.GrowChildCount} grow)" : ""),
                MessageType.Info
            );
            if(GUILayout.Button("Refresh Child Cache")) {
                _layout.RefreshChildCache();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}