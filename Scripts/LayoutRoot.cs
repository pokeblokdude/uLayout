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
using System.Collections.Generic;
using UnityEngine;

namespace Poke.UI
{
    [
        ExecuteAlways,
        RequireComponent(typeof(RectTransform))
    ]
    public class LayoutRoot : MonoBehaviour
    {
        [SerializeField] private int m_tickRate = 60;
        
        private readonly SortedBucket<Layout, int, Layout> _layouts = new (l => l, l => l.GetInstanceID());
        private readonly Stack<Layout> _reverse = new ();
        private float _tickInterval;
        private float _lastTickTimestamp;
        private bool _tick;
        
        private void Awake() {
            _tickInterval = 1.0f / m_tickRate;
        }

        private void Start() {
            _tick = true;
            LateUpdate();
        }

        public void Update() {
            if(Time.time - _lastTickTimestamp >= _tickInterval) {
                _tick = true;
            }
        }

        public void LateUpdate() {
            if(_tick) {
                _reverse.Clear();
                
                // fit sizing pass (0)
                Debug.Log("[Root] Fit Size Pass");
                foreach(Layout l in _layouts) {
                    l.ComputeFitSize();
                    _reverse.Push(l);
                }

                // grow sizing pass (1)
                Debug.Log("[Root] Grow Size Pass");
                foreach(Layout l in _reverse) {
                    l.ComputeGrowSize();
                }
                
                // layout pass (2)
                Debug.Log("[Root] Layout Pass");
                foreach(Layout l in _reverse) {
                    l.ComputeLayout();
                }

                _lastTickTimestamp = Time.time;
                _tick = false;
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