using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // Discovers vanilla resources by name and keeps code-native fallbacks.
    internal static class McgShortcutStyle
    {
        internal static readonly Color BodyTextColor = new Color(.92f, .94f, .96f, 1f);
        internal static readonly Color FieldTextColor = new Color(.13f, .15f, .18f, 1f);
        private static Sprite _blue, _grey, _red, _solid, _field, _reset;
        private static TMP_FontAsset _bold, _medium;
        private static bool _initialized;

        internal static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (sprite == null) continue;
                    if (_blue == null && string.Equals(sprite.name, "Gradient-Blue-Round", StringComparison.OrdinalIgnoreCase)) _blue = sprite;
                    else if (_grey == null && string.Equals(sprite.name, "Gradient-Gray-Border-Round", StringComparison.OrdinalIgnoreCase)) _grey = sprite;
                    else if (_red == null && string.Equals(sprite.name, "Gradient-Red-Round", StringComparison.OrdinalIgnoreCase)) _red = sprite;
                }
                foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (font == null) continue;
                    if (_bold == null && font.name == "Rubik-Bold SDF") _bold = font;
                    else if (_medium == null && font.name == "Rubik-Medium SDF") _medium = font;
                }
            }
            catch { }
        }

        internal static void ApplyFont(TextMeshProUGUI text)
        {
            EnsureInitialized();
            var font = _medium != null ? _medium : _bold;
            if (font != null) text.font = font;
        }

        internal static void ApplyBlue(Image image) { EnsureInitialized(); Apply(image, _blue, new Color(.25f, .58f, .82f, 1f)); }
        internal static void ApplyGrey(Image image) { EnsureInitialized(); Apply(image, _grey, new Color(.36f, .41f, .46f, 1f)); }
        internal static void ApplyRed(Image image)
        { EnsureInitialized(); Apply(image, _red != null ? _red : _grey, new Color(.78f, .28f, .28f, 1f)); }
        internal static void ApplyField(Image image)
        { EnsureInitialized(); Apply(image, FieldSprite(), new Color(.96f, .97f, .98f, 1f), Color.white); }

        internal static Image CreateButtonGraphic(RectTransform root, Action<Image> style)
        {
            var image = root.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            style(image);
            return image;
        }

        internal static Sprite ResetIcon()
        {
            if (_reset != null) return _reset;
            const int size = 32;
            const float center = 16f, radius = 9f, halfStroke = 1.6f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "MCG Reset Icon", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = x + .5f - center, dy = y + .5f - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360;
                bool arc = Mathf.Abs(distance - radius) <= halfStroke && (angle >= 116 || angle <= 76);
                bool arrow = x >= 5 && x <= 13 && y >= 19 && y <= 27 && (x + y <= 33 || y - x >= 14);
                texture.SetPixel(x, y, arc || arrow ? Color.white : clear);
            }
            texture.Apply();
            _reset = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
            _reset.name = "MCG Reset Icon";
            return _reset;
        }

        private static Sprite FieldSprite()
        {
            if (_field != null) return _field;
            const int size = 32;
            const float radius = 5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "MCG Shortcut Field", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0, 0, 0, 0);
            float half = size * .5f, straightHalf = half - radius, radiusSq = radius * radius;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - half) - straightHalf, 0);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - half) - straightHalf, 0);
                texture.SetPixel(x, y, dx * dx + dy * dy <= radiusSq ? Color.white : clear);
            }
            texture.Apply();
            _field = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _field.name = "MCG Shortcut Field";
            return _field;
        }

        private static void Apply(Image image, Sprite sprite, Color fallback) => Apply(image, sprite, fallback, Color.white);
        private static void Apply(Image image, Sprite sprite, Color fallback, Color spriteTint)
        {
            EnsureInitialized();
            image.sprite = sprite != null ? sprite : SolidSprite();
            image.color = sprite != null ? spriteTint : fallback;
            var border = image.sprite.border;
            image.type = border.x > .01f || border.y > .01f || border.z > .01f || border.w > .01f
                ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1;
            image.preserveAspect = false;
        }

        private static Sprite SolidSprite()
        {
            if (_solid != null) return _solid;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            _solid = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 100);
            return _solid;
        }
    }
}
