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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Poke.UI
{
    [
        ExecuteAlways,
        DisallowMultipleComponent,
        RequireComponent(typeof(RectTransform))
    ]
    public class LayoutItem : UIBehaviour, ILayoutElement, ILayoutIgnorer
    {
        [SerializeField] protected bool             m_log;
        [SerializeField] protected bool             m_ignoreLayout = false;
        [SerializeField] protected SizeModes        m_sizing;
        [SerializeField, Min(0)] protected float    m_minWidth;
        [SerializeField] protected bool             m_useMinWidth;
        [SerializeField, Min(0)] protected float    m_minHeight;
        [SerializeField] protected bool             m_useMinHeight;
        [SerializeField, Min(0)] protected float    m_maxWidth;
        [SerializeField] protected bool             m_useMaxWidth;
        [SerializeField, Min(0)] protected float    m_maxHeight;
        [SerializeField] protected bool             m_useMaxHeight;
        [SerializeField] protected float            m_flexWidth = 1;
        [SerializeField] protected float            m_flexHeight = 1;
        [SerializeField] protected Margins          m_margins;
        
        public float minWidth => m_minWidth;
        public float minHeight => m_minHeight;
#if UNITY_6000_6_OR_NEWER
        public float maxWidth => m_maxWidth;
        public float maxHeight => m_maxHeight;
#endif
        public float flexibleWidth => m_flexWidth; // relative "weight" of this item in the horizontal layout
        public float flexibleHeight => m_flexHeight; // relative "weight" of this item in the vertical layout
        [Obsolete("Included for compatibility with built-in layout components. Use LayoutItem.IgnoreLayout instead.")]
        public bool ignoreLayout => m_ignoreLayout;
        public float preferredWidth => m_maxWidth;
        public float preferredHeight => m_maxHeight;
        public int layoutPriority => _layoutPriority; // useless in uLayout (bc only one UL component per object)
        
        private float _preferredWidth, _preferredHeight;
        private int _layoutPriority;
        
        #region Properties
        public bool IgnoreLayout
        {
            get => m_ignoreLayout;
            set {
                m_ignoreLayout = value;
                SetDirty();
            }
        }
        public Margins Margins
        {
            get => m_margins;
            set {
                m_margins = value;
                SetDirty();
            }
        }
        public SizeModes Sizing {
            get => m_sizing;
            set {
                m_sizing = value;
                SetDirty();
            }
        }
        public bool UseMinWidth {
            get => m_useMinWidth;
            set {
                m_useMinWidth = value;
                SetDirty();
            }
        }
        public bool UseMinHeight {
            get => m_useMinHeight;
            set {
                m_useMinHeight = value;
                SetDirty();
            }
        }
        public bool UseMaxWidth {
            get => m_useMaxWidth;
            set {
                m_useMaxWidth = value;
                SetDirty();
            }
        }
        public bool UseMaxHeight {
            get => m_useMaxHeight;
            set {
                m_useMaxHeight = value;
                SetDirty();
            }
        }
        public RectTransform Rect => _rect;
        public DrivenTransformProperties TrackerProps => _trackerProps;
        #endregion
        
        // a plain item has nothing to fit; a Layout fits its children
        protected virtual bool FitOwnContent => false;
        protected virtual string LogPrefix => "LI";
        
        protected RectTransform _rect;
        protected DrivenRectTransformTracker _tracker;
        protected Layout _parent;
        protected readonly Vector3[] _rectCorners = new Vector3[4];

        private Canvas _canvas;
        private RectTransform _parentRect;
        private DrivenTransformProperties _trackerProps;
        protected int _refresh;

		private Canvas Canvas => _canvas ? _canvas : _canvas = GetComponentInParent<Canvas>();

        [Serializable]
        public struct SizeModes : IEquatable<SizeModes>
        {
            public SizingMode x;
            public SizingMode y;
            
            public bool Equals(SizeModes other) {
                return x == other.x && y == other.y;
            }
        }

        #region LayoutItem MonoBehavior
        protected virtual void OnDrawGizmosSelected() {
            _rect.GetWorldCorners(_rectCorners);

            Matrix4x4 ltw = _rect.localToWorldMatrix;
            
            foreach(Vector3 v in _rectCorners) {
                LayoutUtil.DrawCenteredDebugBox(v, 8f * Canvas.transform.localScale.x, 8f * Canvas.transform.localScale.y, Color.red);
            }

            Rect r = new Rect(_rectCorners[0], _rectCorners[2] - _rectCorners[0]);
            LayoutUtil.DrawDebugBox(r, _rect.position.z, Color.white);

            if(m_margins.top != 0 || m_margins.bottom != 0 || m_margins.left != 0 || m_margins.right != 0) {
                r.position -= (Vector2)(ltw * new Vector2(m_margins.left, m_margins.bottom));
                r.size += (Vector2)(ltw * new Vector2(m_margins.left + m_margins.right, m_margins.top + m_margins.bottom));
                LayoutUtil.DrawDebugBox(r, _rect.position.z, Color.orange);
            }
        }
        
        protected override void Awake() {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _tracker = new DrivenRectTransformTracker();
        }

        protected override void OnEnable() {
            Log("<color=cyan>enable</color>");
            Canvas.preWillRenderCanvases += PreLayout;
            
            if(transform.parent) {
                _parentRect = transform.parent.GetComponent<RectTransform>();
                _parent = transform.parent.GetComponent<Layout>();
            }
            if(!_parentRect) Debug.LogError("LayoutItem must be a child of a RectTransform!");
            
            if(_parent) _parent.QueueCacheRefresh();
            if(!m_ignoreLayout) SetDirty();
        }

        protected override void OnDisable() {
            Canvas.preWillRenderCanvases -= PreLayout;
            if(_parent) _parent.QueueCacheRefresh();
        }

        protected void PreLayout() {
            _refresh = 0;
        }

#if UNITY_EDITOR
        // react to inspector value changes
        protected override void OnValidate() {
            if(!m_ignoreLayout && isActiveAndEnabled) {
                Log("<color=cyan>inspector values changed</color>");
                SetDirty();
            }
        }
#endif
        
        // react to animated value changes
        protected override void OnDidApplyAnimationProperties() {
            if(!m_ignoreLayout) {
                Log("<color=cyan>animation properties applied</color>");
                SetDirty();
            }
        }
        #endregion
        
        protected void Log(object msg) {
            if(Debug.isDebugBuild && m_log) Debug.Log($"[{Time.frameCount}] [{LogPrefix}:{gameObject.name}]: {msg}");
        }
        
        protected void SetDrivenProperties() {
            Log("set driven RectTransform properties");
            _tracker.Clear();
            _trackerProps = DrivenTransformProperties.None;
            
            if((m_sizing.x == SizingMode.FitContent && FitOwnContent) || m_sizing.x == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.SizeDeltaX;
            if((m_sizing.y == SizingMode.FitContent && FitOwnContent) || m_sizing.y == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.SizeDeltaY;

            if(_parent && !m_ignoreLayout) 
                _trackerProps |= DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Anchors;

            if(!_parent && m_sizing.x == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchorMaxX | DrivenTransformProperties.AnchoredPositionX;
            if(!_parent && m_sizing.y == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.AnchorMinY | DrivenTransformProperties.AnchorMaxY | DrivenTransformProperties.AnchoredPositionY;
            
            _tracker.Add(this, _rect, _trackerProps);
        }

        #region ILayoutElement
        public virtual void CalculateLayoutInputHorizontal() {
            if(_refresh > 0) return;
            
            Log("<color=green>CalculateLayoutInputHorizontal</color>");
#if UNITY_EDITOR
            SetDrivenProperties();
#endif
            // Do grow sizing here if parent is not a Layout
            if(!_parent && _parentRect && m_sizing.x == SizingMode.Grow) {
                Log("grow horizontal (non-layout)");
                float parentSize = _parentRect.rect.size.x;
                _rect.anchorMin = _rect.anchorMin.SetX(0.5f);
                _rect.anchorMax = _rect.anchorMax.SetX(0.5f);
                _rect.anchoredPosition = _rect.anchoredPosition.SetX(Mathf.LerpUnclamped(-parentSize/2, parentSize/2, _rect.pivot.x));
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _parentRect.rect.size.x);
            }
        }

        public virtual void CalculateLayoutInputVertical() {
            if(_refresh > 0) return;
            
            Log("<color=green>CalculateLayoutInputVertical</color>");
            // Do grow sizing here if parent is not a Layout
            if(!_parent && _parentRect && m_sizing.y == SizingMode.Grow) {
                Log("grow vertical (non-layout)");
                float parentSize = _parentRect.rect.size.y;
                _rect.anchorMin = _rect.anchorMin.SetY(0.5f);
                _rect.anchorMax = _rect.anchorMax.SetY(0.5f);
                _rect.anchoredPosition = _rect.anchoredPosition.SetY(Mathf.LerpUnclamped(-parentSize/2, parentSize/2, _rect.pivot.y));
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _parentRect.rect.size.y);
            }

            _refresh++;
        }
        #endregion
        
        public virtual void SetDirty() {
            if(!CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }
    }
}
