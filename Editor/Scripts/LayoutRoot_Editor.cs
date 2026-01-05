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
[CustomEditor (typeof (LayoutRoot))]
public class LayoutRoot_Editor : Editor
{
    private LayoutRoot _layoutRoot;
    private SerializedProperty _log;

    protected virtual void OnEnable ()
    {
        _layoutRoot = target as LayoutRoot;
        _log = serializedObject.FindProperty ("m_log");  
    }

    public override void OnInspectorGUI ()
    {
        if (_layoutRoot == null)
            return;
        
        EditorGUILayout.PropertyField (_log);
            
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties ();
                _layoutRoot.SetDirty ();
            }

            if (_log.boolValue)
            {
                EditorGUILayout.HelpBox (_layoutRoot.report, MessageType.None);
            }
        }
    }
}