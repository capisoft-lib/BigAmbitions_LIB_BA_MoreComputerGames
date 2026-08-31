using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    // A mini-game image rendered into the native monitor, never a screen-space popup.
    internal sealed class ComputerMenuView : IDisposable
    {
        private static readonly Color Ink = new Color32(12, 25, 35, 255);
        private static readonly Color Paper = new Color32(233, 237, 214, 255);
        private static readonly Color Green = new Color32(173, 226, 137, 255);
        private static readonly Color Muted = new Color32(146, 169, 166, 255);
        private readonly Font _font;
        private readonly CanvasScaler _scaler;
        internal GameObject Root { get; }
        internal Camera Camera { get; }
        private readonly Image[] _rows = new Image[5];
        private readonly Text[] _names = new Text[5];
        private readonly Text _title, _description, _record, _count, _hint, _status, _subtitle;
        private readonly GameObject _library, _loading;
        private readonly RectTransform _progress;
        private bool _busy;
        private static string T(string key, string fallback) => ComputerGames.ResolveText(key, fallback);

        internal ComputerMenuView(Transform parent)
        {
            Root = new GameObject("MCG_MonitorMenu"); Root.transform.SetParent(parent, false);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cameraRoot = new GameObject("MCG_MenuCamera", typeof(Camera));
            cameraRoot.transform.SetParent(Root.transform, false);
            cameraRoot.transform.localPosition = new Vector3(480, 270, -10);
            Camera = cameraRoot.GetComponent<Camera>();
            Camera.orthographic = true; Camera.orthographicSize = 270;
            Camera.nearClipPlane = .1f; Camera.farClipPlane = 20;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = Ink;
            Camera.cullingMask = 1 << 5; Camera.allowHDR = false; Camera.allowMSAA = false;
            var canvasObject = new GameObject("MCG_MenuCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(Root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = Camera;
            _scaler = canvasObject.AddComponent<CanvasScaler>();
            var rect = (RectTransform)canvasObject.transform;
            rect.pivot = Vector2.zero; rect.sizeDelta = new Vector2(960, 540);
            Box(rect, "Background", 0, 0, 960, 540, Ink);
            Box(rect, "TopLine", 32, 504, 896, 3, Green);
            Label(rect, "MCG", 32, 421, 147, 78, 64, Green);
            Label(rect, "MORE COMPUTER GAMES", 191, 446, 630, 34, 29, Paper);
            _subtitle = Label(rect, "", 193, 416, 666, 29, 18, Muted);
            Box(rect, "Divider", 32, 399, 896, 1, new Color32(53, 76, 82, 255));

            var library = Rect(rect, "Library", 0, 0, 960, 400); _library = library.gameObject;
            _count = Label(library, "", 36, 354, 540, 30, 18, Muted);
            for (int i = 0; i < 5; i++)
            {
                var row = Box(library, "GameRow" + i, 32, 292 - i * 55, 535, 49, Ink);
                _rows[i] = row.GetComponent<Image>();
                _names[i] = Label(row, "", 16, 0, 501, 49, 24, Paper);
            }
            Box(library, "DetailsDivider", 596, 81, 1, 261, new Color32(53, 76, 82, 255));
            Label(library, "01 / SELECT", 624, 348, 302, 34, 18, Green).text = T("bacg_details", "SELECTED GAME");
            _title = Label(library, "", 624, 266, 302, 78, 28, Paper, true);
            _description = Label(library, "", 624, 148, 302, 111, 20, Muted, true);
            _record = Label(library, "", 624, 91, 302, 47, 23, Green);

            var loading = Rect(rect, "Loading", 32, 84, 896, 292); _loading = loading.gameObject;
            Label(loading, "MCG / ARCADE", 0, 229, 896, 35, 22, Muted);
            _status = Label(loading, "", 0, 98, 896, 113, 32, Paper, true);
            Box(loading, "ProgressTrack", 0, 59, 896, 7, new Color32(53, 76, 82, 255));
            _progress = Box(loading, "Progress", 0, 59, 170, 7, Green);
            Box(rect, "BottomLine", 32, 69, 896, 1, new Color32(53, 76, 82, 255));
            _hint = Label(rect, "", 32, 13, 896, 44, 18, Paper);
            SetLayers(Root.transform);
        }

        internal void Draw(ComputerGamesCatalog catalog, ComputerLauncherState state, ComputerGameDefinition choice)
        {
            _busy = state == ComputerLauncherState.Loading;
            bool message = _busy || state == ComputerLauncherState.Error;
            _library.SetActive(!message); _loading.SetActive(message);
            _subtitle.text = T("bacg_menu_tagline", "A little break. Big ambitions.");
            _hint.text = message
                ? T("bacg_menu_back", "BACKSPACE  Menu / cancel    TAB  Leave computer    ESC  Pause")
                : T("bacg_menu_controls", "↑ / ↓  Select    ENTER  Play    BACKSPACE  Menu    TAB  Leave    ESC  Pause");
            if (message)
            {
                string title = choice == null ? "" : ComputerGames.ResolveText(choice.TitleKey, choice.Title);
                _status.text = _busy ? T("bacg_loading", "Loading game…") + "\n" + title :
                    T("bacg_failed", "Unable to load this game.") + "\n" + T("bacg_retry", "ENTER to retry, BACKSPACE for the menu.");
                _progress.gameObject.SetActive(_busy); return;
            }
            int pageStart = catalog.SelectedIndex / 5 * 5;
            _count.text = T("bacg_library", "YOUR GAMES") + "   " + (catalog.SelectedIndex + 1) + " / " + catalog.Count;
            for (int i = 0; i < 5; i++)
            {
                int index = pageStart + i; bool visible = index < catalog.Count;
                _rows[i].gameObject.SetActive(visible); if (!visible) continue;
                bool selected = index == catalog.SelectedIndex;
                _rows[i].color = selected ? Green : new Color32(22, 41, 51, 255);
                _names[i].color = selected ? Ink : Paper;
                _names[i].text = (selected ? ">  " : "   ") + ComputerGames.ResolveText(catalog[index].TitleKey, catalog[index].Title);
            }
            var game = catalog.Selected;
            _title.text = ComputerGames.ResolveText(game.TitleKey, game.Title);
            _description.text = ComputerGames.ResolveText(game.DescriptionKey, game.Description);
            _record.text = T("bacg_best", "BEST") + "  " + ComputerGames.GetHighScore(game.Id, game.Ruleset).ToString("N0");
        }
        internal void Animate(float time)
        { if (_busy) _progress.anchoredPosition = new Vector2(Mathf.PingPong(time * 320, 726), 59); }
        internal void SetResolution(int width, int height)
        {
            Camera.orthographicSize = Math.Max(270, 480 / (Math.Max(1, width) / (float)Math.Max(1, height)));
            _scaler.dynamicPixelsPerUnit = Math.Max(1, width / 960f);
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        private Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color color, bool wrap = false)
        {
            var text = Rect(parent, "Label", x, y, w, h).gameObject.AddComponent<Text>();
            text.font = _font; text.text = value; text.fontSize = size; text.color = color;
            text.supportRichText = false; text.raycastTarget = false;
            text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // Long third-party metadata must remain within the monitor's allotted space.
            text.resizeTextForBestFit = true; text.resizeTextMinSize = wrap ? 15 : 14; text.resizeTextMaxSize = size;
            if (!wrap) text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        private static void SetLayers(Transform root)
        { root.gameObject.layer = 5; foreach (Transform child in root) SetLayers(child); }
        public void Dispose()
        {
            if (Camera != null) Camera.targetTexture = null;
            if (Root != null) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); }
        }
    }
}
