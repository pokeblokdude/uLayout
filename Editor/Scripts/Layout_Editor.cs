/*
    Copyright (c) 2025 Alex Howe

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
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