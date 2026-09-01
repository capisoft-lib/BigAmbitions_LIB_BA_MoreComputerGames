using System;
using System.Globalization;
using BAModAPI;
using BigAmbitions.Mods;
using UnityEngine.InputSystem;

namespace Capisoft.Lib.BaComputerGames
{
    internal static class McgShortcuts
    {
        private const string ReturnOptionId = "return_to_menu_shortcut";
        private const string LeaveOptionId = "leave_computer_shortcut";
        private static McgShortcutHandle _returnToMenu, _leaveComputer;
        private static string _modId;

        internal static event Action BindingsChanged;
        internal static string ReturnToMenuDisplay => Display(_returnToMenu, new McgKeybind(Key.Backspace));
        internal static string LeaveComputerDisplay => Display(_leaveComputer, new McgKeybind(Key.Tab));

        internal static void Initialize(ModContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Shutdown();
            var returnOption = new McgShortcutOption(ReturnOptionId, "bacg_shortcut_return", new McgKeybind(Key.Backspace));
            var leaveOption = new McgShortcutOption(LeaveOptionId, "bacg_shortcut_leave", new McgKeybind(Key.Tab));
            try
            {
                var options = new ModOptions()
                    .AddHeader("bacg_shortcuts_header")
                    .AddCustom(returnOption)
                    .AddCustom(leaveOption)
                    .AddSplitter();
                OptionsService.Register(context.ModId, options);
                _modId = context.ModId;
                _returnToMenu = returnOption.Handle;
                _leaveComputer = leaveOption.Handle;
                _returnToMenu.BindingChanged += OnBindingChanged;
                _leaveComputer.BindingChanged += OnBindingChanged;
                _returnToMenu.AttachToMod(_modId, true);
                _leaveComputer.AttachToMod(_modId, true);
            }
            catch
            {
                Shutdown();
                returnOption.Handle.Dispose();
                leaveOption.Handle.Dispose();
                throw;
            }
        }

        internal static bool ReturnToMenuPressed() => _returnToMenu != null && _returnToMenu.WasPressedThisFrame();
        internal static bool LeaveComputerPressed() => _leaveComputer != null && _leaveComputer.WasPressedThisFrame();

        internal static string FormatSessionText(string template)
        {
            if (string.IsNullOrEmpty(template)) return template;
            try
            {
                return string.Format(CultureInfo.CurrentCulture, template, ReturnToMenuDisplay, LeaveComputerDisplay);
            }
            catch (FormatException error)
            {
                ComputerGames.Report(error);
                return template;
            }
        }

        internal static void Shutdown()
        {
            McgShortcutCaptureCoordinator.CancelActive();
            var returnToMenu = _returnToMenu; _returnToMenu = null;
            var leaveComputer = _leaveComputer; _leaveComputer = null;
            if (returnToMenu != null) returnToMenu.BindingChanged -= OnBindingChanged;
            if (leaveComputer != null) leaveComputer.BindingChanged -= OnBindingChanged;
            if (!string.IsNullOrEmpty(_modId))
            {
                try { OptionsService.RemoveModOptions(_modId); }
                catch (Exception error) { ComputerGames.Report(error); }
            }
            _modId = null;
            returnToMenu?.Dispose();
            leaveComputer?.Dispose();
            BindingsChanged = null;
        }

        private static string Display(McgShortcutHandle handle, McgKeybind fallback)
        {
            var binding = handle != null ? handle.Binding : fallback;
            return binding.ToDisplayString(ComputerGames.ResolveText("bacg_shortcut_unbound", "Unbound"));
        }

        private static void OnBindingChanged(McgKeybind _)
        {
            var handlers = BindingsChanged;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
                try { handler(); } catch (Exception error) { ComputerGames.Report(error); }
        }
    }
}
