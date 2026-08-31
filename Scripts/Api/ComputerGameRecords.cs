using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
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

    [DataContract] internal sealed class ComputerGameRecordData
    {
        [DataMember] public string gameId;
        [DataMember] public string ruleset;
        [DataMember] public string gameVersion;
        [DataMember] public string achievedAtUtc;
        [DataMember] public long score;
        [DataMember] public int level;
        [DataMember] public bool modifiedRules;
    }
    [DataContract] internal sealed class ComputerGameRecordDocument
    {
        [DataMember(Order = 0, IsRequired = true)] public int schemaVersion;
        [DataMember(Order = 1, IsRequired = true)] public string profileId;
        [DataMember(Order = 2)] public List<ComputerGameRecordData> records;
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
            var json = File.ReadAllText(_path);
            var loaded = ReadDocument(Encoding.UTF8.GetBytes(json));
            if (loaded == null || loaded.schemaVersion != 1 || loaded.profileId != profile)
                throw new IOException("MCG records file has an unsupported format/profile; original preserved.");
            if (loaded.records == null) {
                // Unity JsonUtility drops lists of custom types from dynamically loaded mod DLLs.
                // Recover only the exact header it previously wrote, never arbitrary incomplete data.
                if (!Regex.IsMatch(json, "\\A\\s*\\{\\s*\"schemaVersion\"\\s*:\\s*1\\s*,\\s*\"profileId\"\\s*:\\s*\"" + Regex.Escape(profile) + "\"\\s*\\}\\s*\\z"))
                    throw new IOException("MCG records list is missing or invalid; original preserved.");
                loaded.records = new List<ComputerGameRecordData>();
                Debug.LogWarning("[MCG] Legacy records header contains no saved scores. Recording is available again; the original will be backed up when a new record is saved.");
            }
            if (loaded.records.Count > 4096) throw new IOException("MCG records file has too many records; original preserved.");
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
            var bytes = WriteDocument(candidate);
            if (bytes.Length > 1024 * 1024) throw new IOException("MCG record size limit reached; original preserved.");
            // Fail before touching disk if any serializer/runtime ever omits record data again.
            var roundtrip = ReadDocument(bytes);
            if (roundtrip.schemaVersion != candidate.schemaVersion || roundtrip.profileId != candidate.profileId ||
                roundtrip.records == null || roundtrip.records.Count != rows.Count)
                throw new IOException("MCG record serialization was incomplete; original preserved.");
            for (int i = 0; i < rows.Count; i++) {
                var a = rows[i]; var b = roundtrip.records[i];
                if (b == null || a.gameId != b.gameId || a.ruleset != b.ruleset || a.gameVersion != b.gameVersion ||
                    a.achievedAtUtc != b.achievedAtUtc || a.score != b.score || a.level != b.level || a.modifiedRules != b.modifiedRules)
                    throw new IOException("MCG record serialization changed data; original preserved.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
                else File.Move(temporary, _path);
                _document = candidate; _records[key] = record;
                return record;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static ComputerGameRecordDocument ReadDocument(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
                return (ComputerGameRecordDocument)new DataContractJsonSerializer(typeof(ComputerGameRecordDocument)).ReadObject(stream);
        }
        private static byte[] WriteDocument(ComputerGameRecordDocument document)
        {
            // Managed reflection handles late-loaded mod types without Unity's build-time type tree.
            using (var stream = new MemoryStream()) {
                new DataContractJsonSerializer(typeof(ComputerGameRecordDocument)).WriteObject(stream, document);
                return stream.ToArray();
            }
        }
    }
}
