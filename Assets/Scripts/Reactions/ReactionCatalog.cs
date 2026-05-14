using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Reactions
{
    /// <summary>
    /// Loads Reactions.json and provides O(1) lookups by symbol pair.
    /// Key is "{lower}|{upper}" sorted alphabetically (case-insensitive).
    /// </summary>
    public static class ReactionCatalog
    {
        private static Dictionary<string, Reaction> _dict;

        private static void EnsureLoaded()
        {
            if (_dict != null) return;
            _dict = new Dictionary<string, Reaction>(StringComparer.OrdinalIgnoreCase);

            var asset = Resources.Load<TextAsset>("Reactions");
            if (asset == null)
            {
                Debug.LogError("[ReactionCatalog] Reactions.json not found in Resources.");
                return;
            }

            var data = JsonConvert.DeserializeObject<ReactionCatalogData>(asset.text);
            if (data?.reactions == null) return;

            foreach (var r in data.reactions)
                _dict[MakeKey(r.reactantA, r.reactantB)] = r;
        }

        public static bool TryFind(string a, string b, out Reaction reaction)
        {
            EnsureLoaded();
            return _dict.TryGetValue(MakeKey(a, b), out reaction);
        }

        public static List<Reaction> AllReactionsContaining(string symbol)
        {
            EnsureLoaded();
            var result = new List<Reaction>();
            foreach (var r in _dict.Values)
            {
                if (string.Equals(r.reactantA, symbol, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.reactantB, symbol, StringComparison.OrdinalIgnoreCase))
                    result.Add(r);
            }
            return result;
        }

        private static string MakeKey(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) a = "";
            if (string.IsNullOrEmpty(b)) b = "";
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
                ? $"{a}|{b}" : $"{b}|{a}";
        }
    }
}
