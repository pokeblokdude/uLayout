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
        
        [Header("Transform")]
        [SerializeField] protected Vector2 m_offset;
        [SerializeField] protected float m_rotation;
        [SerializeField] protected Vector2 m_scale;

        public bool IgnoreLayout {
            get => m_ignoreLayout;
            set {
                m_ignoreLayout = value;
                if(_parent) {
                    _parent.RefreshChildCache();
                }
            }
        }
        public Vector2 Offset => m_offset;
        public RectTransform Rect => _rect;
        public float Rotation => m_rotation;
        public Vector2 Scale => m_scale;
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
