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

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Poke.UI
{
    [ExecuteAlways, RequireComponent (typeof (RectTransform))]
    public class LayoutRoot : MonoBehaviour
    {
        [SerializeField] 
        private bool m_log = false;
        
        [NonSerialized]
        public StringBuilder sb = new StringBuilder ();
        
        [NonSerialized]
        public string report = string.Empty;
        
        private readonly SortedBucket<Layout, int, Layout> _layouts = new(l => l, l => l.GetInstanceID ());
        private readonly Stack<Layout> _reverse = new();
        private bool _dirty;

        private void Start ()
        {
            UpdateLayout ();
        }

        public void SetDirty ()
        {
            _dirty = true;
        }

        public void LateUpdate ()
        {
            if (_dirty)
            {
                UpdateLayout ();
            }
        }

        public void UpdateLayout ()
        {
            _reverse.Clear ();
            int layoutsUpdated = 0;

            // fit sizing pass (0)
            // Debug.Log($"[Root]: Fit Size Pass ({Time.unscaledTime:f5})");
            foreach (Layout l in _layouts)
            {
                if (l.NeedsRefresh)
                {
                    l.ComputeFitSize ();
                    _reverse.Push (l);
                    layoutsUpdated += 1;
                }
            }

            // grow sizing pass (1)
            //Debug.Log($"[Root]: Grow Size Pass ({Time.unscaledTime:f5})");
            foreach (Layout l in _layouts)
            {
                if (l.NeedsRefresh)
                {
                    l.GrowChildren ();
                }
            }

            // layout pass (2)
            //Debug.Log($"[Root]: Layout Pass ({Time.unscaledTime:f5})");
            foreach (Layout l in _reverse)
            {
                l.ComputeLayout ();
            }

            //Debug.Log($"[Root]: Refreshed {_reverse.Count} layouts");
            _dirty = false;

            if (m_log)
            {
                sb.Clear ();
                sb.Append ("Layouts: ");
                sb.Append (_layouts.Count);
                sb.Append ("\nUpdated last: ");
                sb.Append (layoutsUpdated);
                report = sb.ToString ();
                
                if (m_log)
                    Debug.Log ($"LayoutRoot | Updated layout:\n{report}");
            }
        }

        public void RegisterLayout (Layout layout)
        {
            if (m_log)
                Debug.Log ($"LayoutRoot | Registered \"{layout.name}\" at depth [{layout.Depth}]");
            
            layout.OnLayoutChanged += SetDirty;
            _layouts.Add (layout);
        }

        public void UnregisterLayout (Layout layout)
        {
            if (_layouts.Remove (layout))
            {
                layout.OnLayoutChanged -= SetDirty;
                if (m_log)
                    Debug.Log ($"LayoutRoot {name} | Removed \"{layout.name}\"");
            }
            else
            {
                Debug.LogError ($"Failed to remove \"{layout.name}\" (not found)");
            }
        }
    }
}