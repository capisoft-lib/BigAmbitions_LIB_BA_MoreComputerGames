using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // A session-owned sibling of the native Leave button; never part of a game's render texture.
    internal sealed class ComputerReturnButton : IDisposable
    {
        private readonly Button _template, _button;
        private readonly TMP_Text _label;
        private readonly RectTransform _rect;
        private readonly LayoutElement _layout;
        private readonly Action _return;
        private readonly Func<bool> _allowed;
        private readonly float _height;
        private bool _disposed;
        internal Button Button => _button;
        internal bool Uses(Button template) => !_disposed && _button != null && _template == template;

        internal ComputerReturnButton(Button template, Action returnToMenu, Func<bool> allowed, Action<Button> prepare = null)
        {
            _template = template;
            _return = returnToMenu;
            _allowed = allowed;
            var staging = new GameObject("MCG_ButtonStaging"); staging.SetActive(false);
            try
            {
                _button = UnityEngine.Object.Instantiate(template, staging.transform, false);
                _button.name = "MCG_ReturnToMenu";
                _button.gameObject.SetActive(false);
                // Replacing the entire event also removes persistent native Leave listeners.
                _button.onClick = new Button.ButtonClickedEvent();
                _button.onClick.AddListener(Click);
                _button.navigation = new Navigation { mode = Navigation.Mode.None };
                prepare?.Invoke(_button);
                _label = _button.GetComponentInChildren<TMP_Text>(true);
                if (_label == null) throw new InvalidOperationException("Native Leave button has no text label.");
                _label.enableAutoSizing = true;
                _label.fontSizeMax = _label.fontSize;
                _label.fontSizeMin = Mathf.Min(12, _label.fontSizeMax);
                // Shared by the Unity 2022 SDK TMP and the game's newer TMP version.
#pragma warning disable CS0618
                _label.enableWordWrapping = false;
#pragma warning restore CS0618
                _label.raycastTarget = false;
                _rect = (RectTransform)_button.transform;
                _height = ((RectTransform)template.transform).rect.height;
                _layout = _button.GetComponent<LayoutElement>() ?? _button.gameObject.AddComponent<LayoutElement>();
                _layout.flexibleWidth = 0;
                _button.transform.SetParent(template.transform.parent, false);
                _button.transform.SetSiblingIndex(template.transform.GetSiblingIndex());
            }
            catch
            {
                if (_button != null) UnityEngine.Object.Destroy(_button.gameObject);
                throw;
            }
            finally { UnityEngine.Object.Destroy(staging); }
        }

        internal void Refresh(bool visible)
        {
            if (_disposed || _button == null || _template == null) return;
            string text = ComputerGames.ResolveText("bacg_return_menu", "Return to menu [Backspace]");
            if (_label.text != text) _label.text = text;
            _button.gameObject.SetActive(visible);
            _button.interactable = visible && (_allowed == null || _allowed());
            if (!visible || !(_template.transform.parent is RectTransform parent)) return;

            // Respect native layout groups; otherwise use the free space left of Leave.
            var group = parent.GetComponent<HorizontalLayoutGroup>();
            float spacing = group != null ? group.spacing : 8;
            float occupied = 0;
            int siblings = 0;
            foreach (Transform child in parent)
            {
                if (child == _rect || !child.gameObject.activeSelf || !(child is RectTransform rect)) continue;
                if (group == null && child.GetComponent<Button>() == null) continue;
                var layout = child.GetComponent<LayoutElement>();
                if (group != null && layout != null && layout.ignoreLayout) continue;
                occupied += rect.rect.width; siblings++;
            }
            float room = parent.rect.width - occupied - spacing * siblings;
            if (group != null) room -= group.padding.horizontal;
            else room = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, _template.transform).min.x - parent.rect.xMin - 20;
            float width = Mathf.Min(Mathf.Max(220, _label.GetPreferredValues(text).x + 28), Mathf.Max(80, room));
            _layout.minWidth = width; _layout.preferredWidth = width;
            _layout.preferredHeight = _height;
            if (group == null)
            {
                _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0, .5f);
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, _template.transform);
                _rect.anchoredPosition = new Vector2(12, bounds.center.y - parent.rect.center.y);
                _rect.sizeDelta = new Vector2(width, _height);
            }
        }
        private void Click()
        {
            if (_disposed || _button == null || !_button.gameObject.activeInHierarchy || !_button.IsInteractable() ||
                (_allowed != null && !_allowed())) return;
            _return?.Invoke();
            Refresh(false);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_button == null) return;
            _button.gameObject.SetActive(false);
            _button.onClick.RemoveAllListeners();
            UnityEngine.Object.Destroy(_button.gameObject);
        }
    }
}
