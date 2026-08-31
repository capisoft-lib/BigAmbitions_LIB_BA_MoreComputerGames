using System;
using System.IO;
using System.Reflection;
using Controllers;
using Steamworks;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    internal sealed class ComputerGameScoresRuntime : MonoBehaviour
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private FieldInfo _playing, _gameField, _menu, _lives, _score, _pending, _level, _initialLives, _brickScore;
        private object _game;
        private string _profile;
        private readonly VanillaRoundTracker _tracker = new VanillaRoundTracker();
        private bool _stopped;
        internal static string CurrentProfile => SteamClient.IsValid ? SteamClient.SteamId.ToString() : "offline";
        internal void Initialize()
        {
            _profile = CurrentProfile;
            try { ComputerGames.RecordStore = new ComputerGameRecordStore(Path.Combine(Application.persistentDataPath, "MoreComputerGames", "Records", _profile + ".json"), _profile, () => CurrentProfile == _profile); }
            catch (Exception e) { ComputerGames.RecordStore = null; Debug.LogWarning("[MCG] Local records unavailable; original file preserved. " + e.Message); }
            try {
                _playing = typeof(VideoGameSetup).GetField("PlayingInstance", BindingFlags.Static | BindingFlags.NonPublic);
                _gameField = typeof(VideoGameSetup).GetField("_gameInstance", Fields);
                var type = Type.GetType("Items.SpecialItems.VideoGames.BrickBreaker.BrickBreaker, BigAmbitions.ArcadeMachines.BrickBreaker", false);
                if (_playing == null || _gameField == null || type == null) throw new MissingMemberException("Native Brick Breaker API");
                _menu = Require(type, "_inMainMenu", typeof(bool)); _lives = Require(type, "_lives", typeof(int));
                _score = Require(type, "_score", typeof(int)); _pending = Require(type, "_pendingScoreChange", typeof(int));
                _level = Require(type, "_currentLevelIndex", typeof(int)); _initialLives = Require(type, "initialLives", typeof(int)); _brickScore = Require(type, "brickScore", typeof(int));
                ComputerGames.IsVanillaScoreTrackingActive = true;
            }
            catch (Exception e) { ComputerGames.IsVanillaScoreTrackingActive = false; Debug.LogWarning("[MCG] Vanilla score capture unavailable: " + e.Message); }
        }
        private static FieldInfo Require(Type type, string name, Type fieldType)
        {
            var field = type.GetField(name, Fields);
            if (field == null || field.FieldType != fieldType) throw new MissingFieldException(type.FullName, name);
            return field;
        }
        private void LateUpdate()
        {
            if (_stopped || !ComputerGames.IsVanillaScoreTrackingActive) return;
            if (!GameManager.IsInitialized || GameManager.isCitySceneBeingUnloaded || CurrentProfile != _profile) { Reset(); return; }
            try {
                var setup = _playing.GetValue(null) as VideoGameSetup;
                var game = setup == null ? null : _gameField.GetValue(setup);
                if (game == null || game is UnityEngine.Object unity && unity == null || !_menu.DeclaringType.IsInstanceOfType(game)) { Reset(); return; }
                if (!ReferenceEquals(game, _game)) { Reset(); _game = game; }
                var result = _tracker.Observe((bool)_menu.GetValue(game), (int)_lives.GetValue(game), (int)_score.GetValue(game),
                    (int)_pending.GetValue(game), (int)_level.GetValue(game), Time.realtimeSinceStartupAsDouble,
                    Time.timeScale > 0 ? Time.unscaledDeltaTime : 0, DateTime.UtcNow,
                    (int)_initialLives.GetValue(game) != 3 || (int)_brickScore.GetValue(game) != 100, Application.version, _profile);
                if (result != null) ComputerGames.Publish(result);
            }
            catch (Exception e) { ComputerGames.IsVanillaScoreTrackingActive = false; Reset(); ComputerGames.Report(e); }
        }
        private void Reset() { _game = null; _tracker.Reset(); }
        internal void Shutdown() { _stopped = true; Reset(); ComputerGames.IsVanillaScoreTrackingActive = false; ComputerGames.RecordStore = null; }
    }
}
