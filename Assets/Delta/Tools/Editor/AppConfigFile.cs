using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Delta.Tools.Editor
{
    // Reads/writes the INI-style app_configs.bytes in place, preserving comments,
    // ordering and line endings so edits show up as minimal diffs.
    public class AppConfigFile
    {
        private readonly string _assetPath;
        private readonly List<string> _lines = new List<string>();
        private string _lineEnding = "\n";
        private bool _trailingNewline;

        public AppConfigFile(string assetPath)
        {
            _assetPath = assetPath;
            Load();
        }

        public void Load()
        {
            string raw = File.ReadAllText(GetFullPath());
            _lineEnding = raw.Contains("\r\n") ? "\r\n" : "\n";
            _trailingNewline = raw.EndsWith("\n");

            string normalized = raw.Replace("\r\n", "\n");
            if (_trailingNewline)
                normalized = normalized.Substring(0, normalized.Length - 1);

            _lines.Clear();
            _lines.AddRange(normalized.Split('\n'));
        }

        public string GetValue(string section, string key)
        {
            int sectionStart = FindSectionIndex(section);
            if (sectionStart < 0)
                return null;

            int sectionEnd = FindSectionEnd(sectionStart);
            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                if (TryParseKey(_lines[i], out string lineKey, out int equalsIdx) && lineKey == key)
                    return _lines[i].Substring(equalsIdx + 1).Trim();
            }

            return null;
        }

        public void SetValue(string section, string key, string value)
        {
            int sectionStart = FindSectionIndex(section);
            if (sectionStart < 0)
            {
                if (_lines.Count > 0 && _lines[_lines.Count - 1].Trim().Length > 0)
                    _lines.Add(string.Empty);
                _lines.Add($"[{section}]");
                _lines.Add($"{key} = {value}");
                return;
            }

            int sectionEnd = FindSectionEnd(sectionStart);
            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                if (TryParseKey(_lines[i], out string lineKey, out int equalsIdx) && lineKey == key)
                {
                    _lines[i] = $"{_lines[i].Substring(0, equalsIdx + 1)} {value}";
                    return;
                }
            }

            _lines.Insert(sectionEnd, $"{key} = {value}");
        }

        public void Save()
        {
            string content = string.Join(_lineEnding, _lines);
            if (_trailingNewline)
                content += _lineEnding;

            File.WriteAllText(GetFullPath(), content);
            AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
        }

        private string GetFullPath()
        {
            return Path.GetFullPath(_assetPath);
        }

        private int FindSectionIndex(string section)
        {
            string header = $"[{section}]";
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Trim() == header)
                    return i;
            }

            return -1;
        }

        private int FindSectionEnd(int sectionStart)
        {
            for (int i = sectionStart + 1; i < _lines.Count; i++)
            {
                if (_lines[i].TrimStart().StartsWith("["))
                    return i;
            }

            return _lines.Count;
        }

        private bool TryParseKey(string line, out string key, out int equalsIdx)
        {
            key = null;
            equalsIdx = line.IndexOf('=');
            if (equalsIdx < 0)
                return false;

            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == ';')
                return false;

            key = line.Substring(0, equalsIdx).Trim();
            return true;
        }
    }
}
