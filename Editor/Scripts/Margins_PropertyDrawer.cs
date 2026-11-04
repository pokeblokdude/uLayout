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
#if UNITY_6000_0_OR_NEWER
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Poke.UI.Editor
{
    [CustomPropertyDrawer(typeof(Margins))]
    public class Margins_PropertyDrawer : PropertyDrawer
    {
        public VisualTreeAsset margins;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement root = margins.CloneTree();

            SerializedProperty t = property.FindPropertyRelative("top");
            SerializedProperty l = property.FindPropertyRelative("left");
            SerializedProperty r = property.FindPropertyRelative("right");
            SerializedProperty b = property.FindPropertyRelative("bottom");

            Label label = root.Q<Label>("Label");
            label.text = preferredLabel;
            
            FloatField top = root.Q<FloatField>("TopField");
            top.BindProperty(t);
            
            FloatField left = root.Q<FloatField>("LeftField");
            left.BindProperty(l);
            
            FloatField right = root.Q<FloatField>("RightField");
            right.BindProperty(r);
            
            FloatField bottom = root.Q<FloatField>("BottomField");
            bottom.BindProperty(b);
            
            return root;
        }
    }
}
#endif