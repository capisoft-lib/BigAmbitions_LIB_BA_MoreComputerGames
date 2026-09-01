using System;
using System.Globalization;
using UnityEngine.InputSystem;

namespace Capisoft.Lib.BaComputerGames
{
    [Flags]
    internal enum McgKeyModifiers
    {
        None = 0,
        Control = 1 << 0,
        Shift = 1 << 1,
        Alt = 1 << 2,
        Command = 1 << 3
    }

    // Immutable keyboard chord used only by MCG's own session actions.
    internal readonly struct McgKeybind : IEquatable<McgKeybind>
    {
        private const int SerializationVersion = 1;
        private const McgKeyModifiers AllModifiers = McgKeyModifiers.Control | McgKeyModifiers.Shift |
            McgKeyModifiers.Alt | McgKeyModifiers.Command;

        internal static McgKeybind Unbound => default;
        internal Key PrimaryKey { get; }
        internal McgKeyModifiers Modifiers { get; }
        internal bool IsBound => PrimaryKey != Key.None;

        internal McgKeybind(Key primaryKey, McgKeyModifiers modifiers = McgKeyModifiers.None)
        {
            if (!Enum.IsDefined(typeof(Key), primaryKey))
                throw new ArgumentOutOfRangeException(nameof(primaryKey), primaryKey, "Unknown Input System key.");
            if ((modifiers & ~AllModifiers) != 0)
                throw new ArgumentOutOfRangeException(nameof(modifiers), modifiers, "Unknown shortcut modifier.");
            if (primaryKey == Key.None)
            {
                PrimaryKey = Key.None;
                Modifiers = McgKeyModifiers.None;
                return;
            }
            if (IsModifierKey(primaryKey))
                throw new ArgumentException("A shortcut needs a non-modifier primary key.", nameof(primaryKey));
            PrimaryKey = primaryKey;
            Modifiers = modifiers;
        }

        internal string Serialize() => SerializationVersion.ToString(CultureInfo.InvariantCulture) + "|" +
            ((int)Modifiers).ToString(CultureInfo.InvariantCulture) + "|" +
            ((int)PrimaryKey).ToString(CultureInfo.InvariantCulture);

        internal static bool TryParse(string value, out McgKeybind binding)
        {
            binding = Unbound;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Split('|');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) ||
                version != SerializationVersion ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var modifiersValue) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var keyValue)) return false;
            try
            {
                binding = new McgKeybind((Key)keyValue, (McgKeyModifiers)modifiersValue);
                return true;
            }
            catch (ArgumentException)
            {
                binding = Unbound;
                return false;
            }
        }

        internal string ToDisplayString(string unboundText)
        {
            if (!IsBound) return unboundText ?? string.Empty;
            string prefix = string.Empty;
            if ((Modifiers & McgKeyModifiers.Control) != 0) prefix += "Ctrl + ";
            if ((Modifiers & McgKeyModifiers.Shift) != 0) prefix += "Shift + ";
            if ((Modifiers & McgKeyModifiers.Alt) != 0) prefix += "Alt + ";
            if ((Modifiers & McgKeyModifiers.Command) != 0) prefix += "Cmd + ";
            return prefix + PrimaryKeyDisplayName(PrimaryKey);
        }

        internal bool WasPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !IsBound) return false;
            var primary = keyboard[PrimaryKey];
            return primary != null && primary.wasPressedThisFrame && ReadCurrentModifiers(keyboard) == Modifiers;
        }

        internal static McgKeyModifiers ReadCurrentModifiers(Keyboard keyboard)
        {
            if (keyboard == null) return McgKeyModifiers.None;
            var result = McgKeyModifiers.None;
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) result |= McgKeyModifiers.Control;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) result |= McgKeyModifiers.Shift;
            if (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed) result |= McgKeyModifiers.Alt;
            if (keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed) result |= McgKeyModifiers.Command;
            return result;
        }

        internal static bool IsModifierKey(Key key) => key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift || key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftMeta || key == Key.RightMeta;

        public bool Equals(McgKeybind other) => PrimaryKey == other.PrimaryKey && Modifiers == other.Modifiers;
        public override bool Equals(object obj) => obj is McgKeybind other && Equals(other);
        public override int GetHashCode() => ((int)PrimaryKey * 397) ^ (int)Modifiers;
        public static bool operator ==(McgKeybind left, McgKeybind right) => left.Equals(right);
        public static bool operator !=(McgKeybind left, McgKeybind right) => !left.Equals(right);

        private static string PrimaryKeyDisplayName(Key key)
        {
            try
            {
                var name = Keyboard.current?[key]?.displayName;
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            catch { }
            switch (key)
            {
                case Key.Digit0: return "0";
                case Key.Digit1: return "1";
                case Key.Digit2: return "2";
                case Key.Digit3: return "3";
                case Key.Digit4: return "4";
                case Key.Digit5: return "5";
                case Key.Digit6: return "6";
                case Key.Digit7: return "7";
                case Key.Digit8: return "8";
                case Key.Digit9: return "9";
                case Key.LeftArrow: return "Left Arrow";
                case Key.RightArrow: return "Right Arrow";
                case Key.UpArrow: return "Up Arrow";
                case Key.DownArrow: return "Down Arrow";
                case Key.PageDown: return "Page Down";
                case Key.PageUp: return "Page Up";
                case Key.CapsLock: return "Caps Lock";
                case Key.NumLock: return "Num Lock";
                case Key.ScrollLock: return "Scroll Lock";
                case Key.PrintScreen: return "Print Screen";
                case Key.ContextMenu: return "Context Menu";
                case Key.NumpadEnter: return "Num Enter";
                case Key.NumpadDivide: return "Num /";
                case Key.NumpadMultiply: return "Num *";
                case Key.NumpadPlus: return "Num +";
                case Key.NumpadMinus: return "Num -";
                case Key.NumpadPeriod: return "Num .";
                case Key.NumpadEquals: return "Num =";
                default: return key.ToString();
            }
        }
    }
}
