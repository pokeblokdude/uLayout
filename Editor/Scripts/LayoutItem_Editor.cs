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
        CustomEditor(typeof(LayoutItem)),
        CanEditMultipleObjects
    ]
    public class LayoutItem_Editor : Editor
    {
        private SerializedProperty _ignoreLayout;
        private SerializedProperty _sizing;

        protected virtual void Awake() {
            _ignoreLayout = serializedObject.FindProperty("m_ignoreLayout");
            _sizing = serializedObject.FindProperty("m_sizing");
        }

        public override void OnInspectorGUI() {
            EditorGUILayout.PropertyField(_ignoreLayout);
            
            // disable sizing options if ignoreLayout is true
            GUI.enabled = !_ignoreLayout.boolValue;
            EditorGUILayout.PropertyField(_sizing);
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}