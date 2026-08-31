using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Capisoft.Lib.BaComputerGames
{
    // Registration APIs and all Unity-facing continuations are main-thread only.
    public static class ComputerGames
    {
        public const string ApiVersion = "0.2.0";
        public const string VanillaBrickBreakerId = "vanilla:brick-breaker";
        public const string VanillaBrickBreakerRuleset = "ba-1.0-standard";
        public static bool IsVanillaScoreTrackingActive { get; internal set; }
        internal static ComputerGameRecordStore RecordStore;
        internal static string RecordProfileId => LocalRecordsAvailable ? RecordStore.ProfileId : null;
        public static bool LocalRecordsAvailable => RecordStore != null && RecordStore.IsAvailable;
        public static long GetHighScore(string gameId, string ruleset = "default-v1", bool modifiedRules = false)
            => TryGetHighScore(gameId, ruleset, out var record, modifiedRules) ? record.Score : 0;
        public static bool TryGetHighScore(string gameId, string ruleset, out ComputerGameRecord record, bool modifiedRules = false)
        {
            record = null;
            return RecordStore != null && RecordStore.TryGet(gameId, ruleset, modifiedRules, out record);
        }
        private static readonly Dictionary<string, ComputerGameRegistration> Entries = new Dictionary<string, ComputerGameRegistration>(StringComparer.Ordinal);
        internal static readonly Dictionary<string, ComputerGameSession> Sessions = new Dictionary<string, ComputerGameSession>(StringComparer.Ordinal);
        public static bool IsHostActive { get; private set; }
        private static CancellationTokenSource _hostCancellation;
        public static event Action CatalogChanged;
        public static event Action<ComputerGameResult> RoundCompleted;
        public static event Action<ComputerGameRecord> HighScoreChanged;
        public static event Action<Exception> Error;
        internal static Func<string, string, string> Translator;
        internal static Func<bool> InputAllowed;
        internal static Func<ComputerGameSession, bool> SessionAllowed;
        internal static Func<IDisposable> BeginDisplaySession;
        private static IReadOnlyList<ComputerGameDefinition> _catalog = Array.AsReadOnly(Array.Empty<ComputerGameDefinition>());
        public static IReadOnlyList<ComputerGameDefinition> Catalog => _catalog;

        public static ComputerGameRegistration Register(string ownerModId, string modRootPath, ComputerGameDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(ownerModId)) throw new ArgumentException("Owner mod id is required.", nameof(ownerModId));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Id == VanillaBrickBreakerId) throw new ArgumentException("The vanilla Brick Breaker id is reserved.", nameof(definition));
            if (Entries.ContainsKey(definition.Id)) throw new InvalidOperationException("Game id already registered: " + definition.Id);
            var registration = new ComputerGameRegistration(ownerModId, modRootPath ?? "", definition);
            Entries.Add(definition.Id, registration); NotifyCatalog(); return registration;
        }

        public static async Task<ComputerGameSession> PrepareAsync(string gameId, CancellationToken cancellationToken = default)
        {
            if (!IsHostActive) throw new InvalidOperationException("Enable LIB_BaComputerGames before launching a game.");
            if (!Entries.TryGetValue(gameId, out var registration)) throw new KeyNotFoundException(gameId);
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, registration.Cancellation, _hostCancellation.Token))
            {
                ComputerGameAssets assets = null;
                try
                {
                    linked.Token.ThrowIfCancellationRequested();
                    if (registration.Definition.Loader != null)
                        assets = await registration.Definition.Loader.LoadAsync(new ComputerGameLoadContext(gameId, registration.ModRootPath), linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (!IsHostActive) throw new OperationCanceledException("Computer games host was unloaded.");
                    if (!Entries.TryGetValue(gameId, out var current) || !ReferenceEquals(current, registration)) throw new OperationCanceledException("Game was unregistered.");
                    var session = new ComputerGameSession(registration, assets);
                    Sessions.Add(session.AddressKey, session); assets = null;
                    return session;
                }
                finally { SafeDispose(assets); }
            }
        }

        public static void ActivateHost()
        {
            if (IsHostActive) return;
            _hostCancellation = new CancellationTokenSource(); IsHostActive = true; NotifyCatalog();
        }
        public static void DeactivateHost()
        {
            if (!IsHostActive) return;
            IsHostActive = false;
            try { _hostCancellation.Cancel(); } catch (Exception e) { Report(e); }
            _hostCancellation.Dispose(); _hostCancellation = null;
            foreach (var session in Sessions.Values.ToArray()) session.Dispose();
            NotifyCatalog();
        }

        internal static void Unregister(ComputerGameRegistration entry)
        {
            if (!Entries.TryGetValue(entry.Definition.Id, out var existing) || !ReferenceEquals(existing, entry)) return;
            Entries.Remove(entry.Definition.Id);
            foreach (var session in Sessions.Values.Where(s => ReferenceEquals(s.Registration, entry)).ToArray()) session.Dispose();
            NotifyCatalog();
        }
        internal static string ResolveText(string key, string fallback)
        { if (string.IsNullOrEmpty(key) || Translator == null) return fallback; try { return Translator(key, fallback); } catch { return fallback; } }
        internal static void Publish(ComputerGameResult result)
        {
            ComputerGameRecord changed = null;
            if (RecordStore != null) {
                try { changed = RecordStore.Record(result); }
                catch (Exception e) { result.HighScoreSaveFailed = true; Report(e); }
            }
            else result.HighScoreSaveFailed = true;
            result.HighScore = GetHighScore(result.GameId, result.Ruleset, result.ModifiedRules);
            result.IsNewHighScore = changed != null;
            if (RoundCompleted != null) foreach (Action<ComputerGameResult> handler in RoundCompleted.GetInvocationList()) try { handler(result); } catch (Exception e) { Report(e); }
            if (changed != null && HighScoreChanged != null)
                foreach (Action<ComputerGameRecord> handler in HighScoreChanged.GetInvocationList()) try { handler(changed); } catch (Exception e) { Report(e); }
        }
        internal static void Report(Exception error)
        { if (Error != null) foreach (Action<Exception> handler in Error.GetInvocationList()) try { handler(error); } catch { } }
        internal static void SafeDispose(IDisposable value) { try { value?.Dispose(); } catch (Exception e) { Report(e); } }
        private static void NotifyCatalog()
        {
            _catalog = Array.AsReadOnly(Entries.Values.Select(e => e.Definition).OrderBy(d => d.Title, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Id, StringComparer.Ordinal).ToArray());
            if (CatalogChanged != null) foreach (Action handler in CatalogChanged.GetInvocationList()) try { handler(); } catch (Exception e) { Report(e); }
        }
    }

    public sealed class ComputerGameRegistration : IDisposable
    {
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private bool _disposed;
        public string OwnerModId { get; }
        public string ModRootPath { get; }
        public ComputerGameDefinition Definition { get; }
        internal CancellationToken Cancellation => _cancel.Token;
        internal ComputerGameRegistration(string owner, string root, ComputerGameDefinition definition) { OwnerModId = owner; ModRootPath = root; Definition = definition; }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            try { _cancel.Cancel(); } catch (Exception e) { ComputerGames.Report(e); }
            ComputerGames.Unregister(this); _cancel.Dispose();
        }
    }

    public sealed class ComputerGameSession : IDisposable
    {
        internal ComputerGameRegistration Registration { get; }
        public string AddressKey { get; } = Guid.NewGuid().ToString("N");
        public ComputerGameDefinition Definition => Registration.Definition;
        public string ModRootPath => Registration.ModRootPath;
        public ComputerGameAssets Assets { get; }
        public ComputerGameContext Context { get; }
        public bool IsClosed { get; private set; }
        internal IComputerGame Instance;
        private IDisposable _displayScope;
        private bool _displayAttempted;
        internal ComputerGameSession(ComputerGameRegistration registration, ComputerGameAssets assets)
        { Registration = registration; Assets = assets; Context = new ComputerGameContext(this); }
        public void RequestExit() { Dispose(); }
        internal void PrepareDisplay()
        {
            if (IsClosed || _displayAttempted || Definition.UseNativeRetroEffects) return;
            _displayAttempted = true;
            try { _displayScope = ComputerGames.BeginDisplaySession?.Invoke(); }
            catch (Exception error) { ComputerGames.Report(error); } // Optional rendering must not prevent play.
        }
        public void Dispose()
        {
            if (IsClosed) return; IsClosed = true; Context.Abandon();
            ComputerGames.Sessions.Remove(AddressKey);
            try { Instance?.Shutdown(); } catch (Exception e) { ComputerGames.Report(e); }
            ComputerGames.SafeDispose(_displayScope); _displayScope = null;
            ComputerGames.SafeDispose(Assets);
        }
    }
}
