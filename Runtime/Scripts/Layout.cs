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
        
        [Header("Layout")]
        [SerializeField] private Margins m_padding;
        [SerializeField] private LayoutDirection m_direction;
        [SerializeField] private Justification m_justifyContent;
        [SerializeField] private Alignment m_alignContent;
        [SerializeField] private float m_innerSpacing;

        public int ChildCount => _children.Count;
        public int Depth => _depth;
        public int GrowChildCount => _growChildren.Count;
        public LayoutDirection Direction => m_direction;
        public bool NeedsRefresh => _dirty;

        private readonly int MAX_DEPTH = 100;

        private readonly Vector3[] _rectCorners = new Vector3[4];
        private DrivenRectTransformTracker _rectTracker;
        private LayoutRoot _root;
        private Dictionary<RectTransform, ChildInfo> _children = new();
        private Vector2 _contentSize;
        private int _depth;
        private LayoutItem[] _layoutItems;
        private List<LayoutItem> _growChildren;
        private Vector2Int _growChildCount;
        private bool _dirty;
        
        private Vector2 _lastSize;

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

        private class ChildInfo
        {
            public int index;
            public Vector2 size;
            public bool enabled;
            public bool ignoreLayout;
        }
        #endregion
        
        #region Layout MonoBehavior
        protected override void Awake() {
            base.Awake();
            _rectTracker = new DrivenRectTransformTracker();
            _growChildren = new List<LayoutItem>();
            
            // find LayoutRoot
            _root = null;
            _depth = 0;
            Transform t = transform;
            while(_root == null) {
                if(t.parent == null) {
                    Debug.LogError("No UILayoutRoot found! Aborting.");
                    break;
                }

                if(t.TryGetComponent(out LayoutRoot root)) {
                    _root = root;
                    break;
                }

                t = t.parent;
                _depth++;

                if(_depth > MAX_DEPTH) {
                    Debug.LogError("Hit max search depth! Aborting.");
                    break;
                }
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            
            _root?.RegisterLayout(this);
            RefreshChildCache();
        }

        protected override void OnDisable() {
            base.OnDisable();
            _root?.UnregisterLayout(this);
        }

        public override void Update() {
            base.Update();
            bool layoutChanged = _dirty;
            
            // check if the container changed this frame
            if(!Mathf.Approximately(_lastSize.x, _rect.rect.size.x) || !Mathf.Approximately(_lastSize.y, _rect.rect.size.y)) {
                Debug.Log($"[{name}]: container size changed");
                layoutChanged = true;
            }
            // check if any children were added/removed this frame
            if(transform.childCount != _children.Count) {
                Debug.Log($"[{name}]: child count changed");
                RefreshChildCache();
                layoutChanged = true;
            }
            
            foreach(RectTransform rect in _children.Keys) {
                ChildInfo c = _children[rect];
                
                // check if item was disabled this frame
                if(rect.gameObject.activeInHierarchy != c.enabled) {
                    Debug.Log($"[{name}]: item active toggled");
                    c.enabled = rect.gameObject.activeInHierarchy;
                    layoutChanged = true;
                }

                LayoutItem li = _layoutItems[c.index];
                
                // check if ignore layout toggled this frame
                if(li && li.IgnoreLayout != c.ignoreLayout) {
                    Debug.Log($"[{name}]: item ignore toggled");
                    c.ignoreLayout = li.IgnoreLayout;
                    layoutChanged = true;
                }

                // check if item changed size this frame
                if(!(li && li.SizeMode.x == SizingMode.Grow) && !Mathf.Approximately(rect.rect.size.x, c.size.x)) {
                    Debug.Log($"[{name}]: item size x changed");
                    c.size = c.size.With(x: rect.rect.size.x); 
                    layoutChanged = true;
                }
                if(!(li && li.SizeMode.y == SizingMode.Grow) && !Mathf.Approximately(rect.rect.size.y, c.size.y)) {
                    Debug.Log($"[{name}]: item size y changed");
                    c.size = c.size.With(y: rect.rect.size.y);
                    layoutChanged = true;
                }
            }

            if(layoutChanged) {
                _dirty = true;
                OnLayoutChanged?.Invoke();
            }

            _lastSize = _rect.rect.size;
        }

        private void OnDrawGizmosSelected() {
            _rect.GetWorldCorners(_rectCorners);

            Matrix4x4 ltw = _rect.localToWorldMatrix;
            
            foreach(Vector3 v in _rectCorners) {
                LayoutUtil.DrawCenteredDebugBox(v, 0.15f, 0.15f, Color.red);
            }

            Rect r = new Rect(_rectCorners[0], _rectCorners[2] - _rectCorners[0]);
            r.position += (Vector2)(ltw * new Vector2(m_padding.left, m_padding.bottom));
            r.size -= (Vector2)(ltw * new Vector2(m_padding.left + m_padding.right, m_padding.top + m_padding.bottom));
            
            LayoutUtil.DrawDebugBox(r, _rect.position.z, Color.green);
        }
        #endregion

        private bool CheckIngoreElem(ChildInfo ci) {
            return !ci.enabled || ci.ignoreLayout;
        }

        private void SetAnchorPivotX(RectTransform rt, float x) {
            rt.anchorMin = rt.anchorMin.With(x: x);
            rt.anchorMax = rt.anchorMax.With(x: x);
            rt.pivot = rt.pivot.With(x: x);
        }
        private void SetAnchorPivotY(RectTransform rt, float y) {
            rt.anchorMin = rt.anchorMin.With(y: y);
            rt.anchorMax = rt.anchorMax.With(y: y);
            rt.pivot = rt.pivot.With(y: y);
        }
        
        #region LAYOUT PASSES
        public void ComputeFitSize() {
            _growChildren.Clear();
            _growChildCount = new Vector2Int(0, 0);
            
            _rectTracker.Clear();
            if(m_sizing.x == SizingMode.FitContent || (!_parent && m_sizing.x == SizingMode.Grow))
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaX);
            if(m_sizing.y == SizingMode.FitContent || (!_parent && m_sizing.y == SizingMode.Grow))
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaY);
            
            if(_children.Count > 0) {
                float primarySize = m_innerSpacing * (_children.Count-1);
                float crossSize = 0;
                
                switch(m_direction) {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        primarySize += m_padding.left + m_padding.right;
                        crossSize += m_padding.top + m_padding.bottom;
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        primarySize += m_padding.top + m_padding.bottom;
                        crossSize += m_padding.left + m_padding.right;
                        break;
                }

                LayoutItem li = null;
                // calculate content size
                float maxCrossSize = 0;
                foreach(RectTransform rt in _children.Keys) {
                    // skip disabled/ignore items
                    ChildInfo elem = _children[rt];
                    if(!elem.enabled || elem.ignoreLayout)
                        continue;
                    
                    bool growX = false, growY = false;
                    
                    li = _layoutItems[elem.index];
                    if(li) {
                        growX = li.SizeMode.x == SizingMode.Grow;
                        growY = li.SizeMode.y == SizingMode.Grow;
                        if(growX || growY) {
                            _growChildren.Add(li);
                            _growChildCount.x += growX ? 1 : 0;
                            _growChildCount.y += growY ? 1 : 0;
                        }
                    }
                    
                    switch(m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            primarySize += growX ? 0 : rt.sizeDelta.x;
                            maxCrossSize = Mathf.Max(maxCrossSize, growY ? 0 : rt.sizeDelta.y);
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            primarySize += growY ? 0 : rt.sizeDelta.y;
                            maxCrossSize = Mathf.Max(maxCrossSize, growX ? 0 : rt.sizeDelta.x);
                            break;
                    }
                }
                crossSize += maxCrossSize;

                // save content size for later
                switch(m_direction) {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        _contentSize = new Vector2(primarySize, crossSize);
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        _contentSize = new Vector2(crossSize, primarySize);
                        break;
                }
                
                // apply fit sizing X
                if(m_sizing.x == SizingMode.FitContent) {
                    switch(m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, primarySize);
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, crossSize);
                            break;
                    }
                }
                
                // apply fit sizing Y
                if(m_sizing.y == SizingMode.FitContent) {
                    switch(m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, crossSize);
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, primarySize);
                            break;
                    }
                }
            }
            else {
                _contentSize = Vector2.zero;
            }
        }

        public void GrowChildren() {
            if(_growChildren.Count > 0) {
                foreach(LayoutItem li in _growChildren) {
                    Vector2 size;
                    float crossSize;
                    float leftover;
                    switch(m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            leftover = _rect.rect.size.x - _contentSize.x - m_padding.left - m_padding.right;
                            crossSize = _rect.rect.size.y - m_padding.top - m_padding.bottom;
                            size = new Vector2(leftover / _growChildCount.x, crossSize);
                            
                            if(li.SizeMode.x == SizingMode.Grow) {
                                _rectTracker.Add(this, li.Rect, DrivenTransformProperties.SizeDeltaX);
                                li.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                            }
                            if(li.SizeMode.y == SizingMode.Grow) {
                                _rectTracker.Add(this, li.Rect, DrivenTransformProperties.SizeDeltaY);
                                li.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                            }
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            leftover = _rect.rect.size.y - _contentSize.y - m_padding.top - m_padding.bottom;
                            crossSize = _rect.rect.size.x - m_padding.left - m_padding.right;
                            size = new Vector2(crossSize, leftover / _growChildCount.y);
                            
                            if(li.SizeMode.y == SizingMode.Grow) {
                                _rectTracker.Add(this, li.Rect, DrivenTransformProperties.SizeDeltaY);
                                li.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                            }
                            if(li.SizeMode.x == SizingMode.Grow) {
                                _rectTracker.Add(this, li.Rect, DrivenTransformProperties.SizeDeltaX);
                                li.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                            }
                            break;
                    }
                }
            }
        }
        
        public void ComputeLayout() {
            if(_children.Count < 1) {
                Debug.LogWarning("Layout has no children - skipping layout computations");
                return;
            }
            
            // apply RectTransform DrivenTransformProperties
            foreach(RectTransform rt in _children.Keys) {
                _rectTracker.Add(
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
            
            switch(m_direction) {
                // ROW -> PRIMARY AXIS
                case LayoutDirection.Row:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset += m_padding.left;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.x / 2;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0.5f);

                                primaryOffset += rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset + m_padding.left);
                                primaryOffset += rt.sizeDelta.x / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.right + _contentSize.x;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 1);

                                primaryOffset -= rt.sizeDelta.x;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset + m_padding.left + m_padding.right);
                                primaryOffset -= m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.left;
                            leftover = _rect.rect.size.x - _contentSize.x;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);

                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0);

                                if(index != 0) {
                                    primaryOffset += spacing;
                                }
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                                primaryOffset += rt.sizeDelta.x;
                                index++;
                            }
                            break;
                    }
                    break;
                // ROW_REVERSE -> PRIMARY AXIS
                case LayoutDirection.RowReverse:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset += m_padding.left + _contentSize.x;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0);

                                primaryOffset -= rt.sizeDelta.x + m_innerSpacing;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                            }
                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.x / 2;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0.5f);

                                primaryOffset -= rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset - m_padding.right);
                                primaryOffset -= rt.sizeDelta.x / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.right;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.right;
                            
                            leftover = _rect.rect.size.x - _contentSize.x;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 1);
                                
                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset);
                                primaryOffset += rt.sizeDelta.x + spacing;
                            }
                            break;
                    }
                    break;
                // COLUMN -> PRIMARY AXIS
                case LayoutDirection.Column:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset -= m_padding.top;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset -= rt.sizeDelta.y + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.y / 2;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0.5f);

                                primaryOffset -= rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset -= rt.sizeDelta.y / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += _contentSize.y;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0);

                                primaryOffset -= rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset -= m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.top;
                            leftover = _rect.rect.size.y - _contentSize.y;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 1);
                                
                                if(index != 0) {
                                    primaryOffset += spacing;
                                }
                                rt.anchoredPosition = rt.anchoredPosition.With(y: -primaryOffset);
                                primaryOffset += rt.sizeDelta.y;

                                index++;
                            }
                            break;
                    }
                    break;
                // COLUMN_REVERSE -> PRIMARY AXIS
                case LayoutDirection.ColumnReverse:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset -= m_padding.top + _contentSize.y;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 1);

                                primaryOffset += rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.y / 2;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                SetAnchorPivotY(rt, 0.5f);

                                primaryOffset += rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset += rt.sizeDelta.y / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.bottom;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += rt.sizeDelta.y + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.bottom;
                            
                            leftover = _rect.rect.size.y - _contentSize.y;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0);
                                
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += rt.sizeDelta.y + spacing;
                            }
                            break;
                    }
                    break;
            }
            
            // cross axis pass
            float crossOffset = 0;
            switch(m_direction) {
                // ROW -> CROSS
                // ROW_REVERSE -> CROSS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    switch(m_alignContent) {
                        case Alignment.Start:
                            crossOffset += m_padding.top;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: -crossOffset);
                            }
                            break;
                        case Alignment.Center:
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: m_padding.bottom/2 - m_padding.top/2);
                            }
                            break;
                        case Alignment.End:
                            crossOffset += m_padding.bottom;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotY(rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: crossOffset);
                            }
                            break;
                    }
                    break;
                // COLUMN -> CROSS
                // COLUMN_REVERSE -> CROSS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    switch(m_alignContent) {
                        case Alignment.Start:
                            crossOffset += m_padding.left;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: crossOffset);
                            }
                            break;
                        case Alignment.Center:
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: m_padding.left/2 - m_padding.right/2);
                            }
                            break;
                        case Alignment.End:
                            crossOffset += m_padding.right;
                            
                            foreach(RectTransform rt in _children.Keys) {
                                // skip disabled/ignore items
                                if(CheckIngoreElem(_children[rt]))
                                    continue;
                                
                                SetAnchorPivotX(rt, 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: -crossOffset);
                            }
                            break;
                    }
                    break;
            }

            _dirty = false;
        }
        #endregion
        
        public int CompareTo(Layout other) {
            if(_depth < other._depth) {
                return 1;
            }
            if(_depth == other._depth) {
                return 0;
            }
            
            return -1;
        }

        public void SetDirty() {
            _dirty = true;
        }
        
        public void RefreshChildCache() {
            _children.Clear();
            _layoutItems = new LayoutItem[transform.childCount];
            
            for(int i = 0; i < transform.childCount; i++) {
                RectTransform rt = transform.GetChild(i).GetComponent<RectTransform>();
                
                LayoutItem li = rt.GetComponent<LayoutItem>();
                _layoutItems[i] = li;
                
                _children.Add(
                    rt,
                    new ChildInfo {
                        index = i,
                        size = rt.rect.size,
                        enabled = rt.gameObject.activeInHierarchy,
                    }
                );
            }
        }
    }
}
