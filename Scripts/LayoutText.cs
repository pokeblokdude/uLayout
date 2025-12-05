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
