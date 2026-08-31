using System;
using System.Threading;
using Items.SpecialItems.VideoGames;
using UnityEngine;
using UnityEngine.Audio;

namespace Capisoft.Lib.BaComputerGames
{
    internal enum ComputerLauncherState { Menu, Loading, Playing, Error, Closed }

    // Keep the native activity alive; only the camera feeding its monitor changes.
    internal sealed class ComputerGameLauncher : ComputerGameBehaviour
    {
        internal readonly ComputerGamesCatalog Catalog = new ComputerGamesCatalog();
        internal ComputerLauncherState State { get; private set; }
        internal IVideoGame NativeGame => _native?.Game;
        private object _nativeKey;
        private Func<AudioMixerGroup> _mixer;
        private Func<bool> _canLeave;
        private ComputerMenuView _view;
        private ComputerGameSession _playing;
        private GameObject _gameRoot;
        private NativeComputerGame _native;
        private Camera _activeCamera;
        private RenderTexture _target;
        private CancellationTokenSource _loading;
        private IDisposable _menuEffects;
        private int _version, _loadFrame, _width = 960, _height = 540;
        private bool _music = true;
        private Action<bool> _musicToggle;
        private float _acceptInputAt;
        private ComputerGameDefinition _choice;
        public override Camera Camera => _view.Camera;
        internal void Configure(object nativeKey, Func<AudioMixerGroup> mixer, Func<bool> canLeave = null)
        { _nativeKey = nativeKey; _mixer = mixer; _canLeave = canLeave; }

