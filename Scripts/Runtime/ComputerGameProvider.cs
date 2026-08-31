using System;
using System.Collections.Generic;
using Items.SpecialItems.VideoGames;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Capisoft.Lib.BaComputerGames
{
    public sealed class ComputerGameProvider : ResourceProviderBase
    {
        public static int LiveTemplates { get; private set; }
        public static int LiveGames { get; internal set; }
        private readonly HashSet<GameObject> _templates = new HashSet<GameObject>();
        public override Type GetDefaultType(IResourceLocation location) => typeof(GameObject);
        public IResourceLocator CreateLocator() => new SessionLocator(ProviderId);

        public override void Provide(ProvideHandle handle)
        {
            GameObject storage = null;
            GameObject template = null;
            try
            {
                if (!ComputerGames.Sessions.TryGetValue(handle.Location.PrimaryKey, out var session) || session.IsClosed)
                    throw new OperationCanceledException("Game session has ended.");
                storage = new GameObject("BaComputerGames_Template"); storage.SetActive(false);
                template = new GameObject(session.Definition.Id);
                template.transform.SetParent(storage.transform, false);
                template.AddComponent<NativeGameBridge>().SessionKey = session.AddressKey;
                _templates.Add(template); LiveTemplates++;
                handle.Complete(template, true, null);
            }
            catch (Exception error)
            {
                if (!ReferenceEquals(template, null) && _templates.Remove(template)) LiveTemplates--;
                if (storage != null) UnityEngine.Object.Destroy(storage);
                handle.Complete<GameObject>(null, false, error);
            }
        }
        public override void Release(IResourceLocation location, object asset)
        {
            if (asset is GameObject template && _templates.Remove(template))
            {
                LiveTemplates--;
                if (template == null) return; // Scene teardown may have destroyed it before Addressables release.
                var root = template.transform.parent;
                UnityEngine.Object.Destroy(root != null ? root.gameObject : template);
            }
        }

        private sealed class SessionLocator : IResourceLocator
        {
            private readonly string _provider;
            public SessionLocator(string provider) { _provider = provider; }
            public string LocatorId => "capisoft.computer-games.sessions";
            public IEnumerable<object> Keys { get { foreach (var key in ComputerGames.Sessions.Keys) yield return key; } }
            public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
            {
                locations = null;
                if (!(key is string id) || (type != null && !type.IsAssignableFrom(typeof(GameObject))) ||
                    !ComputerGames.Sessions.TryGetValue(id, out var session) || session.IsClosed) return false;
                locations = new[] { new ResourceLocationBase(id, "local:" + session.Definition.Id + "/" + session.AddressKey, _provider, typeof(GameObject)) };
                return true;
            }
        }
    }

    // Only this internal adapter knows the game's IVideoGame contract.
    internal sealed class NativeGameBridge : MonoBehaviour, IVideoGame
    {
        [SerializeField] internal string SessionKey;
        private ComputerGameSession _session;
        private IComputerGame _game;
        private bool _created;
        private float _acceptInputAt;
        internal IVideoGame ActiveNativeGame => (_game as ComputerGameLauncher)?.NativeGame;

        public Camera GetCamera()
        {
            if (_created) return _session != null && !_session.IsClosed ? _game?.Camera : null;
            if (!ComputerGames.Sessions.TryGetValue(SessionKey ?? "", out _session) || _session.IsClosed ||
                (ComputerGames.SessionAllowed != null && !ComputerGames.SessionAllowed(_session))) return null;
            try
            {
                _created = true; ComputerGameProvider.LiveGames++;
                var root = new GameObject("Gameplay"); root.transform.SetParent(transform, false);
                _game = _session.Definition.Factory(root);
                if (!(_game is MonoBehaviour component) || component == null || component.gameObject != root) throw new InvalidOperationException("Game factory must attach its component to the supplied root.");
                _session.Instance = _game; _game.Initialize(_session.Context);
                if (_session.IsClosed) return null;
                if (_game.Camera == null) throw new InvalidOperationException("Game must provide a camera after initialization.");
                _acceptInputAt = Time.unscaledTime + 0.35f;
                return _game.Camera;
            }
            catch (Exception error) { Fail(error); return null; }
        }

        public float GetCoinPrice() => 0;
        public void SetScreenResolution(int width, int height)
        {
            if (_game == null || _session.IsClosed) return;
            try { _game.SetScreenResolution(width, height); _session.PrepareDisplay(); } catch (Exception error) { Fail(error); }
        }
        public void SetMusicState(bool enabled, Action<bool> toggleHandler)
        { if (_game != null && !_session.IsClosed) try { _game.SetMusicState(enabled, toggleHandler); } catch (Exception error) { Fail(error); } }

        private void Update()
        {
            if (_game == null || _session.IsClosed || !Application.isFocused ||
                (ComputerGames.InputAllowed != null && !ComputerGames.InputAllowed())) return;
            try
            {
                bool ready = Time.unscaledTime >= _acceptInputAt;
                if (!(_game is ComputerGameBehaviour)) _session.Context.Advance(Mathf.Min(Time.unscaledDeltaTime, 0.25f));
                _game.Tick(new ComputerGameFrame(Mathf.Min(Time.unscaledDeltaTime, 0.25f),
                    ready && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)),
                    ready && Input.GetKeyDown(KeyCode.R), IVideoGame.CursorViewportPosition));
            }
            catch (Exception error) { Fail(error); }
        }
        private void Fail(Exception error) { ComputerGames.Report(error); _session?.Dispose(); }
        private void OnDestroy()
        {
            if (!_created) return;
            ComputerGameProvider.LiveGames--; _session?.Dispose(); _session = null; _game = null;
        }
    }
}
