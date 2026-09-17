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
using UnityEngine.UI;

namespace Poke.UI
{
    [
        ExecuteAlways,
        RequireComponent(typeof(RectTransform))
    ]
    public class LayoutItem : MonoBehaviour, ILayoutElement
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
        public float maxWidth => m_maxWidth;
        public float minHeight => m_minHeight;
        public float maxHeight => m_maxHeight;
        public float flexibleWidth => m_flexWidth; // relative "weight" of this item in the horizontal layout
        public float flexibleHeight => m_flexHeight; // relative "weight" of this item in the vertical layout
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
        public SizeModes Sizing => m_sizing;
        #endregion
        
        protected RectTransform _rect;
        protected DrivenRectTransformTracker _tracker;
        protected DrivenTransformProperties _trackerProps;
        protected Layout _parent;
        protected bool _dirty = true;
        protected int _frame;
        protected readonly Vector3[] _rectCorners = new Vector3[4];

        private RectTransform _parentRect;
        private Vector2 _parentSize;
        private Canvas _canvas;

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
        protected virtual void Awake() {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _tracker = new DrivenRectTransformTracker();

            _parentSize = _parentRect ? _parentRect.rect.size : default;
        }

        protected virtual void OnEnable() {
            if(transform.parent) {
                _parentRect = transform.parent.GetComponent<RectTransform>();
                _parent = transform.parent.GetComponent<Layout>();
            }
            else { Debug.LogError("LayoutItem must be a child of a RectTransform!"); }
            
            _trackerProps = DrivenTransformProperties.None;
            
			_dirty = true;
			if(_parent) _parent.RefreshChildCache();
        }

        public virtual void Update() {
            //Log("update");
            _frame = Time.frameCount;

#if UNITY_EDITOR
            _tracker.Clear();
            _trackerProps = DrivenTransformProperties.None;

            SetDrivenProperties();

            _tracker.Add(this, _rect, _trackerProps);
#endif

            // Do grow sizing here if parent is not a Layout
            if(!_parent && _parentRect) {
                // only update size if parent size has changed
                if(m_sizing.x == SizingMode.Grow && !Mathf.Approximately(_parentRect.rect.size.x, _parentSize.x)) {
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _parentRect.rect.size.x);
                    _parentSize = _parentSize.SetX(_parentRect.rect.size.x);
                }
                if(m_sizing.y == SizingMode.Grow && !Mathf.Approximately(_parentRect.rect.size.y, _parentSize.y)) {
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _parentRect.rect.size.y);
                    _parentSize = _parentSize.SetY(_parentRect.rect.size.y);
                }
            }
        }
        
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
        #endregion

        private void Log(object msg) {
            if(m_log) Debug.Log($"[{_frame}] [LI:{gameObject.name}]: {msg}");
        }
        
        protected virtual void SetDrivenProperties() {
            if((m_sizing.x == SizingMode.FitContent && transform.childCount > 0) || m_sizing.x == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.SizeDeltaX;
            if((m_sizing.y == SizingMode.FitContent && transform.childCount > 0) || m_sizing.y == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.SizeDeltaY;

            if(_parent && !m_ignoreLayout) 
                _trackerProps |= DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Anchors;
        }

        public virtual void SetDirty() {
            _dirty = true;
            if(_parent) {
                _parent.SetDirty();
            }
        }

        public virtual void CalculateLayoutInputHorizontal() {
            Log("<color=white>CalculateLayoutInputHorizontal</color>");
        }

        public virtual void CalculateLayoutInputVertical() {
            Log("<color=white>CalculateLayoutInputVertical</color>");
        }
    }
}
