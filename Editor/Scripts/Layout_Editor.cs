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
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Poke.UI.Editor
{
    [
        CustomEditor(typeof(Layout)),
        CanEditMultipleObjects
    ]
    public class Layout_Editor : LayoutItem_Editor
    {
        public VisualTreeAsset layout;
        
        private Layout _layout;
        private SerializedProperty _padding;
        private SerializedProperty _direction;
        private SerializedProperty _justifyContent;
        private SerializedProperty _alignContent;
        private SerializedProperty _innerSpacing;
        private SerializedProperty _wrap;
        private SerializedProperty _lineSpacing;
        private SerializedProperty _alignItems;

        protected override void OnEnable() {
            base.OnEnable();
            _layout = target as Layout;

            _padding = serializedObject.FindProperty("m_padding");
            _direction = serializedObject.FindProperty("m_direction");
            _justifyContent = serializedObject.FindProperty("m_justifyContent");
            _alignContent = serializedObject.FindProperty("m_alignContent");
            _innerSpacing = serializedObject.FindProperty("m_innerSpacing");
            _wrap = serializedObject.FindProperty("m_wrap");
            _lineSpacing = serializedObject.FindProperty("m_lineSpacing");
            _alignItems = serializedObject.FindProperty("m_alignItems");
        }
        
#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = base.CreateInspectorGUI();
            root.Add(layout.CloneTree());
            
            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, OnObjectChanged);
            
            root.Q<PropertyField>("PaddingField").BindProperty(_padding);
            root.Q<EnumField>("DirectionField").BindProperty(_direction);
            root.Q<EnumField>("JustifyField").BindProperty(_justifyContent);
            root.Q<EnumField>("AlignField").BindProperty(_alignContent);

            FloatField innerSpacing = root.Q<FloatField>("InnerSpacingField");
            innerSpacing.BindProperty(_innerSpacing);
            innerSpacing.SetEnabled((Layout.Justification)_justifyContent.enumValueIndex != Layout.Justification.SpaceBetween);
            innerSpacing.TrackPropertyValue(_justifyContent, prop => {
                innerSpacing.SetEnabled((Layout.Justification)prop.enumValueIndex != Layout.Justification.SpaceBetween);
            });
            
            root.Q<Toggle>("WrapField").BindProperty(_wrap);

            FloatField lineSpacing = root.Q<FloatField>("LineSpacingField");
            lineSpacing.BindProperty(_lineSpacing);
            lineSpacing.SetEnabled(_wrap.boolValue);
            lineSpacing.TrackPropertyValue(_wrap, prop => lineSpacing.SetEnabled(prop.boolValue));

            root.Q<EnumField>("AlignItemsField").BindProperty(_alignItems);
            
            Label info = root.Q<Label>("InfoLabel");
            info.text = $"Tracking {_layout.ChildCount} layout elements.\nHorizontal Grow: {_layout.GrowChildCount.x}, Vertical Grow: {_layout.GrowChildCount.y}";
            info.TrackSerializedObjectValue(serializedObject, obj => {
                Layout l = obj.targetObject as Layout;
                info.text = $"Tracking {l.ChildCount} layout elements.\nHorizontal Grow: {l.GrowChildCount.x}, Vertical Grow: {l.GrowChildCount.y}";
            });
            
            return root;
        }

        private void OnObjectChanged(SerializedObject obj) {
            if(EditorUtility.IsDirty(obj.targetObject)) {
                foreach(Object layout in obj.targetObjects) {
                    (layout as Layout).SetDirty();
                }
            }
        }
#else
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if(!_layout)
                return;

            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_justifyContent);
            EditorGUILayout.PropertyField(_alignContent);

            if((Layout.Justification)_justifyContent.enumValueFlag == Layout.Justification.SpaceBetween) {
                GUI.enabled = false;
            }
            EditorGUILayout.PropertyField(_innerSpacing);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(_ignoreChildScale);
            
            EditorGUILayout.PropertyField(_wrap);
            if(_wrap.boolValue) {
                EditorGUILayout.PropertyField(_lineSpacing);
            }

            EditorGUILayout.PropertyField(_alignItems);

            if(serializedObject.hasModifiedProperties) {
                serializedObject.ApplyModifiedProperties();
                foreach (var obj in serializedObject.targetObjects) {
                    (obj as Layout).SetDirty();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Tracking {_layout.ChildCount} layout elements.\nHorizontal Grow: {_layout.GrowChildCount.x}, Vertical Grow: {_layout.GrowChildCount.y}",
                MessageType.Info
            );
            if(GUILayout.Button("Refresh Child Cache")) {
                _layout.RefreshChildCache();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
#endif
    }
}