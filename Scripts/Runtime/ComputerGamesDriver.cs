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
using UnityEngine.Audio;

namespace Capisoft.Lib.BaComputerGames
{
    internal sealed class ComputerGamesDriver : MonoBehaviour
    {
        private static readonly FieldInfo PrefabField = typeof(VideoGameSetup).GetField("gamePrefabReference", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo PlayingField = typeof(VideoGameSetup).GetField("PlayingInstance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo ButtonsField = typeof(CustomizableButtonsOverlay).GetField("_buttons", BindingFlags.NonPublic | BindingFlags.Instance);
        private CustomizableButtonsOverlay _overlay;
        private readonly ButtonActionOverride _playAction = new ButtonActionOverride();
        private ComputerReturnButton _returnButton;
        private bool _returnButtonFailed;
        private ComputerController _computer;
        private ComputerGameSession _session;
        private VideoGameSetup _setup;
        private TrackedGameReference _reference;
        private CancellationTokenSource _selection;
        private ComputerGameRegistration _launcherRegistration;
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
            if (!GameManager.IsInitialized || GameManager.isCitySceneBeingUnloaded) { CancelSelection(); CloseSession(); RestorePlayAction(); return; }
            if (_session != null && (!OwnsActiveSession(_session) || _reference != null && _reference.Failed)) CloseSession();
            UpdateReturnButton();
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
            _playAction.Bind(playButton, OpenLauncher);
        }
        private void OpenLauncher()
        {
            var computer = _computer;
            if (_stopped || computer == null || _overlay == null || !_overlay.gameObject.activeInHierarchy ||
                _overlay.linkedController != computer || !_overlay.ShouldShow(computer) || !PlayerActivityUI.CanStartActivity()) return;
            CancelSelection();
            var cancellation = _selection = new CancellationTokenSource();
            var token = cancellation.Token;
            int version = _selectionVersion;
            InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay(); RestorePlayAction();
            // No catalog popup or gameplay loads while the player is walking.
            computer.MoveTowardsEntity(() => StartAtComputer(computer, token, version));
        }
        private void StartAtComputer(ComputerController computer, CancellationToken token, int version)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (_stopped || computer == null || GameManager.isCitySceneBeingUnloaded || VideoGameSetup.IsAnyVideoGamePlaying())
                    return;
                _setup = computer.GetComponentInChildren<VideoGameSetup>(true);
                if (_setup == null) throw new InvalidOperationException("No VideoGameSetup on this computer.");
                var original = (AssetReferenceGameObject)PrefabField.GetValue(_setup);
                var definition = new ComputerGameDefinition("mcg:launcher", "More Computer Games", "", ComputerGames.ApiVersion, root =>
                {
                    var launcher = root.AddComponent<ComputerGameLauncher>();
                    launcher.Configure(original.RuntimeKey, GetNativeMixer, CanLeaveComputer); return launcher;
                });
                _launcherRegistration = new ComputerGameRegistration("LIB_BaComputerGames", "", definition);
                _session = new ComputerGameSession(_launcherRegistration, null);
                ComputerGames.Sessions.Add(_session.AddressKey, _session);
                _reference = new TrackedGameReference(_session.AddressKey);
                try { PrefabField.SetValue(_setup, _reference); computer.StartVideoGame(); }
                finally { if (_setup != null) PrefabField.SetValue(_setup, original); }
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                ComputerGames.Report(error);
                if (!_stopped && version == _selectionVersion) CloseSession();
            }
        }
        private static bool CanLeaveComputer() => !UI.MiniMenu.MiniMenu.IsOpen &&
            !Scenes.MainMenu.Options.IsVisible && !GameManager.isCitySceneBeingUnloaded &&
            !GameManager.ShouldBlockKeyboardShortcuts();
        private bool CanReturnToMenu() => !_stopped && _session != null && OwnsActiveSession(_session) &&
            _session.Instance is ComputerGameLauncher launcher && launcher.State != ComputerLauncherState.Menu &&
            launcher.State != ComputerLauncherState.Closed && Application.isFocused &&
            (ComputerGames.InputAllowed == null || ComputerGames.InputAllowed());
        private void UpdateReturnButton()
        {
            if (_returnButtonFailed || _session == null || !OwnsActiveSession(_session) ||
                !(_session.Instance is ComputerGameLauncher launcher)) return;
            bool visible = launcher.State != ComputerLauncherState.Menu && launcher.State != ComputerLauncherState.Closed;
            var leave = InstanceBehavior<UI.UIs>.Instance?.playerHUD?.itemPanelUI?.leaveButton;
            if (leave == null) { _returnButton?.Dispose(); _returnButton = null; return; }
            try
            {
                if (_returnButton != null && !_returnButton.Uses(leave)) { _returnButton.Dispose(); _returnButton = null; }
                if (_returnButton == null && visible)
                    _returnButton = new ComputerReturnButton(leave, () => launcher.HandleInput(0, false, true), CanReturnToMenu,
                        button =>
                        {
                            // The clone gets its own translated label; native Leave remains untouched.
                            foreach (var localizer in button.GetComponentsInChildren<TextLocalizationComponent>(true))
                                localizer.SetData(default(LanguageChangeEventDataHolder));
                        });
                _returnButton?.Refresh(visible);
            }
            catch (Exception error)
            {
                _returnButton?.Dispose(); _returnButton = null; _returnButtonFailed = true;
                ComputerGames.Report(error); // Backspace still works if the native panel changes.
            }
        }
        private static AudioMixerGroup GetNativeMixer() => BuildingManager.IsInsideBuilding &&
            !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()
                ? InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup
                : InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup;
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
            _returnButton?.Dispose(); _returnButton = null; _returnButtonFailed = false;
            var session = _session; _session = null;
            var setup = _setup; _setup = null;
            var reference = _reference; _reference = null;
            try
            {
                // Never finish another mod's game or the original game.
                if (setup != null && PlayingField.GetValue(null) as VideoGameSetup == setup) VideoGameSetup.RequestFinish();
            }
            catch (Exception error) { ComputerGames.Report(error); }
            finally
            {
                session?.Dispose(); reference?.ReleaseWhenSafe();
                _launcherRegistration?.Dispose(); _launcherRegistration = null;
            }
        }
        private void RestorePlayAction()
        {
            _playAction.Dispose(); _computer = null;
        }
        internal void Shutdown()
        {
            if (_stopped) return; _stopped = true;
            CancelSelection(); CloseSession(); RestorePlayAction();
        }
        private void OnDestroy() { Shutdown(); }
    }
}
