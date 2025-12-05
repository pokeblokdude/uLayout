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

using System;
using UnityEngine;
using Random = UnityEngine.Random;

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
