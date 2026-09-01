using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // Decorates the already-localized native caption, without changing its localizer or click event.
    internal sealed class ComputerLeaveHint : IDisposable
    {
        private readonly Button _button;
        private readonly TMP_Text _label;
        private readonly bool _autoSizing;
        private readonly float _fontSize, _minSize, _maxSize, _hintMin, _hintMax;
        private string _original, _applied, _shortcut;
        private bool _disposed;
        internal bool Uses(Button button) => !_disposed && _button == button && _label != null;

        internal ComputerLeaveHint(Button button)
        {
            _button = button;
            _label = button.GetComponentInChildren<TMP_Text>(true);
            if (_label == null) return;
            _autoSizing = _label.enableAutoSizing;
            _fontSize = _label.fontSize; _minSize = _label.fontSizeMin; _maxSize = _label.fontSizeMax;
            _hintMax = _autoSizing ? _maxSize : _fontSize;
            _hintMin = Mathf.Min(12, _hintMax);
            _label.fontSizeMin = _hintMin; _label.fontSizeMax = _hintMax;
            _label.enableAutoSizing = true;
        }

        internal void Refresh(string shortcut)
        {
            if (_disposed || _label == null || string.IsNullOrEmpty(_label.text)) return;
            // Native localization may replace the caption at any time, including a language change.
            if (_label.text != _applied) _original = _label.text;
            if (string.IsNullOrEmpty(_original)) return;
            shortcut = shortcut ?? string.Empty;
            string applied = _original + " [" + shortcut + "]";
            if (_shortcut == shortcut && _label.text == applied) return;
            _shortcut = shortcut; _applied = applied; _label.text = _applied;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_label == null) return;
            if (_label.text == _applied) _label.text = _original;
            if (_label.enableAutoSizing && _label.fontSizeMin == _hintMin && _label.fontSizeMax == _hintMax)
            {
                _label.fontSizeMin = _minSize; _label.fontSizeMax = _maxSize;
                _label.enableAutoSizing = _autoSizing; _label.fontSize = _fontSize;
            }
        }
    }
}
