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
    public class Layout : LayoutItem, IComparable<Layout>
    {
        /* THINGS THAT CAN CAUSE A LAYOUT UPDATE
            - non-grow child RectTransform changes size
            - number of children change
            - child is enabled/disabled
            - this container changes
        */
        public event Action OnLayoutChanged;
        
        [SerializeField] private Margins m_padding;

        [SerializeField] private LayoutDirection m_direction;
        [SerializeField] private Justification m_justifyContent;
        [SerializeField] private Alignment m_alignContent;
        [SerializeField] private float m_innerSpacing;

        public int ChildCount => _children.Count;
        public int Depth => _depth;
        public int GrowChildCount => _growChildren != null ? _growChildren.Count : 0;
        public LayoutDirection Direction => m_direction;
        public bool NeedsRefresh => _dirty;

        private readonly int MAX_DEPTH = 100;

        private readonly Vector3[] _rectCorners = new Vector3[4];
        private DrivenRectTransformTracker _rectTracker;
        private LayoutRoot _root;

        [NonSerialized]
        private readonly List<ChildInfo> _children = new List<ChildInfo> ();
        
        private Vector2 _contentSize;
        private int _depth;
        // private LayoutItem[] _layoutItems;
        private List<LayoutItem> _growChildren;
        private Vector2Int _growChildCount;

        private bool _childrenChanged;
        private bool _dirty;
        private int _ignoreCount;

        private Vector2 _lastSize;
        
        private static StringBuilder sb = new StringBuilder ();
        
        #region TypeDef

        public enum Justification
        {
            Start,
            Center,
            End,
            SpaceBetween
        }

        public enum Alignment
        {
            Start,
            Center,
            End
        }

        public enum LayoutDirection
        {
            Row,
            Column,
            RowReverse,
            ColumnReverse
        }

        [Serializable]
        public class ChildInfo
        {
            [HideInInspector]
            public RectTransform rt;
            
            public Vector2 size;
            public bool enabled;
            
            [HideInInspector]
            public LayoutItem li;
            public bool liPresent; // To avoid expensive null checks on component reference 
            public bool liIgnoreLayout;
            
            public int siblingIndex;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 pivot;

            public bool IsIgnored => !enabled || liIgnoreLayout;

            public void UpdateCache (RectTransform rt)
            {
                this.rt = rt;
                enabled = rt.gameObject.activeInHierarchy;
                size = rt.rect.size;
                
                siblingIndex = rt.GetSiblingIndex ();
                anchorMin = rt.anchorMin;
                anchorMax = rt.anchorMax;
                anchoredPosition = rt.anchoredPosition;
                sizeDelta = rt.sizeDelta;
                pivot = rt.pivot;
                
                li = rt.GetComponent<LayoutItem> ();
                liPresent = li != null;
                liIgnoreLayout = liPresent && li.IgnoreLayout;
            }

            public bool IsChanged (out bool siblingIndexChanged)
            {
                siblingIndexChanged = siblingIndex != rt.GetSiblingIndex ();
                if (siblingIndexChanged)
                    return true;
                
                if (enabled != rt.gameObject.activeInHierarchy)
                    return true;

                if (liPresent)
                {
                    if (liIgnoreLayout != li.IgnoreLayout)
                        return true;
                }
                
                // This method can be extended further to detect any additional changes, unless they require external info
                /*
                if (anchorMin != rt.anchorMin)
                    return true;
                
                if (anchorMax != rt.anchorMax)
                    return true;
                
                if (anchoredPosition != rt.anchoredPosition)
                    return true;
                
                if (sizeDelta != rt.sizeDelta)
                    return true;
                
                if (pivot != rt.pivot)
                    return true;
                */

                return false;
            }
        }

        #endregion

        #region Layout MonoBehavior

        protected override void Awake ()
        {
            base.Awake ();
            _rectTracker = new DrivenRectTransformTracker ();
            _growChildren = new List<LayoutItem> ();

            // find LayoutRoot
            _root = null;
            _depth = 0;
            Transform t = transform;
            while (_root == null)
            {
                if (t.TryGetComponent (out LayoutRoot root))
                {
                    _root = root;
                    break;
                }

                if (t.parent == null)
                {
                    Debug.LogError ("No LayoutRoot found! Aborting.");
                    break;
                }

                t = t.parent;
                _depth++;

                if (_depth > MAX_DEPTH)
                {
                    Debug.LogError ("Hit max search depth! Aborting.");
                    break;
                }
            }
        }

        protected override void OnEnable ()
        {
            base.OnEnable ();

            _root?.RegisterLayout (this);
            RefreshChildCache ();
            _dirty = true;
        }

        protected override void OnDisable ()
        {
            base.OnDisable ();
            _root?.UnregisterLayout (this);
        }

        public override void Update ()
        {
            base.Update ();
            
            if (m_log)
                sb.Clear ();
            
            // Always determine if children changed first
            bool childrenChanged = _childrenChanged || transform.childCount != _children.Count;
            if (!childrenChanged)
            {
                for (int i = 0, iLimit = _children.Count; i < iLimit; i++)
                {
                    var c = _children[i];
                    int siblingIndexCurrent = c.rt.GetSiblingIndex ();
                    bool siblingIndexChanged = c.siblingIndex != siblingIndexCurrent;
                    if (siblingIndexChanged)
                    {
                        childrenChanged = true;
                        c.siblingIndex = siblingIndexCurrent;

                        if (m_log)
                        {
                            sb.Append ("\n- ");
                            sb.Append (c.rt.name);
                            sb.Append (": Index changed to ");
                            sb.Append (siblingIndexCurrent);
                        }
                    }
                }
            }
            
            // Mark layout as changed if children have changed or if dirty flag is true
            bool layoutChanged = _dirty || childrenChanged;

            // Check if the container changed this frame, but only if layout hasn't changed
            if (!layoutChanged)
            {
                if (!Mathf.Approximately (_lastSize.x, _rect.rect.size.x) || 
                    !Mathf.Approximately (_lastSize.y, _rect.rect.size.y))
                {
                    layoutChanged = true;
                    
                    if (m_log)
                    {
                        sb.Append ("\n- Root size change: ");
                        sb.Append (_lastSize);
                        sb.Append (" > ");
                        sb.Append (_rect.rect.size);
                    }
                }
            }

            // Refresh child cache if needed
            if (childrenChanged)
            {
                _childrenChanged = false;
                
                // This operation will rebuild all ChildInfo, so there's no need to run a second children loop
                RefreshChildCache ();
                
                if (m_log)
                    sb.Append ("\n- Children changed");
            }
            else
            {
                for (int i = 0, iLimit = _children.Count; i < iLimit; i++)
                {
                    var c = _children[i];
                    var rt = c.rt;
                    
                    // check if item changed size this frame
                    if (!(c.liPresent && c.li.SizeMode.x == SizingMode.Grow) && !Mathf.Approximately (rt.rect.size.x, c.size.x))
                    {
                        c.size = c.size.SetX (rt.rect.size.x);
                        layoutChanged = true;

                        if (m_log)
                        {
                            sb.Append ("\n- ");
                            sb.Append (rt.name);
                            sb.Append (": X now ");
                            sb.Append (rt.rect.size.x);
                        }
                    }

                    if (!(c.liPresent && c.li.SizeMode.y == SizingMode.Grow) && !Mathf.Approximately (rt.rect.size.y, c.size.y))
                    {
                        c.size = c.size.SetY (rt.rect.size.y);
                        layoutChanged = true;
                    
                        if (m_log)
                        {
                            sb.Append ("\n- ");
                            sb.Append (rt.name);
                            sb.Append (": Y now ");
                            sb.Append (rt.rect.size.y);
                        }
                    }

                    if (c.IsChanged (out bool siblingIndexChanged))
                    {
                        layoutChanged = true;
                        if (siblingIndexChanged)
                            _childrenChanged = true;
                    
                        c.UpdateCache (rt);
                    
                        if (m_log)
                        {
                            sb.Append ("\n- ");
                            sb.Append (rt.name);
                            sb.Append (": Change");
                            if (siblingIndexChanged)
                                sb.Append (" (Index)");
                        }
                    }
                }
            }

            if (layoutChanged)
            {
                if (m_log)
                    Debug.Log ($"Layout changed: {name}{sb.ToString ()}", gameObject);
                
                _dirty = true;
                OnLayoutChanged?.Invoke ();
            }

            _lastSize = _rect.rect.size;
        }

        private void OnDrawGizmosSelected ()
        {
            _rect.GetWorldCorners (_rectCorners);

            Matrix4x4 ltw = _rect.localToWorldMatrix;

            foreach (Vector3 v in _rectCorners)
            {
                LayoutUtil.DrawCenteredDebugBox (v, 0.15f, 0.15f, Color.red);
            }

            Rect r = new Rect (_rectCorners[0], _rectCorners[2] - _rectCorners[0]);
            r.position += (Vector2)(ltw * new Vector2 (m_padding.left, m_padding.bottom));
            r.size -= (Vector2)(ltw * new Vector2 (m_padding.left + m_padding.right, m_padding.top + m_padding.bottom));

            LayoutUtil.DrawDebugBox (r, _rect.position.z, Color.green);
        }

        #endregion

        private void SetAnchorPivotX (RectTransform rt, float x)
        {
            rt.anchorMin = rt.anchorMin.SetX (x);
            rt.anchorMax = rt.anchorMax.SetX (x);
            rt.pivot = rt.pivot.SetX (x);
        }

        private void SetAnchorPivotY (RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMin.SetY (y);
            rt.anchorMax = rt.anchorMax.SetY (y);
            rt.pivot = rt.pivot.SetY (y);
        }

        #region LAYOUT PASSES

        public void ComputeFitSize ()
        {
            _growChildren.Clear ();
            _growChildCount = new Vector2Int (0, 0);
            _ignoreCount = 0;

            _rectTracker.Clear ();
            if (m_sizing.x == SizingMode.FitContent || (!_parent && m_sizing.x == SizingMode.Grow))
                _rectTracker.Add (this, _rect, DrivenTransformProperties.SizeDeltaX);
            if (m_sizing.y == SizingMode.FitContent || (!_parent && m_sizing.y == SizingMode.Grow))
                _rectTracker.Add (this, _rect, DrivenTransformProperties.SizeDeltaY);
            // Debug.Log ($"ComputeFitSize: {name}");

            if (m_log)
                sb.Clear ();

            if (_children.Count > 0)
            {
                // get number of disabled/ignore children
                foreach (var c in _children)
                {
                    if (c.IsIgnored)
                        _ignoreCount++;
                }

                float primarySize = m_justifyContent == Justification.SpaceBetween ? 0 : m_innerSpacing * (_children.Count - _ignoreCount - 1);
                float crossSize = 0;
                
                if (m_log)
                {
                    sb.Append ("\nPrimary size starts at: ");
                    sb.Append (primarySize);
                }

                switch (m_direction)
                {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        primarySize += m_padding.left + m_padding.right;
                        crossSize += m_padding.top + m_padding.bottom;
                        
                        if (m_log)
                        {
                            sb.Append ("\nPrimary size after padding: ");
                            sb.Append (primarySize);
                        }
                        
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        primarySize += m_padding.top + m_padding.bottom;
                        crossSize += m_padding.left + m_padding.right;
                        
                        if (m_log)
                        {
                            sb.Append ("\nPrimary size after padding: ");
                            sb.Append (primarySize);
                        }
                        
                        break;
                }
                
                float maxCrossSize = 0;
                foreach (var c in _children)
                {
                    if (c.IsIgnored)
                        continue;

                    var rt = c.rt;
                    bool growX = false, growY = false;

                    if (c.liPresent)
                    {
                        growX = c.li.SizeMode.x == SizingMode.Grow;
                        growY = c.li.SizeMode.y == SizingMode.Grow;
                        if (growX || growY)
                        {
                            _growChildren.Add (c.li);
                            _growChildCount.x += growX ? 1 : 0;
                            _growChildCount.y += growY ? 1 : 0;
                        }
                    }

                    switch (m_direction)
                    {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            primarySize += growX ? 0 : rt.sizeDelta.x;
                            maxCrossSize = Mathf.Max (maxCrossSize, growY ? 0 : rt.sizeDelta.y);
                            
                            if (m_log && !growX)
                            {
                                sb.Append ("\nPrimary size grows from ");
                                sb.Append (rt.name);
                                sb.Append (" by X ");
                                sb.Append (rt.sizeDelta.x);
                                sb.Append (": ");
                                sb.Append (primarySize);
                            }
                            
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            primarySize += growY ? 0 : rt.sizeDelta.y;
                            maxCrossSize = Mathf.Max (maxCrossSize, growX ? 0 : rt.sizeDelta.x);
                            
                            if (m_log && !growY)
                            {
                                sb.Append ("\nPrimary size grows ");
                                sb.Append (rt.name);
                                sb.Append (" by Y ");
                                sb.Append (rt.sizeDelta.y);
                                sb.Append (": ");
                                sb.Append (primarySize);
                            }
                            
                            break;
                    }
                }

                crossSize += maxCrossSize;

                // save content size for later
                switch (m_direction)
                {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        _contentSize = new Vector2 (primarySize, crossSize);
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        _contentSize = new Vector2 (crossSize, primarySize);
                        break;
                }

                // apply fit sizing X
                if (m_sizing.x == SizingMode.FitContent)
                {
                    switch (m_direction)
                    {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            _rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, primarySize);
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed X to primary size ");
                                sb.Append (primarySize);
                            }
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            _rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, crossSize);
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed X to cross size ");
                                sb.Append (crossSize);
                            }
                            break;
                    }
                }

                // apply fit sizing Y
                if (m_sizing.y == SizingMode.FitContent)
                {
                    switch (m_direction)
                    {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            _rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, crossSize);
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed Y to cross size ");
                                sb.Append (primarySize);
                            }
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            _rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, primarySize);
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed Y to primary size ");
                                sb.Append (primarySize);
                            }
                            break;
                    }
                }
            }
            else
            {
                _contentSize = Vector2.zero;
            }

            if (m_log)
                Debug.Log ($"{name} / ComputeFitSize:{sb.ToString()}");
        }

        public void GrowChildren ()
        {
            if (_growChildren.Count == 0)
                return;
            
            if (m_log)
                sb.Clear ();
            
            foreach (LayoutItem li in _growChildren)
            {
                Vector2 size;
                float crossSize;
                float leftover;
                switch (m_direction)
                {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        leftover = _rect.rect.size.x - _contentSize.x - m_padding.left - m_padding.right;
                        crossSize = _rect.rect.size.y - m_padding.top - m_padding.bottom;
                        size = new Vector2 (leftover / _growChildCount.x, crossSize);

                        if (li.SizeMode.x == SizingMode.Grow)
                        {
                            _rectTracker.Add (this, li.Rect, DrivenTransformProperties.SizeDeltaX);
                            li.Rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, size.x);
                            
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed X to ");
                                sb.Append (size.x);
                            }
                        }

                        if (li.SizeMode.y == SizingMode.Grow)
                        {
                            _rectTracker.Add (this, li.Rect, DrivenTransformProperties.SizeDeltaY);
                            li.Rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, size.y);
                            
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed Y to ");
                                sb.Append (size.y);
                            }
                        }

                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        leftover = _rect.rect.size.y - _contentSize.y - m_padding.top - m_padding.bottom;
                        crossSize = _rect.rect.size.x - m_padding.left - m_padding.right;
                        size = new Vector2 (crossSize, leftover / _growChildCount.y);

                        if (li.SizeMode.y == SizingMode.Grow)
                        {
                            _rectTracker.Add (this, li.Rect, DrivenTransformProperties.SizeDeltaY);
                            li.Rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, size.y);
                            
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed Y to ");
                                sb.Append (size.y);
                            }
                        }

                        if (li.SizeMode.x == SizingMode.Grow)
                        {
                            _rectTracker.Add (this, li.Rect, DrivenTransformProperties.SizeDeltaX);
                            li.Rect.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, size.x);
                            
                            if (m_log)
                            {
                                sb.Append ("\n- ");
                                sb.Append (_rect.name);
                                sb.Append (": Changed X to ");
                                sb.Append (size.x);
                            }
                        }

                        break;
                }
            }
            
            if (m_log)
                Debug.Log ($"{name} / GrowChildren:{sb.ToString()}");
        }

        public void ComputeLayout ()
        {
            if (_children.Count < 1)
            {
                return;
            }

            // apply RectTransform DrivenTransformProperties
            foreach (var c in _children)
            {
                if (c.IsIgnored)
                    continue;

                var rt = c.rt;
                _rectTracker.Add 
                (
                    this,
                    rt,
                    DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot | DrivenTransformProperties.Anchors
                );
            }

            // primary axis pass
            float primaryOffset = 0;
            float spacing = 0;
            float leftover = 0;
            int index = 0;

            switch (m_direction)
            {
                // ROW -> PRIMARY AXIS
                case LayoutDirection.Row:
                    switch (m_justifyContent)
                    {
                        case Justification.Start:
                            primaryOffset += m_padding.left;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }

                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.x / 2;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0.5f);

                                primaryOffset += rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset + m_padding.left);
                                primaryOffset += rt.sizeDelta.x / 2 + m_innerSpacing;
                            }

                            break;
                        case Justification.End:
                            primaryOffset -= m_padding.right + _contentSize.x;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 1);

                                primaryOffset += rt.sizeDelta.x;
                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset);
                                primaryOffset += m_innerSpacing;
                            }

                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.left;
                            leftover = _rect.rect.size.x - _contentSize.x;

                            if (_children.Count > 1)
                                spacing = leftover / (_children.Count - _ignoreCount - 1);

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0);

                                if (index != 0)
                                {
                                    primaryOffset += spacing;
                                }

                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset);
                                primaryOffset += rt.sizeDelta.x;
                                index++;
                            }

                            break;
                    }

                    break;
                // ROW_REVERSE -> PRIMARY AXIS
                case LayoutDirection.RowReverse:
                    switch (m_justifyContent)
                    {
                        case Justification.Start:
                            primaryOffset += m_padding.left + _contentSize.x;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0);

                                primaryOffset -= rt.sizeDelta.x + m_innerSpacing;
                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset);
                            }

                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.x / 2;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0.5f);

                                primaryOffset -= rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.SetX (primaryOffset - m_padding.right);
                                primaryOffset -= rt.sizeDelta.x / 2 + m_innerSpacing;
                            }

                            break;
                        case Justification.End:
                            primaryOffset += m_padding.right;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (-primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }

                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.right;

                            leftover = _rect.rect.size.x - _contentSize.x;

                            if (_children.Count > 1)
                                spacing = leftover / (_children.Count - 1);

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (-primaryOffset);
                                primaryOffset += rt.sizeDelta.x + spacing;
                            }

                            break;
                    }

                    break;
                // COLUMN -> PRIMARY AXIS
                case LayoutDirection.Column:
                    switch (m_justifyContent)
                    {
                        case Justification.Start:
                            primaryOffset -= m_padding.top;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset);
                                primaryOffset -= rt.sizeDelta.y + m_innerSpacing;
                            }

                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.y / 2;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0.5f);

                                primaryOffset -= rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset - m_padding.top);
                                primaryOffset -= rt.sizeDelta.y / 2 + m_innerSpacing;
                            }

                            break;
                        case Justification.End:
                            primaryOffset += _contentSize.y;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0);

                                primaryOffset -= rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset - m_padding.top);
                                primaryOffset -= m_innerSpacing;
                            }

                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.top;
                            leftover = _rect.rect.size.y - _contentSize.y;

                            if (_children.Count > 1)
                                spacing = leftover / (_children.Count - _ignoreCount - 1);

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 1);

                                if (index != 0)
                                {
                                    primaryOffset += spacing;
                                }

                                rt.anchoredPosition = rt.anchoredPosition.SetY (-primaryOffset);
                                primaryOffset += rt.sizeDelta.y;

                                index++;
                            }

                            break;
                    }

                    break;
                // COLUMN_REVERSE -> PRIMARY AXIS
                case LayoutDirection.ColumnReverse:
                    switch (m_justifyContent)
                    {
                        case Justification.Start:
                            primaryOffset -= m_padding.top + _contentSize.y;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 1);

                                primaryOffset += rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset);
                                primaryOffset += m_innerSpacing;
                            }

                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.y / 2;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0.5f);

                                primaryOffset += rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset - m_padding.top);
                                primaryOffset += rt.sizeDelta.y / 2 + m_innerSpacing;
                            }

                            break;
                        case Justification.End:
                            primaryOffset += m_padding.bottom;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset);
                                primaryOffset += rt.sizeDelta.y + m_innerSpacing;
                            }

                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.bottom;
                            leftover = _rect.rect.size.y - _contentSize.y;

                            if (_children.Count > 1)
                                spacing = leftover / (_children.Count - 1);

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (primaryOffset);
                                primaryOffset += rt.sizeDelta.y + spacing;
                            }

                            break;
                    }

                    break;
            }

            // cross axis pass
            float crossOffset = 0;
            switch (m_direction)
            {
                // ROW -> CROSS
                // ROW_REVERSE -> CROSS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    switch (m_alignContent)
                    {
                        case Alignment.Start:
                            crossOffset += m_padding.top;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (-crossOffset);
                            }

                            break;
                        case Alignment.Center:
                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (m_padding.bottom / 2 - m_padding.top / 2);
                            }

                            break;
                        case Alignment.End:
                            crossOffset += m_padding.bottom;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotY (rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.SetY (crossOffset);
                            }

                            break;
                    }

                    break;
                // COLUMN -> CROSS
                // COLUMN_REVERSE -> CROSS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    switch (m_alignContent)
                    {
                        case Alignment.Start:
                            crossOffset += m_padding.left;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (crossOffset);
                            }

                            break;
                        case Alignment.Center:
                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (m_padding.left / 2 - m_padding.right / 2);
                            }

                            break;
                        case Alignment.End:
                            crossOffset += m_padding.right;

                            foreach (var c in _children)
                            {
                                if (c.IsIgnored)
                                    continue;

                                var rt = c.rt;
                                SetAnchorPivotX (rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.SetX (-crossOffset);
                            }

                            break;
                    }

                    break;
            }

            _dirty = false;
        }

        #endregion

        public int CompareTo (Layout other)
        {
            if (_depth < other._depth)
            {
                return 1;
            }

            if (_depth == other._depth)
            {
                return 0;
            }

            return -1;
        }

        public void SetDirty (bool childrenChanged = false)
        {
            _dirty = true;
            if (childrenChanged)
                _childrenChanged = true;
        }

        public void RefreshChildCache ()
        {
            _children.Clear ();

            for (int i = 0, iLimit = transform.childCount; i < iLimit; i++)
            {
                Transform child = transform.GetChild (i);
                RectTransform rt = child.GetComponent<RectTransform> ();

                var childInfo = new ChildInfo ();
                childInfo.UpdateCache (rt);

                _children.Add (childInfo);
            }
        }
    }
}