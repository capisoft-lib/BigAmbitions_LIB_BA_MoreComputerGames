using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    public sealed class ComputerGameRecord
    {
        public string GameId { get; }
        public string Ruleset { get; }
        public string GameVersion { get; }
        public long Score { get; }
        public int Level { get; }
        public DateTime AchievedAtUtc { get; }
        public bool ModifiedRules { get; }
        internal ComputerGameRecord(ComputerGameRecordData data)
        {
            GameId = data.gameId; Ruleset = data.ruleset; GameVersion = data.gameVersion;
            Score = data.score; Level = data.level; ModifiedRules = data.modifiedRules;
            AchievedAtUtc = DateTime.Parse(data.achievedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
        }
    }

    [Serializable] internal sealed class ComputerGameRecordData
    {
        public string gameId, ruleset, gameVersion, achievedAtUtc;
        public long score;
        public int level;
        public bool modifiedRules;
    }
    [Serializable] internal sealed class ComputerGameRecordDocument
    {
        public int schemaVersion;
        public string profileId;
        public List<ComputerGameRecordData> records;
    }

    // Only the host owns storage. Game IDs are dictionary keys, never filesystem paths.
    internal sealed class ComputerGameRecordStore
    {
        private readonly string _path;
        private readonly Func<bool> _isCurrentProfile;
        private ComputerGameRecordDocument _document;
        private readonly Dictionary<string, ComputerGameRecord> _records = new Dictionary<string, ComputerGameRecord>(StringComparer.Ordinal);
        internal string ProfileId { get; }
        internal bool IsAvailable => _document != null && _isCurrentProfile();
        private static string Key(string id, string rules, bool modified) => id.Length + ":" + id + rules.Length + ":" + rules + (modified ? ":modified" : ":standard");
        internal ComputerGameRecordStore(string path, string profile, Func<bool> isCurrentProfile)
        {
            _path = Path.GetFullPath(path); ProfileId = profile; _isCurrentProfile = isCurrentProfile;
            if (!File.Exists(_path)) {
                _document = new ComputerGameRecordDocument { schemaVersion = 1, profileId = profile, records = new List<ComputerGameRecordData>() };
                return;
            }
            // No fallback to an empty file: preserve unreadable, future or cross-account data.
            if (new FileInfo(_path).Length > 1024 * 1024) throw new IOException("MCG records file is too large; original preserved.");
            var loaded = JsonUtility.FromJson<ComputerGameRecordDocument>(File.ReadAllText(_path));
            if (loaded == null || loaded.schemaVersion != 1 || loaded.profileId != profile || loaded.records == null || loaded.records.Count > 4096)
                throw new IOException("MCG records file has an unsupported format/profile; original preserved.");
            foreach (var data in loaded.records) {
                if (data == null || string.IsNullOrWhiteSpace(data.gameId) || data.gameId.Length > 96 ||
                    string.IsNullOrWhiteSpace(data.ruleset) || data.ruleset.Length > 64 || string.IsNullOrWhiteSpace(data.gameVersion) ||
                    data.gameVersion.Length > 32 || data.score <= 0 || data.level < 0)
                    throw new IOException("MCG record is invalid; original preserved.");
                _records.Add(Key(data.gameId, data.ruleset, data.modifiedRules), new ComputerGameRecord(data));
            }
            _document = loaded;
        }
        internal bool TryGet(string id, string rules, bool modified, out ComputerGameRecord record)
        {
            record = null;
            return IsAvailable && id != null && rules != null && _records.TryGetValue(Key(id, rules, modified), out record);
        }
        internal ComputerGameRecord Record(ComputerGameResult result)
        {
            if (!IsAvailable || result.RecordProfileId != ProfileId)
                throw new IOException("MCG record profile changed or is unavailable; score was not saved.");
            if (TryGet(result.GameId, result.Ruleset, result.ModifiedRules, out var previous) ? result.Score <= previous.Score : result.Score <= 0) return null;
            var key = Key(result.GameId, result.Ruleset, result.ModifiedRules);
            var rows = new List<ComputerGameRecordData>(_document.records);
            rows.RemoveAll(r => Key(r.gameId, r.ruleset, r.modifiedRules) == key);
            if (rows.Count >= 4096) throw new IOException("MCG record limit reached; existing records preserved.");
            var next = new ComputerGameRecordData { gameId = result.GameId, ruleset = result.Ruleset, gameVersion = result.GameVersion,
                score = result.Score, level = result.Level, modifiedRules = result.ModifiedRules, achievedAtUtc = result.EndedAtUtc.ToUniversalTime().ToString("O") };
            rows.Add(next);
            var candidate = new ComputerGameRecordDocument { schemaVersion = 1, profileId = ProfileId, records = rows };
            var record = new ComputerGameRecord(next);
            var json = JsonUtility.ToJson(candidate, true);
            if (System.Text.Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new IOException("MCG record size limit reached; original preserved.");
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
                else File.Move(temporary, _path);
                _document = candidate; _records[key] = record;
                return record;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