        protected override void OnInitialize()
        {
            _view = new ComputerMenuView(transform);
            Catalog.Refresh(); ComputerGames.CatalogChanged += RefreshCatalog;
            State = ComputerLauncherState.Menu; _acceptInputAt = Time.unscaledTime + .4f;
            Draw();
        }
        private void RefreshCatalog() { Catalog.Refresh(); if (State == ComputerLauncherState.Menu) Draw(); }
        public override void SetScreenResolution(int width, int height)
        {
            _width = width; _height = height; _target = _view.Camera.targetTexture;
            _view.SetResolution(width, height);
            if (State != ComputerLauncherState.Playing && _menuEffects == null)
                BeginMenuDisplay();
        }
        public override void SetMusicState(bool enabled, Action<bool> toggleHandler)
        { _music = enabled; _musicToggle = toggleHandler; }
        private void MusicChanged(bool enabled) { _music = enabled; _musicToggle?.Invoke(enabled); }
        protected override void OnTick(ComputerGameFrame frame)
        {
            if (State == ComputerLauncherState.Closed) return;
            bool ready = Time.unscaledTime >= _acceptInputAt;
            HandleInput(ready ? (Input.GetKeyDown(KeyCode.DownArrow) ? 1 : Input.GetKeyDown(KeyCode.UpArrow) ? -1 : 0) : 0,
                ready && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)),
                ready && Input.GetKeyDown(KeyCode.Backspace), ready && LeaveKeyPressed());
            if (State == ComputerLauncherState.Closed) return;
            if (State == ComputerLauncherState.Loading && _loading == null && Time.frameCount > _loadFrame)
                LoadSelected(); // Let the loading screen render even with a synchronous loader.
            if (State != ComputerLauncherState.Playing) { _view.Animate(Time.unscaledTime); return; }
            if (_playing != null)
            {
                if (_playing.IsClosed) { ReturnToMenu(); return; }
                try
                {
                    if (!(_playing.Instance is ComputerGameBehaviour)) _playing.Context.Advance(frame.DeltaSeconds);
                    _playing.Instance.Tick(Time.unscaledTime >= _acceptInputAt ? frame :
                        new ComputerGameFrame(frame.DeltaSeconds, false, false, frame.CursorViewport));
                }
                catch (Exception error) { ShowFailure(error); }
            }
        }
        private static bool LeaveKeyPressed() => Input.GetKeyDown(KeyCode.Tab) &&
            !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) &&
            !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
            !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) &&
            !Input.GetKey(KeyCode.LeftCommand) && !Input.GetKey(KeyCode.RightCommand);

        internal void HandleInput(int direction, bool confirm, bool back, bool leave = false)
        {
            if (State == ComputerLauncherState.Closed) return;
            if (leave)
            {
                // Close only our launcher session. The owning driver finishes the native activity.
                // Escape and native input bindings are never consumed or reset by MCG.
                if (_canLeave == null || _canLeave()) Context.RequestExit();
                return;
            }
            if (back && State != ComputerLauncherState.Menu) { ReturnToMenu(); return; }
            if (State != ComputerLauncherState.Menu && State != ComputerLauncherState.Error) return;
            if (direction != 0) { State = ComputerLauncherState.Menu; Catalog.Move(direction); Draw(); }
            if (!confirm) return;
            _choice = Catalog.Selected; State = ComputerLauncherState.Loading;
            _loadFrame = Time.frameCount; Draw();
        }
        private async void LoadSelected()
        {
            var cancellation = _loading = new CancellationTokenSource();
            var token = cancellation.Token; int version = ++_version;
            ComputerGameSession prepared = null;
            NativeComputerGame native = null;
            try
            {
                if (_choice.Id == ComputerGames.VanillaBrickBreakerId)
                    native = await NativeComputerGame.LoadAsync(_nativeKey, transform, token);
                else prepared = await ComputerGames.PrepareAsync(_choice.Id, token);
                token.ThrowIfCancellationRequested();
                if (Context.IsClosed || State != ComputerLauncherState.Loading || version != _version)
                    throw new OperationCanceledException();
                ComputerGames.SafeDispose(_menuEffects); _menuEffects = null;
                if (prepared != null)
                {
                    _playing = prepared; prepared = null;
                    _gameRoot = new GameObject("MCG_Gameplay"); _gameRoot.transform.SetParent(transform, false);
                    var game = _playing.Definition.Factory(_gameRoot);
                    if (!(game is MonoBehaviour component) || component == null || component.gameObject != _gameRoot)
                        throw new InvalidOperationException("Game factory must attach its component to the supplied root.");
                    _playing.Instance = game; game.Initialize(_playing.Context);
                    if (_playing.IsClosed) { ReturnToMenu(); return; }
                    _activeCamera = game.Camera;
                    BindCamera(); game.SetScreenResolution(_width, _height); _playing.PrepareDisplay();
                    game.SetMusicState(_music, MusicChanged);
                }
                else
                {
                    _native = native; native = null; _gameRoot = _native.Root;
                    _gameRoot.SetActive(true);
                    _activeCamera = _native.Game.GetCamera();
                    BindCamera(); _native.Game.SetScreenResolution(_width, _height);
                    _native.Game.SetMusicState(_music, MusicChanged);
                }
                var mixer = _mixer?.Invoke();
                foreach (var source in _gameRoot.GetComponentsInChildren<AudioSource>(true)) source.outputAudioMixerGroup = mixer;
                State = ComputerLauncherState.Playing; _acceptInputAt = Time.unscaledTime + .25f;
            }
            catch (OperationCanceledException)
            { if (version == _version && State != ComputerLauncherState.Closed) ReturnToMenu(); }
            catch (Exception error)
            { if (version == _version && State != ComputerLauncherState.Closed) ShowFailure(error); }
            finally
            {
                prepared?.Dispose(); native?.Dispose();
                if (ReferenceEquals(_loading, cancellation)) { _loading = null; cancellation.Dispose(); }
            }
        }
        private void BindCamera()
        {
            if (_activeCamera == null) throw new InvalidOperationException("Game must provide a camera after initialization.");
            _activeCamera.targetTexture = _target; _view.Root.SetActive(false);
        }
        private void ShowFailure(Exception error)
        { ComputerGames.Report(error); ReturnToMenu(); State = ComputerLauncherState.Error; Draw(); }
        internal void ReturnToMenu()
        {
            CancelLoad(); ReleaseGame();
            if (Context.IsClosed || State == ComputerLauncherState.Closed) return;
            _view.Root.SetActive(true); _view.Camera.targetTexture = _target;
            ComputerGames.SafeDispose(_menuEffects);
            _menuEffects = null; BeginMenuDisplay();
            State = ComputerLauncherState.Menu; Catalog.Refresh();
            _acceptInputAt = Time.unscaledTime + .2f; Draw();
        }
        private void CancelLoad()
        {
            _version++;
            var previous = _loading; _loading = null;
            if (previous == null) return;
            try { previous.Cancel(); } catch (Exception error) { ComputerGames.Report(error); }
            finally { previous.Dispose(); }
        }
        private void BeginMenuDisplay()
        {
            try { _menuEffects = ComputerGames.BeginDisplaySession?.Invoke(); }
            catch (Exception error) { ComputerGames.Report(error); }
        }
        private void ReleaseGame()
        {
            if (_activeCamera != null) _activeCamera.targetTexture = null;
            _activeCamera = null;
            if (_gameRoot != null) _gameRoot.SetActive(false);
            _playing?.Dispose(); _playing = null;
            if (_native != null) { _native.Dispose(); _native = null; }
            else if (_gameRoot != null) Destroy(_gameRoot);
            _gameRoot = null;
        }
        private void Draw() => _view.Draw(Catalog, State, _choice);
        protected override void OnShutdown()
        {
            State = ComputerLauncherState.Closed;
            ComputerGames.CatalogChanged -= RefreshCatalog;
            CancelLoad(); ReleaseGame();
            ComputerGames.SafeDispose(_menuEffects); _menuEffects = null;
            if (_view != null) { _view.Dispose(); _view = null; }
        }
    }
}
