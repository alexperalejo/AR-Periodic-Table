using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.History
{
    [Serializable]
    public class ScanRecord
    {
        public string   objectLabel;
        public string[] elementSymbols;
        public long     unixTimestamp;
    }

    public static class ScanHistory
    {
        private const string PrefsKey   = "scan_history_v1";
        private const int    MaxRecords = 200;

        private static List<ScanRecord> _records;

        public static event Action<ScanRecord> OnScanAdded;

        private static void EnsureLoaded()
        {
            if (_records != null) return;
            var raw = PlayerPrefs.GetString(PrefsKey, "[]");
            try
            {
                _records = JsonConvert.DeserializeObject<List<ScanRecord>>(raw) ?? new List<ScanRecord>();
            }
            catch
            {
                _records = new List<ScanRecord>();
            }
        }

        public static void Add(string label, IEnumerable<string> symbols)
        {
            EnsureLoaded();
            var record = new ScanRecord
            {
                objectLabel     = label,
                elementSymbols  = symbols?.ToArray() ?? Array.Empty<string>(),
                unixTimestamp   = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            _records.Add(record);

            while (_records.Count > MaxRecords)
                _records.RemoveAt(0);

            PlayerPrefs.SetString(PrefsKey, JsonConvert.SerializeObject(_records));
            PlayerPrefs.Save();

            OnScanAdded?.Invoke(record);
        }

        public static IReadOnlyList<ScanRecord> All()
        {
            EnsureLoaded();
            return _records.AsReadOnly();
        }

        public static HashSet<string> AllSeenElementSymbols()
        {
            EnsureLoaded();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in _records)
                if (r.elementSymbols != null)
                    foreach (var s in r.elementSymbols) seen.Add(s);
            return seen;
        }

        public static List<string> AllSeenObjects()
        {
            EnsureLoaded();
            var seen = new List<string>();
            var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                var lbl = _records[i].objectLabel;
                if (!string.IsNullOrEmpty(lbl) && dedup.Add(lbl))
                    seen.Add(lbl);
            }
            return seen;
        }

        public static void Clear()
        {
            _records = new List<ScanRecord>();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}
