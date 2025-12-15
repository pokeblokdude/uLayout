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
            if(Time.unscaledTime - _lastTickTimestamp >= _tickInterval) {
                _tick = true;
            }
        }

        public void LateUpdate() {
            if(_tick) {
                _reverse.Clear();
                
                // fit sizing pass (0)
                //Debug.Log("[Root] Fit Size Pass");
                foreach(Layout l in _layouts) {
                    l.ComputeFitSize();
                    _reverse.Push(l);
                }

                // grow sizing pass (1)
                //Debug.Log("[Root] Grow Size Pass");
                foreach(Layout l in _layouts) {
                    l.GrowChildren();
                }
                
                // layout pass (2)
                //Debug.Log("[Root] Layout Pass");
                foreach(Layout l in _reverse) {
                    l.ComputeLayout();
                }

                _lastTickTimestamp = Time.unscaledTime;
                _tick = false;
            }
        }

        public void ForceUpdate() {
            LateUpdate();
        }

        public void RegisterLayout(Layout layout) {
            //Debug.Log($"Registered \"{layout.name}\" at depth [{layout.Depth}]");
            _layouts.Add(layout);
        }

        public void UnregisterLayout(Layout layout) {
            if(_layouts.Remove(layout)) {
                //Debug.Log($"Removed \"{layout.name}\"");
            }
            else {
                Debug.LogError($"Failed to remove \"{layout.name}\" (not found)");
            }
        }
    }
}