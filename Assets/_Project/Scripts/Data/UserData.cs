using System.Collections.Generic;
using Delta.Services;

namespace Delta.ProjectName
{
    [System.Serializable]
    public class UserData : IUserData
    {
        public long LastUpdate { get; set; }
        public List<ItemEntry> Currencies { get; set; } = new();
        public List<ItemEntry> Items { get; set; } = new();

        public bool HasNoAds;

        public LevelModeData ClassicLevelModeData = new();
        public WaterlineProgressData WaterlineProgress = new();

        public int CurrentLevel => ClassicLevelModeData.CurrentLevel;

        public void Initialize()
        {
            ClassicLevelModeData.Initialize();
        }
    }
}
