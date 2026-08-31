using System;
using System.Diagnostics;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    public sealed class ComputerGameResult
    {
        public string RoundId { get; } = Guid.NewGuid().ToString();
        public string GameId { get; }
        public string GameVersion { get; }
        public string Ruleset { get; }
        public long Score { get; }
        public int Level { get; }
        public DateTime StartedAtUtc { get; }
        public DateTime EndedAtUtc { get; }
        public double ActiveSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool ModifiedRules { get; }
        public long HighScore { get; internal set; }
        public bool IsNewHighScore { get; internal set; }
        public bool HighScoreSaveFailed { get; internal set; }
        internal string RecordProfileId { get; }
        internal ComputerGameResult(ComputerGameDefinition definition, long score, int level,
            DateTime start, DateTime end, double active, double elapsed)
            : this(definition.Id, definition.Version, definition.Ruleset, score, level, start, end, active, elapsed, false, ComputerGames.RecordProfileId) { }
        internal ComputerGameResult(string id, string version, string rules, long score, int level,
            DateTime start, DateTime end, double active, double elapsed, bool modified, string profile)
        { GameId = id; GameVersion = version; Ruleset = rules; Score = score;
          Level = level; StartedAtUtc = start; EndedAtUtc = end; ActiveSeconds = active; ElapsedSeconds = elapsed;
          ModifiedRules = modified; RecordProfileId = profile; }
    }

    public readonly struct ComputerGameFrame
    {
        public float DeltaSeconds { get; }
        public bool PrimaryPressed { get; }
        public bool RestartPressed { get; }
        public Vector2 CursorViewport { get; }
        public ComputerGameFrame(float delta, bool primary, bool restart, Vector2 cursor)
        { DeltaSeconds = delta; PrimaryPressed = primary; RestartPressed = restart; CursorViewport = cursor; }
    }

    public sealed class ComputerGameContext
    {
        private readonly ComputerGameSession _session;
        private readonly Stopwatch _clock = new Stopwatch();
        private DateTime _started;
        private double _active;
        private bool _round;
        private string _recordProfile;
        public string GameId => _session.Definition.Id;
        public string ModRootPath => _session.ModRootPath;
        public ComputerGameAssets Assets => _session.Assets;
        public bool IsClosed => _session.IsClosed;
        public long HighScore => ComputerGames.GetHighScore(GameId, _session.Definition.Ruleset);
        internal ComputerGameContext(ComputerGameSession session) { _session = session; }
        public string Text(string key, string fallback) => ComputerGames.ResolveText(key, fallback);
        public void RequestExit() { if (!IsClosed) _session.RequestExit(); }

        public void BeginRound()
        {
            if (IsClosed) return;
            _started = DateTime.UtcNow; _clock.Restart(); _active = 0; _round = true;
            _recordProfile = ComputerGames.RecordProfileId;
        }

        public bool CompleteRound(long score, int level = 0)
            => CompleteRound(score, level, false);

        public bool CompleteRound(long score, int level, bool modifiedRules)
        {
            if (IsClosed || !_round) return false;
            if (score < 0 || level < 0) throw new ArgumentOutOfRangeException(nameof(score));
            _round = false; _clock.Stop();
            var definition = _session.Definition;
            ComputerGames.Publish(new ComputerGameResult(definition.Id, definition.Version, definition.Ruleset, score, level,
                _started, DateTime.UtcNow, _active, _clock.Elapsed.TotalSeconds, modifiedRules, _recordProfile));
            return true;
        }

        internal void Advance(float seconds) { if (_round && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) _active += Math.Max(0, seconds); }
        internal void Abandon() { _round = false; _clock.Stop(); }
    }

    public interface IComputerGame
    {
        Camera Camera { get; }
        void Initialize(ComputerGameContext context);
        void Tick(ComputerGameFrame frame);
        void SetScreenResolution(int width, int height);
        void SetMusicState(bool enabled, Action<bool> toggleHandler);
        void Shutdown();
    }

    public abstract class ComputerGameBehaviour : MonoBehaviour, IComputerGame
    {
        public ComputerGameContext Context { get; private set; }
        public abstract Camera Camera { get; }
        private bool _initialized, _shutdown;
        public void Initialize(ComputerGameContext context)
        {
            if (_initialized || _shutdown) throw new InvalidOperationException("A game instance can initialize only once.");
            Context = context ?? throw new ArgumentNullException(nameof(context)); _initialized = true;
            OnInitialize();
        }
        public void Tick(ComputerGameFrame frame) { if (_initialized && !_shutdown) { Context.Advance(frame.DeltaSeconds); OnTick(frame); } }
        public void Shutdown() { if (_shutdown) return; _shutdown = true; if (_initialized) OnShutdown(); }
        protected abstract void OnInitialize();
        protected abstract void OnTick(ComputerGameFrame frame);
        protected virtual void OnShutdown() { }
        public virtual void SetScreenResolution(int width, int height) { }
        public virtual void SetMusicState(bool enabled, Action<bool> toggleHandler) { }
    }
}
