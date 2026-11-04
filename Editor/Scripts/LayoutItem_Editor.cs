/*
    Copyright (c) 2026 Alex Howe

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
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poke.UI.Editor
{
    [
        CustomEditor(typeof(LayoutItem)),
        CanEditMultipleObjects
    ]
    public class LayoutItem_Editor : UnityEditor.Editor
    {
        private LayoutItem _item;

        private SerializedProperty _log;
        private SerializedProperty _ignoreLayout;
        protected SerializedProperty _sizing;
        private SerializedProperty _sizingX;
        private SerializedProperty _sizingY;
        private SerializedProperty _useMinWidth;
        private SerializedProperty _useMinHeight;
        private SerializedProperty _useMaxWidth;
        private SerializedProperty _useMaxHeight;
        private SerializedProperty _minWidth;
        private SerializedProperty _minHeight;
        private SerializedProperty _maxWidth;
        private SerializedProperty _maxHeight;
        private SerializedProperty _flexWidth;
        private SerializedProperty _flexHieght;
        private SerializedProperty _margins;

        protected virtual void OnEnable() {
            _item = target as LayoutItem;

            _log = serializedObject.FindProperty("m_log");
            _ignoreLayout = serializedObject.FindProperty("m_ignoreLayout");
            _sizing = serializedObject.FindProperty("m_sizing");
            _sizingX = _sizing.FindPropertyRelative("x");
            _sizingY = _sizing.FindPropertyRelative("y");
            _useMinWidth = serializedObject.FindProperty("m_useMinWidth");
            _useMinHeight = serializedObject.FindProperty("m_useMinHeight");
            _useMaxWidth = serializedObject.FindProperty("m_useMaxWidth");
            _useMaxHeight = serializedObject.FindProperty("m_useMaxHeight");
            _minWidth = serializedObject.FindProperty("m_minWidth");
            _minHeight = serializedObject.FindProperty("m_minHeight");
            _maxWidth = serializedObject.FindProperty("m_maxWidth");
            _maxHeight = serializedObject.FindProperty("m_maxHeight");
            _flexWidth = serializedObject.FindProperty("m_flexWidth");
            _flexHieght = serializedObject.FindProperty("m_flexHeight");
            _margins = serializedObject.FindProperty("m_margins");
        }
        
#if UNITY_6000_0_OR_NEWER
        public VisualTreeAsset layoutItem;
        
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new();
            root.Add(layoutItem.CloneTree());
            
            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, OnObjectChanged);

            Toggle log = root.Q<Toggle>("LogField");
            log.BindProperty(_log);
            
            Toggle ignoreLayout = root.Q<Toggle>("IgnoreLayoutField");
            ignoreLayout.BindProperty(_ignoreLayout);

            EnumField sizingX = root.Q<EnumField>("SizeModeX");
            sizingX.BindProperty(_sizingX);
            
            EnumField sizingY = root.Q<EnumField>("SizeModeY");
            sizingY.BindProperty(_sizingY);

            Label minLabel = root.Q<Label>("MinLabel");
            minLabel.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed || (SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed);
            minLabel.TrackPropertyValue(_sizing, prop => {
                minLabel.SetEnabled(
                    (SizingMode)prop.FindPropertyRelative("x").enumValueIndex != SizingMode.Fixed ||
                    (SizingMode)prop.FindPropertyRelative("y").enumValueIndex != SizingMode.Fixed
                );
            });

            Toggle useMinWidth = root.Q<Toggle>("MinWidthToggle");
            useMinWidth.BindProperty(_useMinWidth);
            useMinWidth.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed);
            useMinWidth.TrackPropertyValue(_sizingX, prop => {
                useMinWidth.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed);
            });
            
            Toggle useMinHeight = root.Q<Toggle>("MinHeightToggle");
            useMinHeight.BindProperty(_useMinHeight);
            useMinHeight.SetEnabled((SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed);
            useMinHeight.TrackPropertyValue(_sizingY, prop => {
                useMinHeight.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed);
            });
            
            Toggle useMaxWidth = root.Q<Toggle>("MaxWidthToggle");
            useMaxWidth.BindProperty(_useMaxWidth);
            useMaxWidth.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed);
            useMaxWidth.TrackPropertyValue(_sizingX, prop => {
                useMaxWidth.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed);
            });
            
            Toggle useMaxHeight = root.Q<Toggle>("MaxHeightToggle");
            useMaxHeight.BindProperty(_useMaxHeight);
            useMaxHeight.SetEnabled((SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed);
            useMaxHeight.TrackPropertyValue(_sizingY, prop => {
                useMaxHeight.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed);
            });
            
            FloatField minWidth = root.Q<FloatField>("MinWidthField");
            minWidth.BindProperty(_minWidth);
            minWidth.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed && _useMinWidth.boolValue);
            minWidth.TrackPropertyValue(_sizingX, prop => minWidth.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed && _useMinWidth.boolValue));
            minWidth.TrackPropertyValue(_useMinWidth, prop => minWidth.SetEnabled(prop.boolValue && (SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed));
            
            FloatField minHeight = root.Q<FloatField>("MinHeightField");
            minHeight.BindProperty(_minHeight);
            minHeight.SetEnabled((SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed && _useMinHeight.boolValue);
            minHeight.TrackPropertyValue(_sizingY, prop => minHeight.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed && _useMinHeight.boolValue));
            minHeight.TrackPropertyValue(_useMinHeight, prop => minHeight.SetEnabled(prop.boolValue && (SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed));
            
            Label maxLabel = root.Q<Label>("MaxLabel");
            maxLabel.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed || (SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed);
            maxLabel.TrackPropertyValue(_sizing, prop => {
                maxLabel.SetEnabled(
                    (SizingMode)prop.FindPropertyRelative("x").enumValueIndex != SizingMode.Fixed ||
                    (SizingMode)prop.FindPropertyRelative("y").enumValueIndex != SizingMode.Fixed
                );
            });
            
            FloatField maxWidth = root.Q<FloatField>("MaxWidthField");
            maxWidth.BindProperty(_maxWidth);
            maxWidth.SetEnabled((SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed && _useMaxWidth.boolValue);
            maxWidth.TrackPropertyValue(_sizingX, prop => maxWidth.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed && _useMaxWidth.boolValue));
            maxWidth.TrackPropertyValue(_useMaxWidth, prop => maxWidth.SetEnabled(prop.boolValue && (SizingMode)_sizingX.enumValueIndex != SizingMode.Fixed));
            
            FloatField maxHeight = root.Q<FloatField>("MaxHeightField");
            maxHeight.BindProperty(_maxHeight);
            maxHeight.SetEnabled((SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed && _useMaxHeight.boolValue);
            maxHeight.TrackPropertyValue(_sizingY, prop => maxHeight.SetEnabled((SizingMode)prop.enumValueIndex != SizingMode.Fixed && _useMaxHeight.boolValue));
            maxHeight.TrackPropertyValue(_useMaxHeight, prop => maxHeight.SetEnabled(prop.boolValue && (SizingMode)_sizingY.enumValueIndex != SizingMode.Fixed));
            
            Label flexLabel = root.Q<Label>("FlexLabel");
            flexLabel.SetEnabled((SizingMode)_sizingX.enumValueIndex == SizingMode.Grow || (SizingMode)_sizingY.enumValueIndex == SizingMode.Grow);
            flexLabel.TrackPropertyValue(_sizing, prop => {
                flexLabel.SetEnabled(
                    (SizingMode)prop.FindPropertyRelative("x").enumValueIndex == SizingMode.Grow ||
                    (SizingMode)prop.FindPropertyRelative("y").enumValueIndex == SizingMode.Grow);
            });
            
            FloatField flexWidth = root.Q<FloatField>("FlexWidthField");
            flexWidth.BindProperty(_flexWidth);
            flexWidth.SetEnabled((SizingMode)_sizingX.enumValueIndex == SizingMode.Grow);
            flexWidth.TrackPropertyValue(_sizingX, prop => flexWidth.SetEnabled((SizingMode)prop.enumValueIndex == SizingMode.Grow));
            
            FloatField flexHeight = root.Q<FloatField>("FlexHeightField");
            flexHeight.BindProperty(_flexHieght);
            flexHeight.SetEnabled((SizingMode)_sizingY.enumValueIndex == SizingMode.Grow);
            flexHeight.TrackPropertyValue(_sizingY, prop => flexHeight.SetEnabled((SizingMode)prop.enumValueIndex == SizingMode.Grow));

            PropertyField margins = root.Q<PropertyField>("MarginsField");
            margins.BindProperty(_margins);
            
            return root;
        }
        
        private void OnObjectChanged(SerializedObject obj) {
            if(EditorUtility.IsDirty(obj.targetObject)) {
                foreach(Object item in obj.targetObjects) {
                    (item as LayoutItem).SetDirty();
                }
            }
        }
#else
        public override void OnInspectorGUI() {
            if(!_item)
                return;

            EditorGUILayout.PropertyField(_log);
            EditorGUILayout.PropertyField(_ignoreLayout);

            // disable sizing options if ignoreLayout is true
            GUI.enabled = !_ignoreLayout.boolValue;
            EditorGUILayout.PropertyField(_sizing);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(_useMinWidth);
            EditorGUILayout.PropertyField(_minWidth);
            EditorGUILayout.PropertyField(_useMaxWidth);
            EditorGUILayout.PropertyField(_maxWidth);
            EditorGUILayout.PropertyField(_useMinHeight);
            EditorGUILayout.PropertyField(_minHeight);
            EditorGUILayout.PropertyField(_useMaxHeight);
            EditorGUILayout.PropertyField(_maxHeight);
            EditorGUILayout.PropertyField(_flexWidth);
            EditorGUILayout.PropertyField(_flexHieght);
            EditorGUILayout.PropertyField(_margins);

            if(serializedObject.hasModifiedProperties) {
                serializedObject.ApplyModifiedProperties();

                foreach(var obj in serializedObject.targetObjects) {
                    (obj as LayoutItem).SetDirty();
                }

                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
#endif
    }
}