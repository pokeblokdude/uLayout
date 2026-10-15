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
using System.Collections;
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
        public float minHeight => m_minHeight;
#if UNITY_6000_6_OR_NEWER
        public float maxWidth => m_maxWidth;
        public float maxHeight => m_maxHeight;
#endif
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
        
        // a plain item has nothing to fit; a Layout fits its children
        protected virtual bool FitsItsContent => false;
        
        protected RectTransform _rect;
        protected DrivenRectTransformTracker _tracker;
        protected DrivenTransformProperties _trackerProps;
        protected Layout _parent;
        protected bool _dirty = true;
        protected readonly Vector3[] _rectCorners = new Vector3[4];

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
        }

        protected virtual void OnEnable() {
            FindParent();
            RefreshTracker();

            _dirty = true;
            if(_parent) _parent.RefreshChildCache();
            else SetDirty();
        }

        protected virtual void OnDisable() {
#if UNITY_EDITOR
            _tracker.Clear();
#endif
            if(_parent) _parent.SetDirty();
        }

        protected virtual void OnTransformParentChanged() {
            if(!isActiveAndEnabled)
                return;

            FindParent();
            RefreshTracker();
            if(_parent) _parent.RefreshChildCache();
            else SetDirty();
        }

        // a child's resize reaches only itself (OnChildRectTransformDimensionsChange is not sent to MonoBehaviours),
        // so every item reports its own; during a rebuild the resize is the layout's own doing
        protected virtual void OnRectTransformDimensionsChange() {
            if(_parent && !CanvasUpdateRegistry.IsRebuildingLayout())
                MarkForRebuild();
        }

        protected virtual void OnDidApplyAnimationProperties() {
            SetDirty();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() {
            if(_rect && isActiveAndEnabled)
                MarkDirty();
        }
#endif

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
            if(m_log) Debug.Log($"[{Time.frameCount}] [LI:{gameObject.name}]: {msg}");
        }

        private void FindParent() {
            _parent = transform.parent ? transform.parent.GetComponent<Layout>() : null;
            if(!transform.parent) Debug.LogError("LayoutItem must be a child of a RectTransform!");
        }

        protected virtual void RefreshTracker() {
#if UNITY_EDITOR
            _tracker.Clear();
            _trackerProps = DrivenTransformProperties.None;
            SetDrivenProperties();
            _tracker.Add(this, _rect, _trackerProps);
#endif
        }

        // outside a Layout a growing item fills its parent through its anchors, so Unity keeps it filled
        protected void ApplyGrowOutsideLayout() {
            if(_parent)
                return;

            if(m_sizing.x == SizingMode.Grow && (_rect.anchorMin.x != 0 || _rect.anchorMax.x != 1 || _rect.offsetMin.x != 0 || _rect.offsetMax.x != 0)) {
                _rect.anchorMin = _rect.anchorMin.SetX(0);
                _rect.anchorMax = _rect.anchorMax.SetX(1);
                _rect.offsetMin = _rect.offsetMin.SetX(0);
                _rect.offsetMax = _rect.offsetMax.SetX(0);
            }
            if(m_sizing.y == SizingMode.Grow && (_rect.anchorMin.y != 0 || _rect.anchorMax.y != 1 || _rect.offsetMin.y != 0 || _rect.offsetMax.y != 0)) {
                _rect.anchorMin = _rect.anchorMin.SetY(0);
                _rect.anchorMax = _rect.anchorMax.SetY(1);
                _rect.offsetMin = _rect.offsetMin.SetY(0);
                _rect.offsetMax = _rect.offsetMax.SetY(0);
            }
        }

        protected void MarkForRebuild() {
            if(!isActiveAndEnabled)
                return;

            // a rebuild must not re-enter itself, so a change made during one is laid out next frame (as uGUI's LayoutGroup does)
            if(CanvasUpdateRegistry.IsRebuildingLayout())
                StartCoroutine(DelayedMarkForRebuild());
            else
                LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }

        private IEnumerator DelayedMarkForRebuild() {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }
        
        // claim only what something sets: Unity serializes a claimed value as zero
        protected virtual void SetDrivenProperties() {
            bool grownByLayout = !_parent || !m_ignoreLayout;
            if((m_sizing.x == SizingMode.FitContent && FitsItsContent) || (m_sizing.x == SizingMode.Grow && grownByLayout))
                _trackerProps |= DrivenTransformProperties.SizeDeltaX;
            if((m_sizing.y == SizingMode.FitContent && FitsItsContent) || (m_sizing.y == SizingMode.Grow && grownByLayout))
                _trackerProps |= DrivenTransformProperties.SizeDeltaY;

            if(!_parent && m_sizing.x == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchorMaxX | DrivenTransformProperties.AnchoredPositionX;
            if(!_parent && m_sizing.y == SizingMode.Grow)
                _trackerProps |= DrivenTransformProperties.AnchorMinY | DrivenTransformProperties.AnchorMaxY | DrivenTransformProperties.AnchoredPositionY;

            if(_parent && !m_ignoreLayout) 
                _trackerProps |= DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Anchors;
        }

        public virtual void SetDirty() {
            if(isActiveAndEnabled) {
                RefreshTracker();
                ApplyGrowOutsideLayout();
            }
            MarkDirty();
        }

        protected void MarkDirty() {
            _dirty = true;
            MarkForRebuild();
        }

        public virtual void CalculateLayoutInputHorizontal() {
            Log("<color=white>CalculateLayoutInputHorizontal</color>");
            RefreshTracker();
            ApplyGrowOutsideLayout();
        }

        public virtual void CalculateLayoutInputVertical() {
            Log("<color=white>CalculateLayoutInputVertical</color>");
        }
    }
}
