using System;
using System.IO;
using Capisoft.Lib.BaComputerGames;

// Shared fixture: both .NET and Unity run the actual managed JSON persistence code.
public static class RecordsHarness
{
    public static void Run(Action<bool, string> check, string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "profile.json");
        bool current = true;
        var store = new ComputerGameRecordStore(path, "profile", () => current);
        ComputerGames.RecordStore = store;
        ComputerGames.ActivateHost();
        int rounds = 0, changes = 0, errors = 0;
        ComputerGameResult last = null;
        Action<ComputerGameResult> receive = result => { rounds++; last = result; };
        Action<ComputerGameRecord> changed = _ => changes++;
        Action<Exception> error = _ => errors++;
        ComputerGames.RoundCompleted += receive; ComputerGames.HighScoreChanged += changed; ComputerGames.Error += error;
        var definition = new ComputerGameDefinition("tests:records", "Record fixture", "", "1.0", _ => null);
        var registration = ComputerGames.Register("records-tests", directory, definition);
        var session = ComputerGames.PrepareAsync(definition.Id).GetAwaiter().GetResult();
        try {
            check(!File.Exists(path) && session.Context.HighScore == 0, "Records: read/registration does not create a file");
            check(!session.Context.CompleteRound(100), "Records: no save without a round");
            session.Context.BeginRound(); session.Context.CompleteRound(0);
            check(rounds == 1 && changes == 0 && !File.Exists(path), "Records: zero result emits completion without improving zero baseline");
            session.Context.BeginRound(); session.Context.CompleteRound(42, 3);
            check(rounds == 2 && changes == 1 && last.IsNewHighScore && last.HighScore == 42 && !last.HighScoreSaveFailed,
                "Records: completion saves a new best before notifying observers");
            check(session.Context.HighScore == 42 && File.Exists(path), "Records: context reads persistent best");
            check(!session.Context.CompleteRound(999) && changes == 1, "Records: double completion cannot save twice");
            var saved = File.ReadAllText(path); var written = File.GetLastWriteTimeUtc(path);
            session.Context.BeginRound(); session.Context.CompleteRound(42);
            session.Context.BeginRound(); session.Context.CompleteRound(41);
            check(rounds == 4 && changes == 1 && !last.IsNewHighScore && last.HighScore == 42 &&
                File.ReadAllText(path) == saved && File.GetLastWriteTimeUtc(path) == written && !File.Exists(path + ".bak"),
                "Records: equal/lower results emit events but never rewrite file");
            var reloaded = new ComputerGameRecordStore(path, "profile", () => true);
            check(reloaded.TryGet(definition.Id, definition.Ruleset, false, out var restored) && restored.Score == 42 && restored.Level == 3 && restored.GameVersion == "1.0",
                "Records: real file roundtrip restores score and metadata");
            Action<ComputerGameResult> brokenRound = _ => throw new Exception("fixture subscriber");
            Action<ComputerGameRecord> brokenRecord = _ => throw new Exception("fixture record subscriber");
            ComputerGames.RoundCompleted += brokenRound; ComputerGames.HighScoreChanged += brokenRecord;
            session.Context.BeginRound(); session.Context.CompleteRound((long)int.MaxValue + 10);
            ComputerGames.RoundCompleted -= brokenRound; ComputerGames.HighScoreChanged -= brokenRecord;
            check(errors == 2 && changes == 2 && last.IsNewHighScore && session.Context.HighScore == (long)int.MaxValue + 10,
                "Records: subscriber exceptions cannot prevent long-score persistence");
            check(File.ReadAllText(path + ".bak") == saved, "Records: atomic replacement retains previous file backup");
            reloaded = new ComputerGameRecordStore(path, "profile", () => true);
            check(reloaded.TryGet(definition.Id, definition.Ruleset, false, out restored) && restored.Score == (long)int.MaxValue + 10,
                "Records: serializer retains 64-bit score precision");
            session.Context.BeginRound(); session.Context.CompleteRound(50, 1, true);
            check(ComputerGames.GetHighScore(definition.Id, definition.Ruleset, true) == 50 && session.Context.HighScore > int.MaxValue,
                "Records: modified and standard rules never overwrite each other");
            ComputerGames.Publish(Result("other:game", "default-v1", 7));
            ComputerGames.Publish(Result(definition.Id, "hard-v2", 8));
            check(ComputerGames.GetHighScore("other:game") == 7 && ComputerGames.GetHighScore(definition.Id, "hard-v2") == 8,
                "Records: independent game and ruleset keys");
            session.Context.BeginRound(); current = false; var beforeSwitch = File.ReadAllText(path);
            session.Context.CompleteRound(long.MaxValue);
            check(last.HighScoreSaveFailed && !last.IsNewHighScore && File.ReadAllText(path) == beforeSwitch && session.Context.HighScore == 0,
                "Records: profile change blocks cross-account read/write");
            current = true;
            var eventsBeforeAbandon = rounds;
            session.Context.BeginRound(); session.Dispose();
            check(!session.Context.CompleteRound(long.MaxValue) && rounds == eventsBeforeAbandon && File.ReadAllText(path) == beforeSwitch,
                "Records: closing a live round never saves an abandoned score");
            string corrupt = Path.Combine(directory, "corrupt.json"); File.WriteAllText(corrupt, "{ broken");
            bool rejected = false;
            try { new ComputerGameRecordStore(corrupt, "profile", () => true); } catch { rejected = true; }
            check(rejected && File.ReadAllText(corrupt) == "{ broken", "Records: corrupt JSON remains untouched");
            string future = Path.Combine(directory, "future.json"); File.WriteAllText(future, "{\"schemaVersion\":99,\"profileId\":\"profile\",\"records\":[]}");
            rejected = false; try { new ComputerGameRecordStore(future, "profile", () => true); } catch { rejected = true; }
            check(rejected && File.ReadAllText(future).Contains("99"), "Records: unknown schema remains untouched");
            rejected = false; try { new ComputerGameRecordStore(path, "different-profile", () => true); } catch { rejected = true; }
            check(rejected && File.ReadAllText(path) == beforeSwitch, "Records: file profile mismatch is rejected without reset");
            string legacy = Path.Combine(directory, "legacy-header.json");
            string header = "{\n    \"schemaVersion\": 1,\n    \"profileId\": \"profile\"\n}";
            File.WriteAllText(legacy, header);
            var recovered = new ComputerGameRecordStore(legacy, "profile", () => true);
            check(!recovered.TryGet(definition.Id, definition.Ruleset, false, out _) && File.ReadAllText(legacy) == header,
                "Records: known header-only legacy file opens without fabricating or rewriting scores");
            recovered.Record(Result(definition.Id, definition.Ruleset, 35));
            var recoveredReload = new ComputerGameRecordStore(legacy, "profile", () => true);
            check(recoveredReload.TryGet(definition.Id, definition.Ruleset, false, out restored) && restored.Score == 35 && File.ReadAllText(legacy + ".bak") == header,
                "Records: first real completion repairs legacy storage and preserves original bytes in backup");
            foreach (var incomplete in new[] {
                "{\"schemaVersion\":1,\"profileId\":\"profile\",\"records\":null}",
                "{\"schemaVersion\":1,\"profileId\":\"profile\",\"otherScores\":[999]}",
                "{\"schemaVersion\":1,\"profileId\":\"other-profile\"}"
            }) {
                string invalid = Path.Combine(directory, "incomplete.json"); File.WriteAllText(invalid, incomplete);
                rejected = false; try { new ComputerGameRecordStore(invalid, "profile", () => true); } catch { rejected = true; }
                check(rejected && File.ReadAllText(invalid) == incomplete, "Records: unknown/incomplete/cross-profile data is never treated as a legacy empty header");
            }
            string blocked = Path.Combine(directory, "blocked.json"); Directory.CreateDirectory(blocked);
            ComputerGames.RecordStore = new ComputerGameRecordStore(blocked, "profile", () => true);
            var beforeFailure = rounds;
            ComputerGames.Publish(Result("other:game", "default-v1", 99));
            check(rounds == beforeFailure + 1 && last.HighScoreSaveFailed && !last.IsNewHighScore && last.HighScore == 0,
                "Records: write failure does not lose round event or report a saved best");
            check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Records: failed writes leave no temporary files");
            ComputerGames.RecordStore = store;
            Native(check);
        }
        finally {
            session.Dispose(); registration.Dispose(); ComputerGames.DeactivateHost(); ComputerGames.RecordStore = null;
            ComputerGames.RoundCompleted -= receive; ComputerGames.HighScoreChanged -= changed; ComputerGames.Error -= error;
        }
    }
    private static ComputerGameResult Result(string id, string rules, long score) => new ComputerGameResult(id, "1.0", rules, score, 1,
        new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 31, 12, 0, 2, DateTimeKind.Utc), 1, 2, false, "profile");
    private static void Native(Action<bool, string> check)
    {
        var tracker = new VanillaRoundTracker(); var utc = DateTime.UtcNow;
        Func<bool, int, double, int, bool, ComputerGameResult> sample = (menu, lives, now, pending, modified) =>
            tracker.Observe(menu, lives, 300, pending, 2, now, now == 3 ? 0 : 0.1, utc, modified, "1.0", "profile");
        check(sample(false, 1, 0, 0, false) == null && sample(false, 0, 1, 0, false) == null, "Vanilla: attaching during play does not claim a partial run");
        sample(true, 0, 1, 0, false); sample(false, 3, 2, 0, false); sample(false, 2, 3, 0, true);
        var result = sample(false, 0, 4, 75, false);
        check(result != null && result.Score == 375 && result.Level == 3 && result.GameId == ComputerGames.VanillaBrickBreakerId,
            "Vanilla: last life captures score including pending bonus points");
        check(result.ModifiedRules && result.ElapsedSeconds == 2 && result.ActiveSeconds < result.ElapsedSeconds,
            "Vanilla: modified rules stick and paused time is excluded from active duration");
        check(sample(false, 0, 5, 0, false) == null && sample(false, 3, 6, 0, false) == null, "Vanilla: terminal state requires a new menu before another result");
        var events = 0; Action<ComputerGameResult> listener = _ => events++; ComputerGames.RoundCompleted += listener;
        ComputerGames.Publish(result); ComputerGames.RoundCompleted -= listener;
        check(events == 1 && ComputerGames.GetHighScore(ComputerGames.VanillaBrickBreakerId, ComputerGames.VanillaBrickBreakerRuleset, true) == 375,
            "Vanilla: same event and record store as mod games");
        sample(true, 0, 7, 0, false); sample(false, 3, 8, 0, false); tracker.Reset();
        check(sample(false, 0, 9, 0, false) == null, "Vanilla: quit/unload/reset cannot complete abandoned run");
        sample(true, 0, 10, 0, false); sample(false, 3, 11, 0, false);
        var replay = sample(false, 0, 12, 0, false);
        check(replay != null && replay.RoundId != result.RoundId && !replay.ModifiedRules, "Vanilla: replay creates a unique result and clears previous rules flag");
        tracker.Reset();
        tracker.Observe(true, 0, 0, 0, 0, 0, 0, utc, false, "1.0", "profile");
        tracker.Observe(false, 3, 0, 0, 0, 1, 0, utc, false, "1.0", "profile");
        var large = tracker.Observe(false, 0, int.MaxValue, 100, 0, 2, 0, utc, false, "1.0", "profile");
        check(large.Score == (long)int.MaxValue + 100, "Vanilla: pending points cannot overflow int");
    }
}
