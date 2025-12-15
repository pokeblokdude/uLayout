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
using UnityEngine;

namespace Poke.UI
{
    [
        ExecuteAlways,
        RequireComponent(typeof(RectTransform))
    ]
    public class LayoutItem : MonoBehaviour
    {
        [SerializeField] protected bool m_ignoreLayout = false;
        
        [Header("Sizing")]
        [SerializeField] protected SizeModes m_sizing;

        public bool IgnoreLayout {
            get => m_ignoreLayout;
            set {
                m_ignoreLayout = value;
                if(_parent) {
                    _parent.RefreshChildCache();
                }
            }
        }
        public RectTransform Rect => _rect;
        public SizeModes SizeMode => m_sizing;

        protected RectTransform _rect;
        protected RectTransform _parentRect;
        protected Layout _parent;

        [Serializable]
        public struct SizeModes
        {
            public SizingMode x;
            public SizingMode y;
        }

        protected virtual void Awake() {
            _rect = GetComponent<RectTransform>();
            _parentRect = transform.parent.GetComponent<RectTransform>();
        }

        protected virtual void OnEnable() {
            _parent = transform.parent.GetComponent<Layout>();
            if(_parent) {
                _parent.RefreshChildCache();
            }
        }

        protected virtual void OnDisable() {
            if(_parent) {
                _parent.RefreshChildCache();
            }
        }

        public virtual void Update() {
            // Do grow sizing here if parent is not a Layout
            if(!_parent && m_sizing.x == SizingMode.Grow) {
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _parentRect.rect.size.x);
            }
            if(!_parent && m_sizing.y == SizingMode.Grow) {
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _parentRect.rect.size.y);
            }
        }
    }
}
