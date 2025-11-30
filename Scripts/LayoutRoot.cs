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
using UnityEngine;

namespace Poke.UI
{
    [
        ExecuteAlways,
        RequireComponent(typeof(RectTransform))
    ]
    public class LayoutRoot : MonoBehaviour
    {
        private readonly SortedBucket<Layout, int, Layout> _layouts = new (l => l, l => l.GetInstanceID());

        private void Start() {
            LateUpdate();
        }

        public void LateUpdate() {
            // sizing pass (0)
            foreach(Layout l in _layouts) {
                l.ComputeSize();
            }

            // layout pass (1)
            foreach(Layout l in _layouts) {
                l.ComputeLayout();
            }
        }

        public void ForceUpdate() {
            LateUpdate();
        }

        public void RegisterLayout(Layout layout) {
            Debug.Log($"Registered \"{layout.name}\" at depth [{layout.Depth}]");
            _layouts.Add(layout);
        }

        public void UnregisterLayout(Layout layout) {
            if(_layouts.Remove(layout)) {
                Debug.Log($"Removed \"{layout.name}\"");
            }
            else {
                Debug.LogError($"Failed to remove \"{layout.name}\" (not found)");
            }
        }
    }
}