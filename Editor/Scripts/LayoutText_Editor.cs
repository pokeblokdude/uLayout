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
    [CustomEditor (typeof (LayoutText)), CanEditMultipleObjects]
    public class LayoutText_Editor : LayoutItem_Editor
    {
        private LayoutText _layoutText;
        private SerializedProperty _maxFontSize;
        private SerializedProperty _log;
        
        protected virtual void OnEnable ()
        {
            base.OnEnable ();
            _layoutText = target as LayoutText;
            
            _maxFontSize = serializedObject.FindProperty ("m_maxFontSize");
            _log = serializedObject.FindProperty ("m_log");
        }

        public override void OnInspectorGUI ()
        {
            if (_layoutText == null)
                return;
            
            EditorGUILayout.PropertyField (_log);
            base.OnInspectorGUI ();
            
            // disable sizing options if ignoreLayout is true
            GUI.enabled = !_ignoreLayout.boolValue;
            EditorGUILayout.PropertyField (_maxFontSize);
            GUI.enabled = true;

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties ();
                
                var layoutAbove = _layoutText.GetComponentInParent<Layout> ();
                if (layoutAbove != null)
                    layoutAbove.SetDirty ();
            }
        }
    }
}