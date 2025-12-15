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
using UnityEngine;

namespace Poke.UI
{
    public static class LayoutUtil
    {
        public static void DrawCenteredDebugBox(Vector3 pos, float w, float h, Color color) {
            DrawDebugBox(pos - new Vector3(w/2, h/2, 0), w, h, color);
        }

        /// <summary>
        /// Draws a box out of debug rays, positioned at the bottom left corner.
        /// </summary>
        public static void DrawDebugBox(Vector3 pos, float w, float h, Color color) {
            Gizmos.color = color;
            // left
            Gizmos.DrawLine(pos, pos + Vector3.up * h);
            // bottom
            Gizmos.DrawLine(pos, pos + Vector3.right * w);
            // right
            Gizmos.DrawLine(pos + new Vector3(w, h), pos + new Vector3(w, h) + Vector3.down * h);
            // top
            Gizmos.DrawLine(pos + new Vector3(w, h), pos + new Vector3(w, h) + Vector3.left * w);
        }

        public static void DrawDebugBox(Rect rect, float z, Color color) {
            DrawDebugBox((Vector3)rect.position + new Vector3(0, 0, z), rect.width, rect.height, color);
        }
        
        public static Vector2 With(this Vector2 vec, float? x = null, float? y = null) {
            return new Vector2(x ?? vec.x, y ?? vec.y);
        }
    }
    
    [System.Serializable]
    public struct Margins
    {
        public float top, bottom, left, right;
    }
    
    public enum SizingMode
    {
        FitContent,
        Fixed,
        Grow,
    }
}
