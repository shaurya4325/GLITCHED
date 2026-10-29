using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A simple localisation table: Resources/Localization/strings.csv with columns key,en,hi,...
    /// L.T("menu.play") returns the text in the current language (English is the fallback).
    /// Add a column to the CSV to add a language.
    /// </summary>
    public class Localization : MonoBehaviour
    {
        readonly Dictionary<string, Dictionary<string, string>> table = new Dictionary<string, Dictionary<string, string>>();
        public string Language { get; private set; } = "en";
        public List<string> Languages { get; } = new List<string> { "en" };

        public void Init(string lang)
        {
            var csv = Resources.Load<TextAsset>("Localization/strings");
            if (csv != null) Parse(csv.text);
            SetLanguage(lang);
        }

        public void SetLanguage(string lang) => Language = string.IsNullOrEmpty(lang) ? "en" : lang;

        void Parse(string text)
        {
            using (var r = new StringReader(text))
            {
                string header = r.ReadLine();
                if (header == null) return;
                var cols = SplitCsv(header);
                Languages.Clear();
                for (int i = 1; i < cols.Count; i++) Languages.Add(cols[i].Trim());
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = SplitCsv(line);
                    if (parts.Count < 2) continue;
                    var row = new Dictionary<string, string>();
                    for (int i = 1; i < parts.Count && i < cols.Count; i++)
                        if (!string.IsNullOrEmpty(parts[i])) row[cols[i].Trim()] = parts[i].Replace("\\n", "\n");
                    table[parts[0].Trim()] = row;
                }
            }
        }

        static List<string> SplitCsv(string line)
        {
            var result = new List<string>();
            var cur = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else quoted = !quoted; }
                else if (c == ',' && !quoted) { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString());
            return result;
        }

        public string Get(string key, string fallback = null)
        {
            if (table.TryGetValue(key, out var row))
            {
                if (row.TryGetValue(Language, out var v)) return v;
                if (row.TryGetValue("en", out var en)) return en;
            }
            return fallback ?? key;
        }
    }

    /// <summary>Short alias: L.T("key").</summary>
    public static class L
    {
        public static string T(string key, string fallback = null) => Services.Loc != null ? Services.Loc.Get(key, fallback) : (fallback ?? key);
    }
}
