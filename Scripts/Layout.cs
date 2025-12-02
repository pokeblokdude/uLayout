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
using System.Collections.Generic;
using UnityEngine;

namespace Poke.UI
{
    [
        ExecuteAlways,
        RequireComponent(typeof(RectTransform))
    ]
    public class Layout : MonoBehaviour, IComparable<Layout>
    {
        [Header("Sizing")]
        [SerializeField] private SizingModes m_sizingMode;
        
        [Header("Positioning")]
        [SerializeField] private Margins m_padding;
        [SerializeField] private LayoutDirection m_direction;
        [SerializeField] private Justification m_justifyContent;
        [SerializeField] private Alignment m_alignContent;
        [SerializeField] private float m_innerSpacing;

        public int ChildCount => _children.Count;
        public int Depth => _depth;
        public LayoutDirection Direction => m_direction;
        public SizingModes SizeMode => m_sizingMode;

        private readonly int MAX_DEPTH = 100;
        
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
        public struct SizingModes
        {
            public SizingMode x;
            public SizingMode y;
        }

        private RectTransform _rect;
        private readonly Vector3[] _rectCorners = new Vector3[4];
        private DrivenRectTransformTracker _rectTracker;
        private LayoutRoot _root;
        private Layout _parent;
        private RectTransform _parentRect;
        private List<RectTransform> _children = new();
        private Vector2 _contentSize;
        private int _depth;
        private bool _refreshCache;
        private int _growChildren;

        private void Awake() {
            _rect = GetComponent<RectTransform>();
            _rectTracker = new DrivenRectTransformTracker();
            _parentRect = transform.parent.GetComponent<RectTransform>();

            if(_parentRect.TryGetComponent(out Layout l)) {
                _parent = l;
            }
            
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

        private void OnEnable() {
            _root?.RegisterLayout(this);
            RefreshChildCache();
        }

        private void OnDisable() {
            _root?.UnregisterLayout(this);
        }

        public void Update() {
            // check if any children were added/removed this frame
            if(transform.childCount != _children.Count || _refreshCache) {
                RefreshChildCache();
                _refreshCache = false;
            }
            
            // check if any children were disabled this frame
            foreach(RectTransform rect in _children) {
                if(!rect.gameObject.activeInHierarchy) {
                    _refreshCache = true;
                }
            }
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

        #region LAYOUT PASSES
        public void ComputeFitSize() {
            _growChildren = 0;
            
            _rectTracker.Clear();
            if(m_sizingMode.x == SizingMode.FitContent)
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaX);
            if(m_sizingMode.y == SizingMode.FitContent)
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

                Layout l = null;
                // calculate content size
                float maxCrossSize = 0;
                foreach(RectTransform rt in _children) {
                    bool growX = false, growY = false;
                    
                    l = rt.GetComponent<Layout>();
                    if(l != null) {
                        growX = l.SizeMode.x == SizingMode.Grow;
                        growY = l.SizeMode.y == SizingMode.Grow;
                    }
                    
                    switch(m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            if(growX) _growChildren++;
                            
                            primarySize += growX ? 0 : rt.sizeDelta.x;
                            maxCrossSize = Mathf.Max(maxCrossSize, growY ? 0 : rt.sizeDelta.y);
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            if(growY) _growChildren++;
                            
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
                if(m_sizingMode.x == SizingMode.FitContent) {
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
                if(m_sizingMode.y == SizingMode.FitContent) {
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

        public void ComputeGrowSize() {
            bool grow = false;
            if(m_sizingMode.x == SizingMode.Grow) {
                grow = true;
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaX);
                Debug.Log("[Layout] Apply SizeDeltaX RectTracker");
            }

            if(m_sizingMode.y == SizingMode.Grow) {
                grow = true;
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaY);
                Debug.Log("[Layout] Apply SizeDeltaY RectTracker");
            }

            if(grow) {
                Vector2 parentSize;
                
                // parent is Layout
                if(_parent != null) {
                    Vector2 growSize = default;
                    switch(_parent.m_direction) {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            parentSize = _parentRect.rect.size - new Vector2(
                                0,
                                _parent.m_padding.bottom + _parent.m_padding.top
                            );
                            growSize = new Vector2((parentSize.x - _parent._contentSize.x) / _parent._growChildren, parentSize.y);

                            _parent._contentSize.x += growSize.x;
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            parentSize = _parentRect.rect.size - new Vector2(
                                _parent.m_padding.left + _parent.m_padding.right,
                                0
                            );
                            growSize = new Vector2(parentSize.x, (parentSize.y - _parent._contentSize.y) / _parent._growChildren);

                            _parent._contentSize.y += growSize.y;
                            break;
                    }
                    
                    if(m_sizingMode.x == SizingMode.Grow) {
                        Debug.Log("grow x layout");
                        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, growSize.x);
                    }
                    
                    if(m_sizingMode.y == SizingMode.Grow) {
                        Debug.Log("grow y layout");
                        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, growSize.y);
                    }
                }
                // parent is *not* Layout
                else {
                    parentSize = _parentRect.rect.size;
                    if(m_sizingMode.x == SizingMode.Grow) {
                        Debug.Log("grow x no layout");
                        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentSize.x);
                    }
                    
                    if(m_sizingMode.y == SizingMode.Grow) {
                        Debug.Log("grow y no layout");
                        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentSize.y);
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
            foreach(RectTransform rt in _children) {
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
            int lastChildIndex = _children.Count - 1;
            
            switch(m_direction) {
                // ROW -> PRIMARY AXIS
                case LayoutDirection.Row:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset += m_padding.left;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0);
                                rt.anchorMax = rt.anchorMax.With(x: 0);
                                rt.pivot = rt.pivot.With(x: 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.x / 2;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(x: 0.5f);
                                rt.pivot = rt.pivot.With(x: 0.5f);

                                primaryOffset += rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset + m_padding.left);
                                primaryOffset += rt.sizeDelta.x / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.right + _contentSize.x;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 1);
                                rt.anchorMax = rt.anchorMax.With(x: 1);
                                rt.pivot = rt.pivot.With(x: 1);

                                primaryOffset -= rt.sizeDelta.x;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset + m_padding.left + m_padding.right);
                                primaryOffset -= m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.left;
                            
                            leftover = _rect.sizeDelta.x - (m_padding.left + m_padding.right + _contentSize.x);
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);

                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0);
                                rt.anchorMax = rt.anchorMax.With(x: 0);
                                rt.pivot = rt.pivot.With(x: 0);

                                if(index != 0) {
                                    primaryOffset += m_padding.left / lastChildIndex + m_padding.right / lastChildIndex;
                                }
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                                primaryOffset += rt.sizeDelta.x + spacing;
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
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0);
                                rt.anchorMax = rt.anchorMax.With(x: 0);
                                rt.pivot = rt.pivot.With(x: 0);

