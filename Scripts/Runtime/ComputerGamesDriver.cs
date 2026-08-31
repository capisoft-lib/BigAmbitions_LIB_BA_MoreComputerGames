using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Controllers;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Capisoft.Lib.BaComputerGames
{
    internal sealed class ComputerGamesDriver : MonoBehaviour
    {
        private static readonly FieldInfo PrefabField = typeof(VideoGameSetup).GetField("gamePrefabReference", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo PlayingField = typeof(VideoGameSetup).GetField("PlayingInstance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo ButtonsField = typeof(CustomizableButtonsOverlay).GetField("_buttons", BindingFlags.NonPublic | BindingFlags.Instance);
        private CustomizableButtonsOverlay _overlay;
        private readonly ButtonActionOverride _playAction = new ButtonActionOverride();
        private ComputerController _computer;
        private ComputerGameSession _session;
        private VideoGameSetup _setup;
        private TrackedGameReference _reference;
        private CancellationTokenSource _selection;
        private ComputerGamesCatalog _catalog;
        private float _nextSearch;
        private int _selectionVersion;
        private bool _stopped;

        internal static void ValidateContract()
        {
            if (PrefabField?.FieldType != typeof(AssetReferenceGameObject) || PlayingField?.FieldType != typeof(VideoGameSetup) ||
                ButtonsField?.FieldType != typeof(List<Button>))
                throw new NotSupportedException("BaComputerGames: native computer API changed; catalog disabled, original game untouched.");
        }
        internal bool OwnsActiveSession(ComputerGameSession session) => !_stopped && ReferenceEquals(session, _session) &&
            !session.IsClosed && _setup != null && PlayingField.GetValue(null) as VideoGameSetup == _setup;

        private void LateUpdate()
        {
            if (_stopped) return;
            if (!GameManager.IsInitialized || GameManager.isCitySceneBeingUnloaded) { CancelSelection(); CloseSession(); RestorePlayAction(); _catalog?.Hide(); return; }
            if (_session != null && (!OwnsActiveSession(_session) || _reference != null && _reference.Failed)) CloseSession();
            _catalog?.Tick();
            if (_overlay == null && Time.unscaledTime >= _nextSearch)
            {
                _nextSearch = Time.unscaledTime + 1;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<CustomizableButtonsOverlay>())
                    if (candidate != null && candidate.gameObject.scene.IsValid()) { _overlay = candidate; break; }
            }
            if (_overlay == null || !_overlay.gameObject.activeInHierarchy || !(_overlay.linkedController is ComputerController computer) ||
                VideoGameSetup.IsAnyVideoGamePlaying() || !_overlay.ShouldShow(computer))
            { RestorePlayAction(); return; }
            _computer = computer;
            var buttons = (List<Button>)ButtonsField.GetValue(_overlay);
            Button playButton = null;
            foreach (var button in buttons)
            {
                if (button == null) continue;
                var localizer = button.GetComponentInChildren<TextLocalizationComponent>(true);
                if (localizer != null && localizer.Key == "playpanel_headline") { playButton = button; break; }
            }
            // Only replace the native computer action, never its label or a neighbouring activity.
            // The catalog still contains Brick Breaker when no additional game mod is enabled.
            if (playButton == null) { RestorePlayAction(); return; }
            _playAction.Bind(playButton, OpenCatalog);
        }
        private void OpenCatalog()
        {
            var computer = _computer;
            if (_stopped || computer == null || _overlay == null || !_overlay.gameObject.activeInHierarchy ||
                _overlay.linkedController != computer || !_overlay.ShouldShow(computer) || !PlayerActivityUI.CanStartActivity()) return;
            CancelSelection();
            if (_catalog == null) _catalog = new ComputerGamesCatalog(CancelSelection);
            _catalog.Show(id => Select(computer, id));
            InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay(); RestorePlayAction();
        }
        private void Select(ComputerController computer, string id)
        {
            CancelSelection();
            if (_stopped || computer == null || !PlayerActivityUI.CanStartActivity()) { _catalog.Hide(); return; }
            var cancellation = _selection = new CancellationTokenSource();
            var token = cancellation.Token;
            int version = _selectionVersion;
            _catalog.Loading();
            // Metadata only until the player actually reaches the selected computer.
            computer.MoveTowardsEntity(() => StartAtComputer(computer, id, token, version));
        }
        private async void StartAtComputer(ComputerController computer, string id, CancellationToken token, int version)
        {
            ComputerGameSession prepared = null;
            try
            {
                token.ThrowIfCancellationRequested();
                if (_stopped || computer == null || GameManager.isCitySceneBeingUnloaded || VideoGameSetup.IsAnyVideoGamePlaying())
                { if (version == _selectionVersion) _catalog?.Hide(); return; }
                if (id == null) { _catalog.Hide(); computer.StartVideoGame(); return; }
                prepared = await ComputerGames.PrepareAsync(id, token);
                token.ThrowIfCancellationRequested();
                if (_stopped || computer == null || GameManager.isCitySceneBeingUnloaded || VideoGameSetup.IsAnyVideoGamePlaying())
                { if (version == _selectionVersion) _catalog?.Hide(); return; }
                _setup = computer.GetComponentInChildren<VideoGameSetup>(true);
                if (_setup == null) throw new InvalidOperationException("No VideoGameSetup on this computer.");
                _session = prepared; prepared = null;
                _reference = new TrackedGameReference(_session.AddressKey);
                var original = (AssetReferenceGameObject)PrefabField.GetValue(_setup);
                try { PrefabField.SetValue(_setup, _reference); _catalog.Hide(); computer.StartVideoGame(); }
                finally { if (_setup != null) PrefabField.SetValue(_setup, original); }
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                ComputerGames.Report(error);
                // A loader from an abandoned selection must not close a newer game or reopen its old UI.
                if (!_stopped && version == _selectionVersion) { CloseSession(); _catalog?.Failed(); }
            }
            finally { prepared?.Dispose(); }
        }
        private void CancelSelection()
        {
            _selectionVersion++;
            var previous = _selection; _selection = null;
            if (previous == null) return;
            try { previous.Cancel(); } catch (Exception error) { ComputerGames.Report(error); }
            finally { previous.Dispose(); }
        }
        private void CloseSession()
        {
            var session = _session; _session = null;
            var setup = _setup; _setup = null;
            var reference = _reference; _reference = null;
            try
            {
                // Never finish another mod's game or the original game.
                if (setup != null && PlayingField.GetValue(null) as VideoGameSetup == setup) VideoGameSetup.RequestFinish();
            }
            catch (Exception error) { ComputerGames.Report(error); }
            finally { session?.Dispose(); reference?.ReleaseWhenSafe(); }
        }
        private void RestorePlayAction()
        {
            _playAction.Dispose(); _computer = null;
        }
        internal void Shutdown()
        {
            if (_stopped) return; _stopped = true;
            CancelSelection(); CloseSession(); RestorePlayAction(); _catalog?.Dispose(); _catalog = null;
        }
        private void OnDestroy() { Shutdown(); }
    }
}
