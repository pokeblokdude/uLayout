/*
    Copyright (c) 2026 Alex Howe

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
using UnityEngine.UI;

namespace Poke.UI
{
    public class Layout : LayoutItem, ILayoutGroup
    {
        /* THINGS THAT CAN CAUSE A LAYOUT UPDATE
            - non-grow child RectTransform changes size
            - number of children change
            - child is enabled/disabled
            - this container changes
        */

#if UNITY_EDITOR
        public static readonly List<Layout> RefreshedThisFrame = new();
#endif
        public event Action OnLayoutChanged;
        
        [SerializeField] private Margins            m_padding;
        [SerializeField] private LayoutDirection    m_direction;
        [SerializeField] private Justification      m_justifyContent;
        [SerializeField] private Alignment          m_alignContent;
        [SerializeField] private float              m_innerSpacing;
        [SerializeField] private bool               m_ignoreChildScale;
        [SerializeField] private bool               m_wrap;
        [SerializeField] private float              m_lineSpacing;
        [SerializeField] private Alignment          m_alignItems;

        #region Properties
        public Margins Padding
        {
            get => m_padding;
            set
            {
                m_padding = value;
                SetDirty();
            }
        }
        public LayoutDirection Direction
        {
            get => m_direction;
            set
            {
                m_direction = value;
                SetDirty();
            }
        }
        public Justification JustifyContent
        {
            get => m_justifyContent;
            set
            {
                m_justifyContent = value;
                SetDirty();
            }
        }
        public Alignment AlignContent
        {
            get => m_alignContent;
            set
            {
                m_alignContent = value;
                SetDirty();
            }
        }
        public float InnerSpacing
        {
            get => m_innerSpacing;
            set
            {
                m_innerSpacing = value;
                SetDirty();
            }
        }
        public bool IgnoreChildScale
        {
            get => m_ignoreChildScale;
            set
            {
                m_ignoreChildScale = value;
                SetDirty();
            }
        }
        public bool Wrap
        {
            get => m_wrap;
            set
            {
                m_wrap = value;
                SetDirty();
            }
        }
        public float LineSpacing
        {
            get => m_lineSpacing;
            set
            {
                m_lineSpacing = value;
                SetDirty();
            }
        }
        public Alignment AlignItems {
            get => m_alignItems;
            set {
                m_alignItems = value;
                SetDirty();
            }
        }
        #endregion

        public int ChildCount => _children?.Count ?? 0;
        public Vector2Int GrowChildCount => _growChildCount;

        private readonly List<ChildInfo>    _children = new();
        private Vector2                     _contentSize;
        private Vector2Int                  _growChildCount;
        private int                         _ignoreCount;
        private Vector2                     _innerSize;
        private Vector2                     _lastSize;
        private readonly List<LineInfo>     _lines = new();
        private bool                        _precalcYSize;
        
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
            public RectTransform rect;
            public LayoutItem li;
            public SizingMode sizingX;
            public SizingMode sizingY;
            public Vector2 size;
            public Margins margins;
            public bool enabled;
            public bool ignoreLayout;
            public int lineIndex;
        }

        private struct LineInfo
        {
            public int firstItemIdx;      // first child index in _children for this line (inclusive)
            public int lastItemIdx;       // exclusive
            public int itemCount;
            public int ignoreCount;
            public float primarySize;      // sum of non-grow primary sizes + innerSpacing * (count-1)
            public float crossSize;        // max cross size of the line
        }
        #endregion

        #region Layout MonoBehavior
        protected override void OnEnable() {
            base.OnEnable();
            Log("enable");
            RefreshChildCache();
        }

        public override void Update() {
            base.Update();

            bool layoutChanged = _dirty;
            bool needsCacheRefresh = false;

            // check for changes in children
            foreach(ChildInfo c in _children) {
                if(!c.rect) {
                    layoutChanged = true;
                    needsCacheRefresh = true;
                    continue;
                }

                // check if child index has changed
                if(c.rect.GetSiblingIndex() != c.index) {
                    layoutChanged = true;
                    needsCacheRefresh = true;
                }

                // check if item was disabled this frame
                if(c.rect.gameObject.activeInHierarchy != c.enabled) {
                    c.enabled = c.rect.gameObject.activeInHierarchy;
                    layoutChanged = true;
                }
                
                if(!CheckIgnoreElem(c) && c.rect.rect.size != c.size) {
                    layoutChanged = true;
                }

                if(c.li) {
                    // check if ignore layout toggled this frame
                    if(c.li.IgnoreLayout != c.ignoreLayout) {
                        c.ignoreLayout = c.li.IgnoreLayout;
                        layoutChanged = true;
                    }

                    if(c.li.Margins != c.margins) {
                        c.margins = c.li.Margins;
                        layoutChanged = true;
                    }

                    if(c.li.Sizing.x != c.sizingX) {
                        c.sizingX = c.li.Sizing.x;
                        layoutChanged = true;
                    }
                    
                    if(c.li.Sizing.y != c.sizingY) {
                        c.sizingY = c.li.Sizing.y;
                        layoutChanged = true;
                    }
                }
                else {
                    _tracker.Add(
                        this,
                        c.rect,
                        DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Anchors
                    );
                }
            }

            // check if the container changed this frame
            if(!Mathf.Approximately(_lastSize.x, _rect.rect.size.x) || !Mathf.Approximately(_lastSize.y, _rect.rect.size.y)) {
                layoutChanged = true;
            }
            // check if any children were added/removed this frame
            if(transform.childCount != _children.Count) {
                layoutChanged = true;
                needsCacheRefresh = true;
            }

            if(layoutChanged) {
                SetDirty();
                Log("marking for rebuild");
                LayoutRebuilder.MarkLayoutForRebuild(_rect);
                if(needsCacheRefresh)
                    RefreshChildCache();
            }

            _lastSize = _rect.rect.size;
        }
        
        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();

            Matrix4x4 ltw = _rect.localToWorldMatrix;

            if(m_padding.top != 0 || m_padding.bottom != 0 || m_padding.left != 0 || m_padding.right != 0) {
                Rect r = new Rect(_rectCorners[0], _rectCorners[2] - _rectCorners[0]);
                r.position += (Vector2)(ltw * new Vector2(m_padding.left, m_padding.bottom));
                r.size -= (Vector2)(ltw * new Vector2(m_padding.left + m_padding.right, m_padding.top + m_padding.bottom));

                LayoutUtil.DrawDebugBox(r, _rect.position.z, Color.green);
            }
        }
        #endregion

        #region ILayoutGroup
        public override void CalculateLayoutInputHorizontal() {
            if(!_dirty) return;
            
#if UNITY_EDITOR
            RefreshedThisFrame.Add(this);
#endif
            Log("<color=white>CalculateLayoutInputHorizontal</color>");
            
            _growChildCount.x = 0;
            _ignoreCount = 0;
            _innerSize = Vector2.zero;
            _precalcYSize = false;
            
            // get number of disabled/ignore children
            foreach(ChildInfo c in _children) {
                if(CheckIgnoreElem(c)) {
                    _ignoreCount++;
                }
                else {
                    c.size = c.size.SetX(Mathf.Max(0, c.rect.rect.size.x * (m_ignoreChildScale ? 1 : c.rect.localScale.x)));
                }
            }
            
            if(_children.Count > 0) {
                float primarySize = m_justifyContent == Justification.SpaceBetween ? 0 : m_innerSpacing * (_children.Count - _ignoreCount - 1);
                float crossSize = 0;

                // calculate content size
                float maxCrossSize = 0;
                foreach(ChildInfo c in _children) {
                    // skip disabled/ignore items
                    if(CheckIgnoreElem(c))
                        continue;

                    bool grow = false;
                    if(c.li) {
                        grow = c.li.Sizing.x == SizingMode.Grow;
                        if(grow) {
                            _growChildCount.x++;
                        }
                    }

                    float margins = c.margins.left + c.margins.right;
                    
                    switch(m_direction)
                    {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            primarySize += grow ? 0 : c.size.x + margins;
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            maxCrossSize = Mathf.Max(maxCrossSize, grow ? 0 : c.size.x + margins);
                            break;
                    }

                }
                crossSize += maxCrossSize;

                // save content size for later
                switch(m_direction)
                {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        _contentSize.x = primarySize;
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        _contentSize.x = crossSize;
                        break;
                }
            }
            else {
                _contentSize = Vector2.zero;
            }
            
            // apply fit sizing X
            if(m_sizing.x == SizingMode.FitContent) {
                float size = _contentSize.x + m_padding.left + m_padding.right;
                if(m_useMaxWidth) size = Mathf.Min(m_maxWidth, size);
                if(m_useMinWidth) size = Mathf.Max(m_minWidth, size);
                
                _rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    size
                );
            }
            
            _innerSize.x = _rect.rect.size.x - m_padding.left - m_padding.right;
            Log($"calculated rect x size: {_rect.rect.size.x:f3}, inner: {_innerSize.x}");

            if(IsRowDirection() && m_wrap && _contentSize.x > _innerSize.x) {
                PackLines();
            }
        }

        public override void CalculateLayoutInputVertical() {
            if(!_dirty) return;
            
            Log("<color=white>CalculateLayoutInputVertical</color>");
            
            _growChildCount.y = 0;

            foreach(ChildInfo c in _children) {
                if(!CheckIgnoreElem(c)) {
                    c.size = c.size.SetY(Mathf.Max(0, c.rect.rect.size.y * (m_ignoreChildScale ? 1 : c.rect.localScale.y)));
                }
            }
            
            if(_children.Count > 0 && !_precalcYSize) {
                float primarySize = m_justifyContent == Justification.SpaceBetween ? 0 : m_innerSpacing * (_children.Count - _ignoreCount - 1);
                float crossSize = 0;

                // calculate content size
                float maxCrossSize = 0;
                foreach(ChildInfo c in _children) {
                    // skip disabled/ignore items
                    if(CheckIgnoreElem(c))
                        continue;

                    bool grow = false;
                    if(c.li) {
                        grow = c.li.Sizing.y == SizingMode.Grow;
                        if(grow) {
                            _growChildCount.y++;
                        }
                    }

                    float margins = c.margins.top + c.margins.bottom;
                    
                    switch(m_direction)
                    {
                        case LayoutDirection.Row:
                        case LayoutDirection.RowReverse:
                            maxCrossSize = Mathf.Max(maxCrossSize, grow ? 0 : c.size.y + margins);
                            break;
                        case LayoutDirection.Column:
                        case LayoutDirection.ColumnReverse:
                            primarySize += grow ? 0 : c.size.y + margins;
                            break;
                    }
                }
                crossSize += maxCrossSize;

                // save content size for later
                switch(m_direction)
                {
                    case LayoutDirection.Row:
                    case LayoutDirection.RowReverse:
                        _contentSize.y = crossSize;
                        break;
                    case LayoutDirection.Column:
                    case LayoutDirection.ColumnReverse:
                        _contentSize.y = primarySize;
                        break;
                }
                
                Log($"calculated rect y size: {_rect.rect.size.y:f3}");
            }
            else if(!_precalcYSize) {
                _contentSize = Vector2.zero;
            }

            // apply fit sizing Y
            if(m_sizing.y == SizingMode.FitContent) {
                float size = _contentSize.y + m_padding.top + m_padding.bottom;
                if(m_useMaxHeight) size = Mathf.Min(m_maxHeight, size);
                if(m_useMinHeight) size = Mathf.Max(m_minHeight, size);
                
                _rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    size
                );
            }

            _innerSize.y = _rect.rect.size.y - m_padding.top - m_padding.bottom;
            Log($"calculated rect y size: {_rect.rect.size.y:f3}, inner: {_innerSize.y}");

            if(!IsRowDirection() && m_wrap && _contentSize.y > _innerSize.y) {
                PackColumns();
            }
        }

        public void SetLayoutHorizontal() {
            if(!_dirty) return;
            
            Log("<color=white>SetLayoutHorizontal</color>");
            
            if(m_wrap && _lines.Count > 0) {
                if(IsRowDirection()) {
                    GrowChildrenHorizontalWrapped(_contentSize.x);
                    foreach(LineInfo line in _lines) {
                        HorizontalLayout(line.firstItemIdx, line.lastItemIdx, line.crossSize, line.primarySize, line.ignoreCount);
                    }
                }
                // vertical wrap requires calculating vertical sizes BEFORE horizontal layout
            }
            else {
                GrowChildrenHorizontal();
                HorizontalLayout(0, _children.Count-1, _contentSize.x, _contentSize.x, _ignoreCount);
            }
        }

        public void SetLayoutVertical() {
            if(!_dirty) return;
            
            Log("<color=white>SetLayoutVertical</color>");
            
            if(m_wrap && _lines.Count > 0) {
                GrowChildrenVerticalWrapped(_contentSize.y);
                if(IsRowDirection()) {
                    float offset = 0;
                    foreach(LineInfo line in _lines) {
                        VerticalLayout(line.firstItemIdx, line.lastItemIdx, line.crossSize, _contentSize.y, line.ignoreCount, offset);
                        offset += line.crossSize + m_lineSpacing;
                    }
                }
                else {
                    foreach(LineInfo line in _lines) {
                        VerticalLayout(line.firstItemIdx, line.lastItemIdx, line.crossSize, line.primarySize, line.ignoreCount);
                    }
                    
                    GrowChildrenHorizontalWrapped(_contentSize.x);
                    
                    float offset = 0;
                    foreach(LineInfo line in _lines) {
                        HorizontalLayout(line.firstItemIdx, line.lastItemIdx, line.crossSize, _contentSize.x, line.ignoreCount, offset);
                        offset += line.crossSize + m_lineSpacing;
                    }
                }
            }
            else {
                GrowChildrenVertical();
                VerticalLayout(0, _children.Count-1, _contentSize.y, _contentSize.y, _ignoreCount);
            }

            OnLayoutChanged?.Invoke();
            _dirty = false;
        }
        #endregion

        #region Layout Helpers
        private void Log(object msg) {
            if(m_log) Debug.Log($"[{_frame}] [L:{gameObject.name}]: {msg}");
        }
        
        private bool CheckIgnoreElem(ChildInfo ci) {
            return ci.rect == null || !ci.enabled || ci.ignoreLayout;
        }

        private bool IsRowDirection() {
            return m_direction == LayoutDirection.Row || m_direction == LayoutDirection.RowReverse;
        }
        
        private void SetAnchorX(RectTransform rt, float x) {
            rt.anchorMin = rt.anchorMin.SetX(x);
            rt.anchorMax = rt.anchorMax.SetX(x);
        }
        
        private void SetAnchorY(RectTransform rt, float y) {
            rt.anchorMin = rt.anchorMin.SetY(y);
            rt.anchorMax = rt.anchorMax.SetY(y);
        }
        #endregion
        
        #region Layout Internal
        private void HorizontalLayout(int childStartIdx, int childEndIdx, float columnWidth, float contentWidth, int ignoreCount, float startOffset = 0) {
            Log($"Horizontal Layout - content total width: {contentWidth}");
            
            float offset = startOffset;
            float leftover;
            float spacing = 0;
            
            switch(m_direction) {
                // ROW -> PRIMARY AXIS
                case LayoutDirection.Row:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            offset += m_padding.left;
                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.margins.left + pivot;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset);
                                offset += (c.size.x - pivot) + c.margins.right + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            offset -= (contentWidth + m_padding.left + m_padding.right) / 2;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0.5f);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.margins.left + pivot;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset + m_padding.left);
                                offset += (c.size.x - pivot) + c.margins.right + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            offset -= m_padding.right + contentWidth;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 1);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.margins.left + pivot;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset);
                                offset += c.size.x - pivot + c.margins.right + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            offset += m_padding.left;
                            leftover = _rect.rect.size.x - contentWidth - m_padding.left - m_padding.right;
                            
                            Log($"space-between leftover: {leftover}");
                            
                            int count = childEndIdx - childStartIdx + 1;
                            if(count > 1)
                                spacing = leftover / (count - ignoreCount - 1);

                            Log($"spacing: {spacing}");
                            
                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.margins.left + pivot;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset);
                                offset += c.size.x - pivot + c.margins.right + spacing;
                            }
                            break;
                    }
                    break;
                // ROW-REVERSE -> PRIMARY AXIS
                case LayoutDirection.RowReverse:
                    switch(m_justifyContent) {
                        case Justification.Start:
                            offset += m_padding.left + contentWidth;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset -= c.size.x - pivot + c.margins.right;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset);
                                offset -= pivot + c.margins.left + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            offset = contentWidth/2;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if (CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0.5f);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset -= c.size.x - pivot + c.margins.right;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset);
                                offset -= pivot + c.margins.left + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            offset += m_padding.right;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 1);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.size.x - pivot + c.margins.right;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(-offset);
                                offset += pivot + c.margins.left + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            offset += m_padding.right;
                            leftover = _rect.rect.size.x - contentWidth - m_padding.left - m_padding.right;

                            int count = childEndIdx - childStartIdx + 1;
                            if(count > 1)
                                spacing = leftover / (count - ignoreCount - 1);

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 1);

                                float pivot = c.size.x * c.rect.pivot.x;
                                offset += c.size.x - pivot + c.margins.right;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(-offset);
                                offset += pivot + c.margins.left + spacing;
                            }
                            break;
                    }
                    break;
                // COLUMN/COLUMN-REVERSE -> CROSS AXIS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    switch(m_alignContent) {
                        case Alignment.Start:
                            offset = m_padding.left + startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0);

                                LayoutColumnAligned(c, offset, columnWidth);
                            }
                            break;
                        case Alignment.Center:
                            offset = -contentWidth / 2 + startOffset;
                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 0.5f);

                                LayoutColumnAligned(c, offset, columnWidth);
                            }
                            break;
                        case Alignment.End:
                            offset = -m_padding.right - contentWidth + startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorX(c.rect, 1);

                                LayoutColumnAligned(c, offset, columnWidth);
                            }
                            break;
                    }
                    break;
            }

        }

        private void VerticalLayout(int childStartIdx, int childEndIdx, float lineHeight, float contentHeight, int ignoreCount, float startOffset = 0) {
            Log($"Vertical Layout - content size y: {contentHeight}");
            
            float offset = 0;
            float leftover;
            float spacing = 0;
            int index = 0;
            switch(m_direction) {
                // ROW/ROW-REVERSE -> CROSS AXIS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    switch(m_alignContent) {
                        case Alignment.Start:
                            offset = -m_padding.top - startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 1);

                                LayoutRowAligned(c, offset, lineHeight);
                            }
                            break;
                        case Alignment.Center:
                            offset = contentHeight / 2 - startOffset;
                            
                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0.5f);

                                LayoutRowAligned(c, offset, lineHeight);
                            }
                            break;
                        case Alignment.End:
                            offset = m_padding.bottom + contentHeight - startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0);

                                LayoutRowAligned(c, offset, lineHeight);
                            }
                            break;
                    }
                    break;
                // COLUMN -> PRIMARY AXIS
                case LayoutDirection.Column:
                    switch(m_justifyContent)
                    {
                        case Justification.Start:
                            offset -= m_padding.top;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 1);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset -= c.size.y - pivot + c.margins.top;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset -= pivot + c.margins.bottom + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            offset = contentHeight/2 - startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0.5f);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset -= c.size.y - pivot + c.margins.top;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset -= pivot + c.margins.bottom + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            offset = contentHeight - startOffset;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset -= c.size.y - pivot + c.margins.top;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset + m_padding.bottom);
                                offset -= pivot + c.margins.bottom + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            offset += m_padding.top;
                            leftover = _rect.rect.size.y - contentHeight - m_padding.top - m_padding.bottom;

                            int count = childEndIdx - childStartIdx + 1;
                            if(count > 1)
                                spacing = leftover / (count - ignoreCount - 1);

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 1);

                                if(index != 0) {
                                    offset += spacing;
                                }

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset += c.size.y - pivot + c.margins.top;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(-offset);
                                offset += pivot + c.margins.bottom;

                                index++;
                            }
                            break;
                    }
                    break;
                // COLUMN-REVERSE -> PRIMARY AXIS
                case LayoutDirection.ColumnReverse:
                    switch(m_justifyContent)
                    {
                        case Justification.Start:
                            offset = -m_padding.top - contentHeight;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 1);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset += pivot + c.margins.bottom;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset += c.size.y - pivot + c.margins.top + m_innerSpacing;
                            }
                            break;
                        case Justification.Center:
                            offset = -contentHeight/2;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0.5f);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset += pivot + c.margins.bottom;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset += c.size.y - pivot + c.margins.top + m_innerSpacing;
                            }
                            break;
                        case Justification.End:
                            offset = m_padding.bottom;

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0);

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset += pivot + c.margins.bottom;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset += c.size.y - pivot + c.margins.top + m_innerSpacing;
                            }
                            break;
                        case Justification.SpaceBetween:
                            offset += m_padding.bottom;
                            leftover = _rect.rect.size.y - contentHeight - m_padding.top - m_padding.bottom;

                            float count = childEndIdx - childStartIdx + 1;
                            if(count > 1)
                                spacing = leftover / (count - ignoreCount - 1);

                            for(int i = childStartIdx; i <= childEndIdx; i++) {
                                ChildInfo c = _children[i];
                                // skip disabled/ignore items
                                if(CheckIgnoreElem(c))
                                    continue;

                                SetAnchorY(c.rect, 0);

                                if(index != 0) {
                                    offset += spacing;
                                }

                                float pivot = c.size.y * c.rect.pivot.y;
                                offset += pivot + c.margins.bottom;
                                c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset);
                                offset += c.size.y - pivot + c.margins.top;

                                index++;
                            }
                            break;
                    }
                    break;
            }
        }

        private void LayoutRowAligned(ChildInfo c, float offset, float lineHeight) {
            float pivot = c.size.y * c.rect.pivot.y;
            float size = c.size.y + c.margins.top + c.margins.bottom;
            float leftover = lineHeight - size;
                                
            switch(m_alignItems) {
                case Alignment.Start:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset - c.margins.top - (c.size.y - pivot));
                    break;
                case Alignment.Center:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset - leftover / 2 - c.margins.top - (c.size.y - pivot));
                    break;
                case Alignment.End:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetY(offset - leftover - c.margins.top - (c.size.y - pivot));
                    break;
            }
        }

        private void LayoutColumnAligned(ChildInfo c, float offset, float columnWidth) {
            float pivot = c.size.x * c.rect.pivot.x;
            float size = c.size.x + c.margins.left + c.margins.right;
            float leftover = columnWidth - size;
                                
            switch(m_alignItems) {
                case Alignment.Start:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset + c.margins.left + pivot);
                    break;
                case Alignment.Center:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset + leftover / 2 + c.margins.left + pivot);
                    break;
                case Alignment.End:
                    c.rect.anchoredPosition = c.rect.anchoredPosition.SetX(offset + leftover + c.margins.left + pivot);
                    break;
            }
        }
        
        private void GrowChildrenHorizontal() {
            if(_growChildCount.x == 0) return;
            
            Log($"growing {_growChildCount.x} children horizontally (rect: {_rect.rect.size.x}, inner: {_innerSize.x}, content: {_contentSize.x})");
            
            float size;
            float crossSize;
            float leftover = 0;
            float flexTotal = 0;
            
            switch(m_direction) {
                // GROW HORIZONTAL --> PRIMARY AXIS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    // save total flex sum for size distribution
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.x != SizingMode.Grow)
                            continue;

                        flexTotal += c.li.flexibleWidth;
                    }
                    
                    leftover = _rect.rect.size.x - _contentSize.x - m_padding.left - m_padding.right;
                    // Log($"free space: {leftover}");
                    
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.x != SizingMode.Grow)
                            continue;
                        
                        size = leftover * (c.li.flexibleWidth / flexTotal) - c.margins.left - c.margins.right;
                        if(c.li.UseMaxWidth) size = Mathf.Min(c.li.preferredWidth, size);
                        if(c.li.UseMinWidth) size = Mathf.Max(c.li.minWidth, size);
                        
                        Log($"growing \"{c.li.name}\" x axis ({size}) - margins: {c.margins.left}, {c.margins.right}");
                        
                        c.size.x = size;
                        _contentSize.x += size + c.margins.left + c.margins.right;

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.x, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);

                            // special case for text growing
                            if(c.li is LayoutText t) {
                                float oldSize = c.size.y;
                                t.HandleGrowSizingX();
                                float diff = c.rect.rect.size.y * (m_ignoreChildScale ? 1 : c.rect.localScale.y) - oldSize;
                                // text resized vertically bc of growth
                                if(!Mathf.Approximately(0, diff)) {
                                    c.size.y = oldSize + diff;
                                    GrowSizingXCallback(diff);
                                }
                            }
                        }
                    }
                    break;
                // GROW HORIZONTAL --> CROSS AXIS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    crossSize = _rect.rect.size.x - m_padding.left - m_padding.right;

                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.x != SizingMode.Grow)
                            continue;
                        
                        size = crossSize - c.margins.left - c.margins.right;
                        if(c.li.UseMaxWidth) size = Mathf.Min(c.li.preferredWidth, size);
                        if(c.li.UseMinWidth) size = Mathf.Max(c.li.minWidth, size);
                        
                        Log($"growing \"{c.li.name}\" x axis ({size})");
                        
                        c.size.x = size;
                        _contentSize.x = Mathf.Max(size + c.margins.left + c.margins.bottom, _contentSize.x);

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.x, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);

                            // special case for text growing
                            if(c.li is LayoutText t) {
                                float oldSize = c.size.y;
                                t.HandleGrowSizingX();
                                float diff = c.rect.rect.size.y * (m_ignoreChildScale ? 1 : c.rect.localScale.y) - oldSize;
                                // text resized vertically bc of growth
                                if(!Mathf.Approximately(0, diff)) {
                                    c.size.y = oldSize + diff;
                                    GrowSizingXCallback(diff);
                                }
                            }
                        }
                    }
                    break;
            }
        
        }
        
        private void GrowChildrenVertical() {
            if(_growChildCount.y == 0) return;
            
            Log($"growing {_growChildCount.y} children vertically (rect: {_rect.rect.size.y}, inner: {_innerSize.y}, content: {_contentSize.y})");
            
            float size;
            float crossSize;
            float leftover = 0;
            float flexTotal = 0;
            
            switch(m_direction) {
                // GROW VERTICAL --> CROSS AXIS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    crossSize = _rect.rect.size.y - m_padding.top - m_padding.bottom;

                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.y != SizingMode.Grow)
                            continue;
                        
                        size = crossSize - c.margins.top - c.margins.bottom;
                        if(c.li.UseMaxHeight) size = Mathf.Min(c.li.preferredHeight, size);
                        if(c.li.UseMinHeight) size = Mathf.Max(c.li.minHeight, size);
                        
                        Log($"growing \"{c.li.name}\" y axis ({size})");
                        
                        c.size.y = size;
                        _contentSize.y = Mathf.Max(size + c.margins.top + c.margins.bottom, _contentSize.y);

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.y, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                        }
                    }
                    break;
                // GROW VERTICAL --> PRIMARY AXIS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    // save total flex sum for size distribution
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.y != SizingMode.Grow)
                            continue;

                        flexTotal += c.li.flexibleHeight;
                    }
                    
                    leftover = _rect.rect.size.y - _contentSize.y - m_padding.top - m_padding.bottom;
                    
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.y != SizingMode.Grow)
                            continue;
                        
                        size = leftover * (c.li.flexibleHeight / flexTotal) - c.margins.top - c.margins.bottom;
                        if(c.li.UseMaxHeight) size = Mathf.Min(c.li.preferredHeight, size);
                        if(c.li.UseMinHeight) size = Mathf.Max(c.li.minHeight, size);
                        
                        Log($"growing \"{c.li.name}\" y axis ({size})");
                        c.size.y = size;
                        _contentSize.y += size + c.margins.top + c.margins.bottom;

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.y, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                        }
                    }
                    break;
            }
        }
        
        private void PackLines() {
            _lines.Clear();

            LineInfo line = new LineInfo { firstItemIdx = 0 };
            
            float cursor = 0;
            int lineIndex = 0;
            bool newLine = true;
            
            foreach(ChildInfo c in _children) {
                if(CheckIgnoreElem(c)) {
                    line.ignoreCount++;
                    continue;
                }

                bool grow = c.li && c.li.Sizing.x == SizingMode.Grow;
                bool crossGrow = c.li && c.li.Sizing.y == SizingMode.Grow;

                // Grow child: occupies a line on its own. If current active line is not empty, close it first,
                // take grow to a new line and close immediately. Thus grow takes ownership of the entire line.
                if(grow) {
                    if(line.firstItemIdx == c.index) {
                        line.lastItemIdx = c.index;
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                    }
                    else {
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo {
                            firstItemIdx = c.index,
                            lastItemIdx = c.index,
                            itemCount = 1,
                            primarySize = 0,
                            crossSize = crossGrow ? 0 : c.size.y + c.margins.top + c.margins.bottom
                        };
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                    }

                    cursor = 0;
                    continue;
                }

                float childH = c.size.x + c.margins.left + c.margins.right;
                float childV = crossGrow ? 0 : c.size.y + c.margins.top + c.margins.bottom;
                
                float candidate = childH + 
                                  (newLine || c.index == _children.Count-1 || m_justifyContent == Justification.SpaceBetween ? 0 : m_innerSpacing);

                if(cursor + candidate > _innerSize.x) {
                    // this element is the first item AND too big
                    if(line.firstItemIdx == c.index) {
                        line.itemCount = 1;
                        line.primarySize = _innerSize.x;
                        line.crossSize = childV;
                        line.lastItemIdx = c.index;
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                        cursor = 0;
                        newLine = true;
                    }
                    // this element runs off the end of the line normally
                    else {
                        if(lineIndex != 0 && m_justifyContent != Justification.SpaceBetween) line.primarySize -= m_innerSpacing;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo {
                            firstItemIdx = c.index,
                            lastItemIdx = c.index,
                            itemCount = 1,
                            primarySize = candidate,
                            crossSize = childV
                        };
                        c.lineIndex = lineIndex;
                        cursor = candidate;
                        newLine = false;
                    }
                }
                else {
                    c.lineIndex = lineIndex;
                    line.lastItemIdx = c.index;
                    line.primarySize += candidate;
                    line.crossSize = Mathf.Max(line.crossSize, childV);
                    line.itemCount++;
                    cursor += candidate;
                    newLine = false;
                }
            }
            
            _lines.Add(line);

            Log($"packed {_lines.Count} lines");
            int index = 0;
            // foreach(LineInfo l in _lines) {
            //     Log($"line {index}: {l.itemCount} items - {l.primarySize}, {l.crossSize}");
            //     index++;
            // }
            
            
            float maxLineSize = 0;
            foreach(LineInfo l in _lines) {
                maxLineSize = Mathf.Max(maxLineSize, l.primarySize);
            }
            _contentSize.x = maxLineSize;

            float total = 0;
            index = 0;
            foreach(LineInfo l in _lines) {
                total += l.crossSize + (index == _lines.Count - 1 ? 0 : m_lineSpacing);
                index++;
            }
            _contentSize.y = total;
            _precalcYSize = true;
            
            // apply fit sizing X
            if(m_sizing.x == SizingMode.FitContent) {
                float size = _contentSize.x + m_padding.left + m_padding.right;
                if(m_useMaxWidth) size = Mathf.Min(m_maxWidth, size);
                if(m_useMinWidth) size = Mathf.Max(m_minWidth, size);
                
                _rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    size
                );
            }
        }

        private void PackColumns() {
            _lines.Clear();

            LineInfo line = new LineInfo { firstItemIdx = 0 };
            
            float cursor = 0;
            int lineIndex = 0;
            bool newLine = true;
            
            foreach(ChildInfo c in _children) {
                if(CheckIgnoreElem(c)) {
                    line.ignoreCount++;
                    continue;
                }

                bool grow = c.li && c.li.Sizing.y == SizingMode.Grow;
                bool crossGrow = c.li && c.li.Sizing.x == SizingMode.Grow;

                // Grow child: occupies a line on its own. If current active line is not empty, close it first,
                // take grow to a new line and close immediately. Thus grow takes ownership of the entire line.
                if(grow) {
                    if(line.firstItemIdx == c.index) {
                        line.lastItemIdx = c.index;
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                    }
                    else {
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo {
                            firstItemIdx = c.index,
                            lastItemIdx = c.index,
                            itemCount = 1,
                            primarySize = 0,
                            crossSize = crossGrow ? 0 : c.size.x + c.margins.left + c.margins.right
                        };
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                    }

                    cursor = 0;
                    continue;
                }

                float childH = crossGrow ? 0 : c.size.x + c.margins.left + c.margins.right;
                float childV = c.size.y + c.margins.top + c.margins.bottom;
                
                float candidate = childV + 
                                  (newLine || c.index == _children.Count-1 || m_justifyContent == Justification.SpaceBetween ? 0 : m_innerSpacing);

                if(cursor + candidate > _innerSize.y) {
                    // this element is the first item AND too big
                    if(line.firstItemIdx == c.index) {
                        line.itemCount = 1;
                        line.primarySize = _innerSize.y;
                        line.crossSize = childH;
                        line.lastItemIdx = c.index;
                        c.lineIndex = lineIndex;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo { firstItemIdx = c.index+1 };
                        cursor = 0;
                        newLine = true;
                    }
                    // this element runs off the end of the line normally
                    else {
                        if(lineIndex != 0 && m_justifyContent != Justification.SpaceBetween) line.primarySize -= m_innerSpacing;
                        _lines.Add(line);
                        lineIndex++;
                        line = new LineInfo {
                            firstItemIdx = c.index,
                            lastItemIdx = c.index,
                            itemCount = 1,
                            primarySize = candidate,
                            crossSize = childH
                        };
                        c.lineIndex = lineIndex;
                        cursor = candidate;
                        newLine = false;
                    }
                }
                else {
                    c.lineIndex = lineIndex;
                    line.lastItemIdx = c.index;
                    line.primarySize += candidate;
                    line.crossSize = Mathf.Max(line.crossSize, childH);
                    line.itemCount++;
                    cursor += candidate;
                    newLine = false;
                }
            }
            
            _lines.Add(line);

            Log($"packed {_lines.Count} columns");
            int index = 0;
            // foreach(LineInfo l in _lines) {
            //     Log($"line {index}: {l.itemCount} items - {l.primarySize}, {l.crossSize}");
            //     index++;
            // }
            
            float maxLineSize = 0;
            foreach(LineInfo l in _lines) {
                maxLineSize = Mathf.Max(maxLineSize, l.primarySize);
            }
            _contentSize.y = maxLineSize;

            float total = 0;
            index = 0;
            foreach(LineInfo l in _lines) {
                total += l.crossSize + (index == 0 ? 0 : m_lineSpacing);
                index++;
            }
            _contentSize.x = total;

            if(m_sizing.y == SizingMode.FitContent) {
                float size = _contentSize.y + m_padding.bottom + m_padding.top;
                if(m_useMaxHeight) size = Mathf.Min(m_maxHeight, size);
                if(m_useMinHeight) size = Mathf.Max(m_minHeight, size);
                
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }
            
            // apply fit sizing X
            if(m_sizing.x == SizingMode.FitContent) {
                float size = _contentSize.x + m_padding.left + m_padding.right;
                if(m_useMaxWidth) size = Mathf.Min(m_maxWidth, size);
                if(m_useMinWidth) size = Mathf.Max(m_minWidth, size);
                
                _rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    size
                );
            }
        }
        
        private void GrowChildrenHorizontalWrapped(float contentWidth) {
            Log("<color=white>GrowChildrenHorizontalWrapped</color>");
            switch(m_direction) {
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.x != SizingMode.Grow)
                            continue;
                        
                        float size = _rect.rect.size.x - m_padding.left - m_padding.right - c.margins.left - c.margins.right;
                        if(c.li.UseMaxWidth) size = Mathf.Min(c.li.preferredWidth, size);
                        if(c.li.UseMaxWidth) size = Mathf.Max(c.li.minWidth, size);
                        
                        Log($"growing \"{c.li.name}\" y axis ({size})");
                        c.size.x = size;
                        
                        // update line size
                        LineInfo line = _lines[c.lineIndex];
                        line.primarySize = size + c.margins.left + c.margins.right;
                        _lines[c.lineIndex] = line;

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.x, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                        }
                    }
                    break;
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    float leftover = _rect.rect.size.x - m_padding.left - m_padding.right - contentWidth;
                    
                    // get flex heights sum
                    float flexTotal = 0;
                    foreach(LineInfo line in _lines) {
                        if(line.itemCount > 1)
                            continue;

                        ChildInfo c = _children[line.firstItemIdx];
                        if(CheckIgnoreElem(c) || c.sizingX != SizingMode.Grow)
                            continue;

                        flexTotal += c.li.flexibleWidth;
                    }
                    
                    for(int l = 0; l < _lines.Count; l++) {
                        LineInfo line = _lines[l];
                        
                        if(line.itemCount > 1) {
                            for(int i = line.firstItemIdx; i <= line.lastItemIdx; i++) {
                                ChildInfo c = _children[i];
                                
                                if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.x != SizingMode.Grow)
                                    continue;

                                // Log($"line width: {line.crossSize}");
                                
                                float size = line.crossSize - c.margins.left - c.margins.right;
                                if(c.li.UseMaxWidth) size = Mathf.Min(c.li.preferredWidth, size);
                                if(c.li.UseMinWidth) size = Mathf.Max(c.li.minWidth, size);
                                
                                Log($"growing \"{c.li.name}\" x axis ({size})");

                                // size actually needs to change
                                if(!Mathf.Approximately(c.rect.rect.size.x, size)) {
                                    c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                                }
                            }
                        }
                        else {
                            ChildInfo c = _children[line.firstItemIdx];
                            if(CheckIgnoreElem(c) || c.sizingX != SizingMode.Grow)
                                continue;

                            float size = leftover * (c.li.flexibleWidth / flexTotal) - c.margins.left - c.margins.right;
                            if(c.li.UseMaxWidth) size = Mathf.Min(c.li.preferredWidth, size);
                            if(c.li.UseMaxWidth) size = Mathf.Max(c.li.minWidth, size);
                            
                            line.crossSize = size + c.margins.left + c.margins.right;
                            _lines[l] = line;
                            _contentSize.x += size + c.margins.left + c.margins.right;
                            
                            Log($"growing \"{c.li.name}\" x axis ({size})");
                            
                            // size actually needs to change
                            if(!Mathf.Approximately(c.rect.rect.size.x, size)) {
                                c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                            }
                        }
                    }
                    break;
            }
        }

        private void GrowChildrenVerticalWrapped(float contentHeight) {
            Log("<color=white>GrowChildrenVerticalWrapped</color>");
            
            float size;
            
            switch(m_direction) {
                // GROW VERTICAL --> CROSS AXIS
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    float leftover = _rect.rect.size.y - m_padding.top - m_padding.bottom - contentHeight;
                    
                    // get flex heights sum
                    float flexTotal = 0;
                    foreach(LineInfo line in _lines) {
                        if(line.itemCount > 1)
                            continue;

                        ChildInfo c = _children[line.firstItemIdx];
                        if(CheckIgnoreElem(c) || c.sizingY != SizingMode.Grow)
                            continue;

                        flexTotal += c.li.flexibleHeight;
                    }
                    
                    for(int l = 0; l < _lines.Count; l++) {
                        LineInfo line = _lines[l];
                        
                        if(line.itemCount > 1) {
                            for(int i = line.firstItemIdx; i <= line.lastItemIdx; i++) {
                                ChildInfo c = _children[i];
                                
                                if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.y != SizingMode.Grow)
                                    continue;

                                // Log($"line height: {line.crossSize}");
                                
                                size = line.crossSize - c.margins.top - c.margins.bottom;
                                if(c.li.UseMaxHeight) size = Mathf.Min(c.li.preferredHeight, size);
                                if(c.li.UseMinHeight) size = Mathf.Max(c.li.minHeight, size);
                                
                                Log($"growing \"{c.li.name}\" y axis ({size})");

                                // size actually needs to change
                                if(!Mathf.Approximately(c.rect.rect.size.y, size)) {
                                    c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                                }
                            }
                        }
                        else {
                            ChildInfo c = _children[line.firstItemIdx];
                            if(CheckIgnoreElem(c) || c.sizingY != SizingMode.Grow)
                                continue;

                            size = leftover * (c.li.flexibleHeight / flexTotal) - c.margins.top - c.margins.bottom;
                            if(c.li.UseMaxHeight) size = Mathf.Min(c.li.preferredHeight, size);
                            if(c.li.UseMinHeight) size = Mathf.Max(c.li.minHeight, size);
                            
                            line.crossSize = size + c.margins.top + c.margins.bottom;
                            _lines[l] = line;
                            _contentSize.y += size + c.margins.top + c.margins.bottom;
                            
                            Log($"growing \"{c.li.name}\" y axis ({size})");
                            
                            // size actually needs to change
                            if(!Mathf.Approximately(c.rect.rect.size.y, size)) {
                                c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                            }
                        }
                    }
                    break;
                // GROW VERTICAL --> PRIMARY AXIS
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    foreach(ChildInfo c in _children) {
                        if(!c.li || CheckIgnoreElem(c) || c.li.Sizing.y != SizingMode.Grow)
                            continue;
                        
                        size = _rect.rect.size.y - m_padding.top - m_padding.bottom - c.margins.top - c.margins.bottom;
                        if(c.li.UseMaxHeight) size = Mathf.Min(c.li.preferredHeight, size);
                        if(c.li.UseMinHeight) size = Mathf.Max(c.li.minHeight, size);
                        
                        Log($"growing \"{c.li.name}\" y axis ({size})");
                        c.size.y = size;
                        
                        // update line size
                        LineInfo line = _lines[c.lineIndex];
                        line.primarySize = size + c.margins.top + c.margins.bottom;
                        _lines[c.lineIndex] = line;

                        // size actually needs to change
                        if(!Mathf.Approximately(c.rect.rect.size.y, size)) {
                            c.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                        }
                    }
                    break;
            }
        }
        #endregion

        public void GrowSizingXCallback(float yDiff) {
            Log($"X Grow Callback ({yDiff})");
            // remove grow items from calculated content size
            foreach(ChildInfo c in _children) {
                if(CheckIgnoreElem(c))
                    continue;

                if(c.li && c.li.Sizing.y == SizingMode.Grow) {
                    _contentSize.y -= c.rect.rect.size.y;
                }
                else {
                    c.size.y = c.rect.rect.size.y;
                }
            }

            float oldSize = _contentSize.y;
            float oldHeight = _rect.rect.size.y;

            // recalculate content size
            switch(m_direction)
            {
                case LayoutDirection.Row:
                case LayoutDirection.RowReverse:
                    _contentSize.y = 0;
                    foreach(ChildInfo c in _children) {
                        if(CheckIgnoreElem(c) || (c.li && c.li.Sizing.y == SizingMode.Grow))
                            continue;

                        _contentSize.y = Mathf.Max(_contentSize.y, c.size.y);
                    }
                    break;
                case LayoutDirection.Column:
                case LayoutDirection.ColumnReverse:
                    _contentSize.y += yDiff;
                    break;
            }
            bool sizeChanged = !Mathf.Approximately(_contentSize.y, oldSize);

            if(m_sizing.y == SizingMode.FitContent && sizeChanged) {
                _rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    m_padding.top + m_padding.bottom + _contentSize.y
                );
            }

            Log($"old content: {oldSize}, old height: {oldHeight}\nnew content: {_contentSize.y}, new height: {_rect.rect.height}");
            
            if(_parent)
                _parent.GrowSizingXCallback(yDiff);

            if(!_dirty && sizeChanged) {
                Log("forcing vertical layout update from x grow callback");
                GrowChildrenVertical();
                VerticalLayout(0, _children.Count-1, _contentSize.y, _contentSize.y, _ignoreCount);
            }
        }

        public void RefreshChildCache() {
            _children.Clear();
            _lines.Clear();

            int childCount = transform.childCount;
            Log($"Refreshing child cache - {childCount} children detected");

            for(int i = 0; i < childCount; i++) {
                RectTransform rt = transform.GetChild(i).GetComponent<RectTransform>();
                if (rt == null) continue;
                
                LayoutItem li = rt.GetComponent<LayoutItem>();
                
                Log($"Adding child \"{rt.name}\" - size: {rt.rect.size}, layoutitem: {li != null}");
                
                _children.Add(
                    new ChildInfo
                    {
                        index = i,
                        rect = rt,
                        li = li,
                        margins = li ? li.Margins : default,
                        size = rt.rect.size * (m_ignoreChildScale ? Vector2.one : rt.localScale),
                        enabled = rt.gameObject.activeInHierarchy,
                        ignoreLayout = li && li.IgnoreLayout,
                    }
                );
            }

			SetDirty();
            LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }
    }
}