                                primaryOffset -= rt.sizeDelta.x + m_innerSpacing;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset);
                            }
                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.x / 2;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(x: 0.5f);
                                rt.pivot = rt.pivot.With(x: 0.5f);

                                primaryOffset -= rt.sizeDelta.x / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(x: primaryOffset - m_padding.right);
                                primaryOffset -= rt.sizeDelta.x / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.right;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 1);
                                rt.anchorMax = rt.anchorMax.With(x: 1);
                                rt.pivot = rt.pivot.With(x: 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset);
                                primaryOffset += rt.sizeDelta.x + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.right;
                            
                            leftover = _rect.sizeDelta.x - (m_padding.left + m_padding.right) - _contentSize.x;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 1);
                                rt.anchorMax = rt.anchorMax.With(x: 1);
                                rt.pivot = rt.pivot.With(x: 1);
                                
                                if(index != 0) {
                                    primaryOffset += m_padding.left / lastChildIndex + m_padding.right / lastChildIndex;
                                }
                                
                                rt.anchoredPosition = rt.anchoredPosition.With(x: -primaryOffset);
                                primaryOffset += rt.sizeDelta.x + spacing;

                                index++;
                            }
                            break;
                    }
                    break;
                // COLUMN -> PRIMARY AXIS
                case LayoutDirection.Column:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            primaryOffset -= m_padding.top;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 1);
                                rt.anchorMax = rt.anchorMax.With(y: 1);
                                rt.pivot = rt.pivot.With(y: 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset -= rt.sizeDelta.y + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset += _contentSize.y / 2;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(y: 0.5f);
                                rt.pivot = rt.pivot.With(y: 0.5f);

                                primaryOffset -= rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset -= rt.sizeDelta.y / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += _contentSize.y;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0);
                                rt.anchorMax = rt.anchorMax.With(y: 0);
                                rt.pivot = rt.pivot.With(y: 0);

                                primaryOffset -= rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset -= m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.top;
                            
                            leftover = _rect.sizeDelta.y - (m_padding.top + m_padding.bottom) - _contentSize.y;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 1);
                                rt.anchorMax = rt.anchorMax.With(y: 1);
                                rt.pivot = rt.pivot.With(y: 1);
                                
                                if(index != 0) {
                                    primaryOffset += m_padding.top / lastChildIndex + m_padding.bottom / lastChildIndex;
                                }
                                
                                rt.anchoredPosition = rt.anchoredPosition.With(y: -primaryOffset);
                                primaryOffset += rt.sizeDelta.y + spacing;

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
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 1);
                                rt.anchorMax = rt.anchorMax.With(y: 1);
                                rt.pivot = rt.pivot.With(y: 1);

                                primaryOffset += rt.sizeDelta.y;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            primaryOffset -= _contentSize.y / 2;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(y: 0.5f);
                                rt.pivot = rt.pivot.With(y: 0.5f);

                                primaryOffset += rt.sizeDelta.y / 2;
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset - m_padding.top);
                                primaryOffset += rt.sizeDelta.y / 2 + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            primaryOffset += m_padding.bottom;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0);
                                rt.anchorMax = rt.anchorMax.With(y: 0);
                                rt.pivot = rt.pivot.With(y: 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += rt.sizeDelta.y + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            primaryOffset += m_padding.bottom;
                            
                            leftover = _rect.sizeDelta.y - (m_padding.top + m_padding.bottom) - _contentSize.y;
                            
                            if(_children.Count > 1)
                                spacing = leftover / (_children.Count-1);
                                
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0);
                                rt.anchorMax = rt.anchorMax.With(y: 0);
                                rt.pivot = rt.pivot.With(y: 0);
                                
                                if(index != 0) {
                                    primaryOffset += m_padding.top / lastChildIndex + m_padding.bottom / lastChildIndex;
                                }
                                
                                rt.anchoredPosition = rt.anchoredPosition.With(y: primaryOffset);
                                primaryOffset += rt.sizeDelta.y + spacing;

                                index++;
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
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 1);
                                rt.anchorMax = rt.anchorMax.With(y: 1);
                                rt.pivot = rt.pivot.With(y: 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: -crossOffset);
                            }
                            break;
                        case Alignment.Center:
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(y: 0.5f);
                                rt.pivot = rt.pivot.With(y: 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.With(y: m_padding.bottom/2 - m_padding.top/2);
                            }
                            break;
                        case Alignment.End:
                            crossOffset += m_padding.bottom;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(y: 0);
                                rt.anchorMax = rt.anchorMax.With(y: 0);
                                rt.pivot = rt.pivot.With(y: 0);

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
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0);
                                rt.anchorMax = rt.anchorMax.With(x: 0);
                                rt.pivot = rt.pivot.With(x: 0);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: crossOffset);
                            }
                            break;
                        case Alignment.Center:
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 0.5f);
                                rt.anchorMax = rt.anchorMax.With(x: 0.5f);
                                rt.pivot = rt.pivot.With(x: 0.5f);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: m_padding.left/2 - m_padding.right/2);
                            }
                            break;
                        case Alignment.End:
                            crossOffset += m_padding.right;
                            
                            foreach(RectTransform rt in _children) {
                                rt.anchorMin = rt.anchorMin.With(x: 1);
                                rt.anchorMax = rt.anchorMax.With(x: 1);
                                rt.pivot = rt.pivot.With(x: 1);

                                rt.anchoredPosition = rt.anchoredPosition.With(x: -crossOffset);
                            }
                            break;
                    }
                    break;
            }
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
        
        public void RefreshChildCache() {
            //Debug.Log("Refreshing child cache");
            _children.Clear();
            for(int i = 0; i < transform.childCount; i++) {
                RectTransform rt = transform.GetChild(i).GetComponent<RectTransform>();
                if(rt.gameObject.activeInHierarchy) {
                    if(!rt.TryGetComponent(out IgnoreLayout ignore) || !ignore.enabled) {
                        _children.Add(rt);
                    }
                }
            }
        }
    }
}
