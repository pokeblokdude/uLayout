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
using TMPro;

namespace Poke.UI
{
    public static class LayoutEditorUtil
    {
        [MenuItem ("GameObject/UI/Layout/Layout #&w", false, 9)]
        public static void CreateLayoutObject (MenuCommand command)
        {
            GameObject parent = Selection.activeGameObject;
            var  canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogWarning ($"No parent Canvas, can't create Layout widget");
                return;
            }

            var root = parent.GetComponentInParent<LayoutRoot> ();
            if (root == null)
            {
                Debug.LogWarning ($"No parent LayoutRoot, can't create Layout widget");
                return;
            }
            
            GameObject g = new GameObject("Layout");  
            GameObjectUtility.SetParentAndAlign(g, parent);
            
            var layout = g.AddComponent<Layout> ();
            
            Undo.RegisterCreatedObjectUndo(g, "Create Layout");
            Selection.activeGameObject = g;
        }

        [MenuItem ("GameObject/UI/Layout/Layout Text #&l", false, 10)]
        public static void CreateLayoutTextObject (MenuCommand command)
        {
            GameObject parent = Selection.activeGameObject;
            var  canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogWarning ($"No parent Canvas, can't create LayoutText widget");
                return;
            }

            var root = parent.GetComponentInParent<LayoutRoot> ();
            if (root == null)
            {
                Debug.LogWarning ($"No parent LayoutRoot, can't create LayoutText widget");
                return;
            }

            GameObject g = new GameObject("Label", typeof(RectTransform));
            TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI> ();
            t.text = "New Text";
            t.alignment = TextAlignmentOptions.Capline;
            g.AddComponent<LayoutText> ();
            
            Undo.RegisterCreatedObjectUndo(g, "Create Label");

            GameObjectUtility.SetParentAndAlign(g, parent);
            Selection.activeGameObject = g;
        }

        [MenuItem ("GameObject/UI/Layout/Layout Item", false, 11)]
        public static void CreateLayoutItemObject (MenuCommand command)
        {
            GameObject parent = Selection.activeGameObject;
            var  canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogWarning ($"No parent Canvas, can't create LayoutItem widget");
                return;
            }

            var root = parent.GetComponentInParent<LayoutRoot> ();
            if (root == null)
            {
                Debug.LogWarning ($"No parent LayoutRoot, can't create LayoutItem widget");
                return;
            }
            
            GameObject g = new GameObject("LayoutItem");  
            GameObjectUtility.SetParentAndAlign(g, parent);
            
            var layout = g.AddComponent<LayoutItem> ();
            
            Undo.RegisterCreatedObjectUndo(g, "Create LayoutItem");
            Selection.activeGameObject = g;
        }

        [MenuItem ("GameObject/UI/Layout/Layout Root", false, 12)]
        public static void CreateLayoutRootObject (MenuCommand command)
        {
            GameObject g = new GameObject ("LayoutRoot");
            GameObjectUtility.SetParentAndAlign (g, command.context as GameObject);

            g.AddComponent<RectTransform> ();
            g.AddComponent<LayoutRoot> ();

            Undo.RegisterCreatedObjectUndo (g, "Create " + g.name);
            Selection.activeObject = g;
        }
    }
}