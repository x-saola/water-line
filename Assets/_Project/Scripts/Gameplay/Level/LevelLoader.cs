using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Delta.ProjectName
{
    // Loads/validates a LevelConfig for play. Disk path mirrors LevelEditorWindow's own
    // save format exactly (same JsonSerializerSettings, same Assets/_Project/Levels/{id}.json
    // convention) so the editor/runtime contract round-trips without drift.
    public static class LevelLoader
    {
        public const string LevelsFolder = "Assets/_Project/Levels";

        static JsonSerializerSettings JsonSettings => new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
        };

        // Editor Play Mode only - Assets/ isn't packaged in a Player build (see plan's open
        // questions). Levels need to move to Resources/Addressables before shipping.
        public static bool TryLoadFromDisk(string levelId, out LevelConfig config, out string error)
        {
            config = null;
            string path = Path.Combine(Directory.GetCurrentDirectory(), LevelsFolder, $"{levelId}.json");

            if (!File.Exists(path))
            {
                error = $"No level file found for '{levelId}' at {path}.";
                return false;
            }

            try
            {
                config = JsonConvert.DeserializeObject<LevelConfig>(File.ReadAllText(path), JsonSettings);
            }
            catch (Exception e)
            {
                error = $"Failed to parse '{levelId}': {e.Message}";
                return false;
            }

            return ValidateForPlay(config, out error);
        }

        public static bool ValidateForPlay(LevelConfig config, out string error)
        {
            if (config == null)
            {
                error = "Level config is null.";
                return false;
            }

            var result = LevelValidator.Validate(config);
            if (!result.IsValid)
            {
                error = $"Level '{config.LevelId}' failed validation: {string.Join("; ", result.Errors)}";
                return false;
            }

            error = null;
            return true;
        }

        // Scans Assets/_Project/Levels/level_{n}.json for the smallest n greater than
        // currentLevelId's trailing number, mirroring LevelEditorWindow.NextLevelNumber()'s
        // naming convention. Powers the Win screen's "Next" button - a bare sequential disk
        // lookup, not a level-select/progression system (out of scope, see plan).
        public static bool TryGetNextLevelId(string currentLevelId, out string nextLevelId)
        {
            nextLevelId = null;
            if (!TryParseTrailingNumber(currentLevelId, out int currentNumber))
                return false;

            string folder = Path.Combine(Directory.GetCurrentDirectory(), LevelsFolder);
            if (!Directory.Exists(folder))
                return false;

            int best = int.MaxValue;
            foreach (var file in Directory.GetFiles(folder, "level_*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (TryParseTrailingNumber(id, out int n) && n > currentNumber && n < best)
                    best = n;
            }

            if (best == int.MaxValue) return false;
            nextLevelId = $"level_{best}";
            return true;
        }

        static bool TryParseTrailingNumber(string levelId, out int number)
        {
            number = 0;
            const string prefix = "level_";
            if (string.IsNullOrEmpty(levelId) || !levelId.StartsWith(prefix)) return false;
            return int.TryParse(levelId.Substring(prefix.Length), out number);
        }
    }
}
