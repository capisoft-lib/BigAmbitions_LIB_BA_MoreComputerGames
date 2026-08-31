using System;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    internal sealed class ComputerGamesCatalog : IDisposable
    {
        private readonly Action _cancel;
        private GameObject _root;
        private RectTransform _content;
        private ScrollRect _scroll;
        private TextMeshProUGUI _status;
        private Action<string> _choose;
        private bool _loading;
        public ComputerGamesCatalog(Action cancel) { _cancel = cancel; ComputerGames.CatalogChanged += Refresh; }
        private static string T(string key, string fallback) => ComputerGames.ResolveText(key, fallback);
        public void Show(Action<string> choose)
        {
            EnsureCreated(); _choose = choose; _loading = false; _root.SetActive(true); Refresh();
        }
        public void Hide() { if (_root != null) _root.SetActive(false); BaUiFocus.ReleaseForMovement(); }
        private void Cancel() { _cancel(); Hide(); }
        public void Loading()
        {
            _loading = true; _scroll.gameObject.SetActive(false);
            _status.text = T("bacg_loading", "Walking to the computer / loading game…");
        }
        public void Failed()
        {
            if (_root == null) return;
            _loading = false; _root.SetActive(true); Refresh();
            _status.text = T("bacg_failed", "Unable to start this game. See Player.log for details.");
        }
        public void Tick()
        { if (_root != null && _root.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Cancel(); }
        private void EnsureCreated()
        {
            if (_root != null) return;
            BaUi.EnsureReady();
            var built = BaUi.Modal("BaComputerGames_Catalog", 12000, 0.45f).OnDismiss(Cancel)
                .Panel(BaPanelRecipe.Modal, 760, height: 590)
                .Header(h => h.TitleCenter(T("bacg_title", "More Computer Games (MCG)"))).SkipBody().Build();
            _root = built.Root;
            var area = Rect(built.Panel, "Games"); Stretch(area, 24, 24, 105, 92);
            area.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.1f, 0.14f, 0.3f);
            _scroll = area.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false; _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 35;
            var viewport = Rect(area, "Viewport"); Stretch(viewport, 0, 18, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = Rect(viewport, "Content");
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = Vector2.one; _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero; _content.sizeDelta = Vector2.zero;
            _scroll.viewport = viewport; _scroll.content = _content;
            var rail = Rect(area, "Scrollbar"); rail.anchorMin = new Vector2(1, 0); rail.anchorMax = Vector2.one;
            rail.pivot = Vector2.one; rail.offsetMin = new Vector2(-12, 0); rail.offsetMax = Vector2.zero;
            rail.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            var grip = Rect(rail, "Handle"); Stretch(grip, 0, 0, 0, 0);
            var graphic = grip.gameObject.AddComponent<Image>(); graphic.color = new Color(0.5f, 0.63f, 0.69f, 1);
            var scrollbar = rail.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = grip; scrollbar.targetGraphic = graphic;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            _scroll.verticalScrollbar = scrollbar;
            _status = Label(built.Panel, "Status", "", 15); Stretch(_status.rectTransform, 26, 26, 76, 479);
            BaUiWidgets.CreateFooterButton(built.Panel, "Close", new Vector2(0, 22), new Vector2(200, 42),
                Mathf.Clamp(built.Scale, 0.85f, 1.15f), T("bacg_close", "Cancel / close"), BaButtonStyle.Grey, Cancel);
            BaUi.ApplyLayer(_root);
        }
        private void Refresh()
        {
            if (_root == null || !_root.activeSelf || _loading) return;
            foreach (Transform child in _content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            int row = 0;
            AddRow(row++, T("bacg_original", "Original game (Brick Breaker)"), T("bacg_original_desc", "Included with Big Ambitions."), null);
            foreach (var game in ComputerGames.Catalog)
                AddRow(row++, ComputerGames.ResolveText(game.TitleKey, game.Title) + "  ·  " + game.Version,
                    ComputerGames.ResolveText(game.DescriptionKey, game.Description), game.Id);
            _content.sizeDelta = new Vector2(0, row * 102);
            _scroll.gameObject.SetActive(true); _scroll.verticalNormalizedPosition = 1;
            _status.text = T("bacg_lazy", "Game resources load only when you play.");
            BaUi.ApplyLayer(_root);
        }
        private void AddRow(int index, string title, string description, string id)
        {
            var row = Rect(_content, "Game_" + index); row.anchorMin = new Vector2(0, 1); row.anchorMax = Vector2.one;
            row.pivot = new Vector2(0.5f, 1); row.anchoredPosition = new Vector2(0, -index * 102); row.sizeDelta = new Vector2(0, 96);
            row.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.21f, 0.27f, 0.85f);
            var name = Label(row, "Title", title, 21); Stretch(name.rectTransform, 15, 152, 53, 8);
            name.fontStyle = FontStyles.Bold;
            var detail = Label(row, "Description", description, 15); Stretch(detail.rectTransform, 15, 152, 10, 43);
            var play = BaUiWidgets.CreateFooterButton(row, "Play", new Vector2(0, 25), new Vector2(125, 44), 1,
                T("bacg_play", "Play"), BaButtonStyle.Blue, () => _choose?.Invoke(id));
            var rect = (RectTransform)play.transform; rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-77, 25);
        }
        private static RectTransform Rect(Transform parent, string name)
        { var obj = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)obj.transform; rect.SetParent(parent, false); return rect; }
        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
        private static TextMeshProUGUI Label(Transform parent, string name, string text, int size)
        {
            var label = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.color = Color.white; label.raycastTarget = false;
            label.richText = false; label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft; return label;
        }
        public void Dispose()
        {
            ComputerGames.CatalogChanged -= Refresh;
            if (_root != null) { _root.SetActive(false); UnityEngine.Object.Destroy(_root); }
            _root = null; _choose = null;
        }
    }
}
