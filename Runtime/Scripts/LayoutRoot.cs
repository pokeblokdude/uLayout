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
        private readonly SortedBucket<Layout, int, Layout> _layouts = new (l => l, l => l.GetInstanceID());
        private readonly Stack<Layout> _reverse = new ();
        private bool _dirty;

        private void Start() {
            UpdateLayout();
        }

        private void SetDirty() {
            _dirty = true;
        }
        
        public void LateUpdate() {
            if(_dirty) {
                UpdateLayout();
            }
            else {
                Debug.Log("[Root]: No layout changes");
            }
        }

        public void UpdateLayout() {
            _reverse.Clear();
                
            // fit sizing pass (0)
            Debug.Log($"[Root]: Fit Size Pass ({Time.unscaledTime:f5})");
            foreach(Layout l in _layouts) {
                if(l.NeedsRefresh) {
                    l.ComputeFitSize();
                    _reverse.Push(l);
                }
            }

            // grow sizing pass (1)
            Debug.Log($"[Root]: Grow Size Pass ({Time.unscaledTime:f5})");
            foreach(Layout l in _layouts) {
                if(l.NeedsRefresh) {
                    l.GrowChildren();
                }
            }
                
            // layout pass (2)
            Debug.Log($"[Root]: Layout Pass ({Time.unscaledTime:f5})");
            foreach(Layout l in _reverse) {
                l.ComputeLayout();
                Debug.Log(l.name);
            }
            
            Debug.Log($"[Root]: Refreshed {_reverse.Count} layouts");
            
            _dirty = false;
        }

        public void RegisterLayout(Layout layout) {
            //Debug.Log($"Registered \"{layout.name}\" at depth [{layout.Depth}]");
            layout.OnLayoutChanged += SetDirty;
            _layouts.Add(layout);
        }

        public void UnregisterLayout(Layout layout) {
            if(_layouts.Remove(layout)) {
                layout.OnLayoutChanged -= SetDirty;
                //Debug.Log($"Removed \"{layout.name}\"");
            }
            else {
                Debug.LogError($"Failed to remove \"{layout.name}\" (not found)");
            }
        }
    }
}