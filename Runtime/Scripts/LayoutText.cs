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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poke.UI
{
    [RequireComponent(typeof(TMP_Text))]
    // Inheriting from ILayoutSelfController is a workaround to receive layout events even when
    // there is no Layout parent. The alternative would be some sort of special deferred call
    // from TextDirty(), but this way is cleaner imo
    public class LayoutText : LayoutItem, ILayoutSelfController
    {
        private TMP_Text _text;
        private Vector2 _preferredSize;

        protected override bool FitOwnContent => true;
        protected override string LogPrefix => "LT";

        protected override void Awake() {
            base.Awake();
            _text = GetComponent<TMP_Text>();
        }

        protected override void OnEnable() {
            _text.RegisterDirtyLayoutCallback(TextDirty);
            _text.ForceMeshUpdate(true, true);
            
            _preferredSize = _text.GetPreferredValues();
            if(m_sizing.x == SizingMode.FitContent) FitTextHorizontal();
            _preferredSize = _text.GetPreferredValues();
            if(m_sizing.y == SizingMode.FitContent) FitTextVertical();
            
            base.OnEnable();
        }

        protected override void OnDisable() {
            _text.UnregisterDirtyLayoutCallback(TextDirty);
            base.OnDisable();
        }

        private void TextDirty() {
            if(!CanvasUpdateRegistry.IsRebuildingLayout()) {
                Log("<color=cyan>text changed</color>");
                SetDirty();
            }
        }

        private void FitTextHorizontal() {
            _text.textWrappingMode = m_useMaxWidth ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                
            float size = _preferredSize.x;
            if(m_useMaxWidth) size = Mathf.Min(m_maxWidth, size);
            if(m_useMinWidth) size = Mathf.Max(m_minWidth, size);
            Log($"fit text horizontal: {size}");
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        }

        private void FitTextVertical() {
            float size = _preferredSize.y;
            if(m_useMaxHeight) size = Mathf.Min(m_maxHeight, size);
            if(m_useMinHeight) size = Mathf.Max(m_minHeight, size);
            Log($"fit text vertical: {size}");
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }
        
        public override void CalculateLayoutInputHorizontal() {
            base.CalculateLayoutInputHorizontal();
            
            _preferredSize = _text.GetPreferredValues();
            Log($"text size: {_preferredSize}");
                
            if(m_sizing.x == SizingMode.FitContent) {
                FitTextHorizontal();
            }
        }

        public override void CalculateLayoutInputVertical() {
            base.CalculateLayoutInputVertical();
            
            _preferredSize = _text.GetPreferredValues();
            if(m_sizing.y == SizingMode.FitContent) {
                FitTextVertical();
            }
        }

        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }
    }
}
