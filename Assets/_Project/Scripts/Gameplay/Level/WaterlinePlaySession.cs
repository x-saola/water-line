namespace Delta.ProjectName
{
    // In-memory handoff for launching Play with a specific in-editor level layout, bypassing
    // disk load entirely - the seam a future Level Editor "Play this level" button targets
    // (not implemented yet - see plan), and what this project's own verification/manual test
    // levels use today. GamePlayController checks this first, falling back to
    // LevelLoader.TryLoadFromDisk(levelId) when nothing is pending.
    public static class WaterlinePlaySession
    {
        public static LevelConfig PendingLevelConfig { get; private set; }

        public static void PlayLevel(LevelConfig config) => PendingLevelConfig = config;

        public static LevelConfig ConsumePendingLevelConfig()
        {
            var config = PendingLevelConfig;
            PendingLevelConfig = null;
            return config;
        }
    }
}
