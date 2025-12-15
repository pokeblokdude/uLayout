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
using TMPro;
using UnityEngine;

namespace Poke.UI {
    [
        ExecuteAlways,
        RequireComponent(typeof(TMP_Text))
    ]
    public class LayoutText : LayoutItem
    {
        [Header("Text")]
        [SerializeField, Min(0)] private float m_maxFontSize;
        
        private TMP_Text _text;
        private DrivenRectTransformTracker _rectTracker;

        protected override void Awake() {
            base.Awake();
            _text = GetComponent<TMP_Text>();
            _rectTracker = new DrivenRectTransformTracker();
        }

        public override void Update() {
            base.Update();
            
            _text.textWrappingMode = m_sizing.x == SizingMode.Grow ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            bool fitX = m_sizing.x == SizingMode.FitContent && m_sizing.x != SizingMode.Grow;
            bool fitY = m_sizing.y == SizingMode.FitContent && m_sizing.y != SizingMode.Grow;
            
            _rectTracker.Clear();
            if(fitX)
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaX);
            if(fitY)
                _rectTracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaY);

            if(m_maxFontSize > 0) {
                _text.fontSizeMax = m_maxFontSize;
            }
            Vector2 size = _text.GetPreferredValues(_text.text);
            
            // X Pass
            if(fitX) {
                _rect.sizeDelta = _rect.sizeDelta.With(size.x);
            }
            
            // Y Pass
            if(fitY) {
                float height = 0;
                for(int i = 0; i < _text.textInfo.lineCount; i++) {
                    float lineHeight = _text.textInfo.lineInfo[i].lineHeight;
                    height += lineHeight;
                }
                size.y = height;
                _rect.sizeDelta = _rect.sizeDelta.With(y: size.y);
            }
        }
    }
}
