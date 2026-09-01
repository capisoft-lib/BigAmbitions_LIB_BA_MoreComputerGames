using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // Native-scale custom row owned entirely by MCG.
    internal sealed class McgShortcutOptionControl : MonoBehaviour
    {
        private const float RowHeight = 100, ButtonHeight = 60, BindingWidth = 620, ResetWidth = 60;
        private const float ResetIconSize = 38, LabelMinWidth = 280, LabelSize = 36, ButtonSize = 30, ButtonMinSize = 20;
        private McgShortcutOption _option;
        private Image _bindingGraphic;
        private TextMeshProUGUI _bindingLabel;
        private bool _capturing, _initialized;
        private int _captureStartedFrame;
        private float _rejectedUntil;
        private McgKeybind _rejected;

        internal void Initialize(McgShortcutOption option, string modId)
        {
            _option = option;
            McgShortcutStyle.EnsureInitialized();
            BuildUi();
            _option.Handle.BindingChanged += OnBindingChanged;
            McgShortcutRegistry.Changed += OnConflictsChanged;
            _initialized = true;
            _option.Handle.AttachToMod(modId, true);
            UpdateVisual();
        }

        private void BuildUi()
        {
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(0, RowHeight);
            var rootLayout = gameObject.AddComponent<LayoutElement>();
            rootLayout.minHeight = rootLayout.preferredHeight = RowHeight;
            rootLayout.flexibleWidth = 1;
            var row = gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 20, 20);
            row.spacing = 12;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var optionLabelRoot = CreateRect(transform, "OptionLabel");
            var optionLayout = optionLabelRoot.gameObject.AddComponent<LayoutElement>();
            optionLayout.minWidth = LabelMinWidth; optionLayout.flexibleWidth = 1;
            var optionLabel = optionLabelRoot.gameObject.AddComponent<TextMeshProUGUI>();
            optionLabel.text = _option.Label; optionLabel.fontSize = LabelSize; optionLabel.fontStyle = FontStyles.Bold;
            optionLabel.color = McgShortcutStyle.BodyTextColor; optionLabel.alignment = TextAlignmentOptions.MidlineLeft;
#pragma warning disable CS0618
            optionLabel.enableWordWrapping = false;
#pragma warning restore CS0618
            optionLabel.overflowMode = TextOverflowModes.Ellipsis; optionLabel.raycastTarget = false;
            McgShortcutStyle.ApplyFont(optionLabel);
            optionLabelRoot.gameObject.AddComponent<TextLocalizationComponent>().Key = _option.Label;

            var bindingButton = CreateButton(transform, "BindingButton", BindingWidth, ButtonHeight,
                out _bindingGraphic, out _bindingLabel);
            bindingButton.onClick.AddListener(BeginCapture);
            _bindingLabel.enableAutoSizing = true; _bindingLabel.fontSizeMin = ButtonMinSize; _bindingLabel.fontSizeMax = ButtonSize;
            _bindingLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var resetButton = CreateButton(transform, "ResetButton", ResetWidth, ButtonHeight, out var resetGraphic, out var resetLabel);
            McgShortcutStyle.ApplyRed(resetGraphic); resetLabel.gameObject.SetActive(false);
            var iconRoot = CreateRect(resetButton.transform, "ResetIcon");
            iconRoot.anchorMin = iconRoot.anchorMax = iconRoot.pivot = new Vector2(.5f, .5f);
            iconRoot.anchoredPosition = Vector2.zero; iconRoot.sizeDelta = new Vector2(ResetIconSize, ResetIconSize);
            var icon = iconRoot.gameObject.AddComponent<Image>();
            icon.sprite = McgShortcutStyle.ResetIcon(); icon.color = Color.white; icon.preserveAspect = true; icon.raycastTarget = false;
            if (resetGraphic.material != null) icon.material = resetGraphic.material;
            resetButton.onClick.AddListener(ResetBinding);
        }

        private void Update()
        {
            if (!_capturing) return;
            if (_rejectedUntil > 0 && Time.unscaledTime >= _rejectedUntil) { _rejectedUntil = 0; UpdateVisual(); }
            if (Time.frameCount <= _captureStartedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { StopCapture(); return; }
            if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame)
            { if (_option.Handle.TrySetBinding(McgKeybind.Unbound)) StopCapture(); return; }
            foreach (var control in keyboard.allKeys)
            {
                var key = control.keyCode;
                if (!control.wasPressedThisFrame || key == Key.None || key == Key.Escape || key == Key.Backspace ||
                    key == Key.Delete || McgKeybind.IsModifierKey(key)) continue;
                var candidate = new McgKeybind(key, McgKeybind.ReadCurrentModifiers(keyboard));
                if (_option.Handle.TrySetBinding(candidate)) StopCapture();
                else { _rejected = candidate; _rejectedUntil = Time.unscaledTime + 1.5f; UpdateVisual(); }
                return;
            }
        }

        private void BeginCapture()
        {
            if (_capturing) { StopCapture(); return; }
            McgShortcutCaptureCoordinator.Begin(this);
            _capturing = true; _captureStartedFrame = Time.frameCount; _rejectedUntil = 0; UpdateVisual();
        }
        private void ResetBinding() { StopCapture(); _option.Handle.TrySetBinding(_option.DefaultBinding); UpdateVisual(); }
        private void StopCapture()
        {
            if (!_capturing) return;
            _capturing = false; _rejectedUntil = 0; McgShortcutCaptureCoordinator.Release(this); UpdateVisual();
        }
        internal void CancelCaptureFromCoordinator() { _capturing = false; _rejectedUntil = 0; UpdateVisual(); }
        private void OnBindingChanged(McgKeybind _) => UpdateVisual();
        private void OnConflictsChanged() => UpdateVisual();

        private void UpdateVisual()
        {
            if (!_initialized || _bindingLabel == null) return;
            if (_capturing)
            {
                if (_rejectedUntil > Time.unscaledTime)
                {
                    _bindingLabel.text = _option.UiText.ConflictPrefix + ": " + _rejected.ToDisplayString(_option.UiText.Unbound);
                    _bindingLabel.color = Color.white; McgShortcutStyle.ApplyRed(_bindingGraphic);
                }
                else
                {
                    _bindingLabel.text = _option.UiText.CapturePrompt;
                    _bindingLabel.color = Color.white; McgShortcutStyle.ApplyBlue(_bindingGraphic);
                }
                return;
            }
            var binding = _option.Handle.Binding;
            if (_option.Handle.HasConflict)
            {
                _bindingLabel.text = _option.UiText.ConflictPrefix + ": " + binding.ToDisplayString(_option.UiText.Unbound);
                _bindingLabel.color = Color.white; McgShortcutStyle.ApplyRed(_bindingGraphic);
            }
            else
            {
                _bindingLabel.text = binding.ToDisplayString(_option.UiText.Unbound);
                _bindingLabel.color = McgShortcutStyle.FieldTextColor; McgShortcutStyle.ApplyField(_bindingGraphic);
            }
        }

        private void OnDisable() { if (_capturing) StopCapture(); }
        private void OnDestroy()
        {
            McgShortcutCaptureCoordinator.Release(this);
            if (!_initialized || _option == null) return;
            _option.Handle.BindingChanged -= OnBindingChanged;
            McgShortcutRegistry.Changed -= OnConflictsChanged;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false); return root;
        }
        private static Button CreateButton(Transform parent, string name, float width, float height,
            out Image graphic, out TextMeshProUGUI label)
        {
            var root = CreateRect(parent, name);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = width; layout.minHeight = layout.preferredHeight = height;
            graphic = McgShortcutStyle.CreateButtonGraphic(root, McgShortcutStyle.ApplyGrey);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            var labelRoot = CreateRect(root, "Label");
            labelRoot.anchorMin = Vector2.zero; labelRoot.anchorMax = Vector2.one;
            labelRoot.offsetMin = new Vector2(16, 0); labelRoot.offsetMax = new Vector2(-16, 0);
            label = labelRoot.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = ButtonSize; label.fontStyle = FontStyles.Bold; label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
#pragma warning disable CS0618
            label.enableWordWrapping = false;
#pragma warning restore CS0618
            label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
            McgShortcutStyle.ApplyFont(label);
            return button;
        }
    }

    internal static class McgShortcutCaptureCoordinator
    {
        private static McgShortcutOptionControl _active;
        internal static bool IsCaptureActive => _active != null;
        internal static void Begin(McgShortcutOptionControl control)
        {
            if (_active == control) return;
            var previous = _active; _active = control; previous?.CancelCaptureFromCoordinator();
        }
        internal static void Release(McgShortcutOptionControl control) { if (_active == control) _active = null; }
        internal static void CancelActive()
        {
            var active = _active; _active = null; active?.CancelCaptureFromCoordinator();
        }
    }
}
