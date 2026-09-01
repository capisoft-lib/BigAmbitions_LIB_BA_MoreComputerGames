using System;
using System.Collections.Generic;
using BigAmbitions.Mods;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    internal sealed class McgShortcutUiText
    {
        internal string Unbound => ComputerGames.ResolveText("bacg_shortcut_unbound", "Unbound");
        internal string CapturePrompt => ComputerGames.ResolveText("bacg_shortcut_capture", "Press a key...");
        internal string ConflictPrefix => ComputerGames.ResolveText("bacg_shortcut_conflict", "Already used");
    }

    internal sealed class McgShortcutOption : ModOption, IPersistableOption
    {
        internal McgKeybind DefaultBinding { get; }
        internal McgShortcutUiText UiText { get; } = new McgShortcutUiText();
        internal McgShortcutHandle Handle { get; }

        internal McgShortcutOption(string id, string label, McgKeybind defaultBinding) : base(ValidateId(id), label)
        {
            DefaultBinding = defaultBinding;
            Handle = new McgShortcutHandle(this);
        }

        public override void SpawnUi(Transform parent, string modId)
        {
            if (parent == null)
            {
                Debug.LogError("[BaComputerGames] Cannot spawn shortcut option '" + Id + "': parent is null.");
                return;
            }
            var root = new GameObject("McgShortcutOption_" + Id, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.AddComponent<McgShortcutOptionControl>().Initialize(this, modId);
        }

        private static string ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A shortcut option needs a persistent id.", nameof(id));
            return id;
        }
    }

    internal sealed class McgShortcutHandle : IDisposable
    {
        private readonly McgShortcutOption _option;
        private McgKeybind _binding;
        private string _modId, _loadedModId;
        private bool _hasLoaded, _disposed;

        internal McgShortcutHandle(McgShortcutOption option)
        {
            _option = option ?? throw new ArgumentNullException(nameof(option));
            _binding = option.DefaultBinding;
            McgShortcutRegistry.Track(this);
        }

        internal event Action<McgKeybind> BindingChanged;
        internal string OptionId => _option.Id;
        internal McgKeybind Binding { get { EnsureLoaded(); return _binding; } }
        internal McgKeybind DefaultBinding => _option.DefaultBinding;
        internal bool IsDisposed => _disposed;
        internal bool HasConflict => McgShortcutRegistry.HasConflict(this, Binding);

        internal void AttachToMod(string modId, bool force = false)
        {
            if (_disposed) return;
            if (!string.Equals(_modId, modId, StringComparison.Ordinal))
            {
                _modId = modId;
                _hasLoaded = false;
            }
            LoadFromPreferences(force);
        }

        internal bool WasPressedThisFrame()
        {
            if (_disposed || McgShortcutCaptureCoordinator.IsCaptureActive) return false;
            var binding = Binding;
            return binding.IsBound && !McgShortcutRegistry.HasConflict(this, binding) && binding.WasPressedThisFrame();
        }

        internal bool TrySetBinding(McgKeybind binding)
        {
            if (_disposed || string.IsNullOrEmpty(_modId) || McgShortcutRegistry.HasConflict(this, binding)) return false;
            SetBinding(binding, true);
            return true;
        }

        internal McgKeybind GetBindingForRegistry() { EnsureLoaded(); return _binding; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            BindingChanged = null;
            McgShortcutRegistry.Untrack(this);
        }

        private void EnsureLoaded()
        {
            if (_disposed) return;
            if (!_hasLoaded || !string.Equals(_loadedModId, _modId, StringComparison.Ordinal)) LoadFromPreferences(false);
        }

        private void LoadFromPreferences(bool force)
        {
            if (string.IsNullOrEmpty(_modId) || (!force && _hasLoaded && _loadedModId == _modId)) return;
            var loaded = _option.DefaultBinding;
            var key = PreferenceKey(_modId, OptionId);
            if (UnityEngine.PlayerPrefs.HasKey(key) &&
                !McgKeybind.TryParse(UnityEngine.PlayerPrefs.GetString(key, string.Empty), out loaded))
            {
                loaded = _option.DefaultBinding;
                Debug.LogWarning("[BaComputerGames] Ignoring invalid shortcut preference '" + key + "'.");
            }
            var changed = _binding != loaded;
            _binding = loaded;
            _loadedModId = _modId;
            _hasLoaded = true;
            if (changed) RaiseChanged();
        }

        private void SetBinding(McgKeybind binding, bool persist)
        {
            if (_binding == binding) return;
            _binding = binding;
            _loadedModId = _modId;
            _hasLoaded = true;
            if (persist) UnityEngine.PlayerPrefs.SetString(PreferenceKey(_modId, OptionId), binding.Serialize());
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            McgShortcutRegistry.NotifyChanged();
            var handlers = BindingChanged;
            if (handlers == null) return;
            foreach (Action<McgKeybind> handler in handlers.GetInvocationList())
                try { handler(_binding); } catch (Exception error) { ComputerGames.Report(error); }
        }

        private static string PreferenceKey(string modId, string optionId) => "m:" + modId + ":" + optionId;
    }

    internal static class McgShortcutRegistry
    {
        private static readonly HashSet<McgShortcutHandle> Handles = new HashSet<McgShortcutHandle>();
        internal static event Action Changed;

        internal static void Track(McgShortcutHandle handle) { if (handle != null) Handles.Add(handle); }
        internal static void Untrack(McgShortcutHandle handle) { if (handle != null && Handles.Remove(handle)) NotifyChanged(); }
        internal static bool HasConflict(McgShortcutHandle self, McgKeybind candidate)
        {
            if (!candidate.IsBound) return false;
            Handles.RemoveWhere(handle => handle == null || handle.IsDisposed);
            foreach (var other in Handles)
                if (!ReferenceEquals(other, self) && other.GetBindingForRegistry() == candidate) return true;
            return false;
        }
        internal static void NotifyChanged()
        {
            var handlers = Changed;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
                try { handler(); } catch (Exception error) { ComputerGames.Report(error); }
        }
    }
}
