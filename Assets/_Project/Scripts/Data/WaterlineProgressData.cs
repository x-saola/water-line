using System.Collections.Generic;

namespace Delta.ProjectName
{
    [System.Serializable]
    public class WaterlineLevelResult
    {
        public string LevelId;
        public bool IsComplete;
        public int Attempts;
        public float BestTimeSeconds; // 0 = no completion recorded yet
    }

    // LevelId-keyed completion/best-time tracking, added as a new field on UserData rather than
    // a retrofit of the legacy ClassicLevelModeData, which is int-level-indexed, merge-game
    // shaped (RandomLevel/Stars/TutorialDone), and has no time field at all.
    [System.Serializable]
    public class WaterlineProgressData
    {
        public List<WaterlineLevelResult> Results = new();

        WaterlineLevelResult GetOrCreate(string levelId)
        {
            foreach (var r in Results)
                if (r.LevelId == levelId) return r;

            var created = new WaterlineLevelResult { LevelId = levelId };
            Results.Add(created);
            return created;
        }

        public int IncrementAttempts(string levelId)
        {
            var r = GetOrCreate(levelId);
            r.Attempts++;
            return r.Attempts;
        }

        // Best time never regresses - only overwritten by a strictly faster completion.
        public void RecordCompletion(string levelId, float timeSeconds)
        {
            var r = GetOrCreate(levelId);
            r.IsComplete = true;
            if (r.BestTimeSeconds <= 0f || timeSeconds < r.BestTimeSeconds)
                r.BestTimeSeconds = timeSeconds;
        }

        public bool TryGetBestTime(string levelId, out float bestTimeSeconds)
        {
            foreach (var r in Results)
            {
                if (r.LevelId != levelId) continue;
                bestTimeSeconds = r.BestTimeSeconds;
                return r.BestTimeSeconds > 0f;
            }
            bestTimeSeconds = 0f;
            return false;
        }
    }
}
