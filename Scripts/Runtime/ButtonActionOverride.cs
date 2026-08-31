using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // Retain the complete native event (including persistent listeners) for exact restoration.
    internal sealed class ButtonActionOverride : IDisposable
    {
        private Button _button;
        private Button.ButtonClickedEvent _original;
        private Button.ButtonClickedEvent _replacement;

        internal void Bind(Button button, UnityAction action)
        {
            if (ReferenceEquals(_button, button)) return;
            Dispose();
            if (button == null) return;
            _button = button;
            _original = button.onClick;
            _replacement = new Button.ButtonClickedEvent();
            _replacement.AddListener(action);
            button.onClick = _replacement;
        }

        public void Dispose()
        {
            // Do not overwrite a newer event installed by the game or another mod.
            if (_button != null && ReferenceEquals(_button.onClick, _replacement))
                _button.onClick = _original;
            _replacement?.RemoveAllListeners();
            _button = null; _original = null; _replacement = null;
        }
    }
}
