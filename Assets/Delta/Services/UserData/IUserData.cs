using System.Collections.Generic;

namespace Delta.Services
{
    public interface IUserData
    {
        public long LastUpdate { get; set; }
        public List<ItemEntry> Currencies { get; set; }
        public List<ItemEntry> Items { get; set; }
        int CurrentLevel { get; }

        void Initialize();
    }

    [System.Serializable]
    public class ItemEntry
    {
        public string Id;
        public int Quantity;
    }
    
    [System.Serializable]
    public class PlayedLevelData
    {
        public int Level;
        public bool IsComplete;
        public int Attempts;
        public int Stars;
        public int RandomLevel;
        public bool TutorialDone;
    }

    [System.Serializable]
    public class LevelModeData
    {
        public int CurrentLevel;
        public List<PlayedLevelData> PlayedLevels;

        public LevelModeData()
        {
            CurrentLevel = 1;
            PlayedLevels = new();
        }

        public void Initialize() { }

        private PlayedLevelData GetOrCreateLevelData(int level)
        {
            PlayedLevels ??= new();
            int index = level - 1;
            while (PlayedLevels.Count <= index)
                PlayedLevels.Add(new PlayedLevelData { Level = PlayedLevels.Count + 1 });
            return PlayedLevels[index];
        }

        public int GetRandomLevel(int actualLevel)
        {
            int index = actualLevel - 1;
            if (PlayedLevels != null && index >= 0 && index < PlayedLevels.Count)
            {
                int random = PlayedLevels[index].RandomLevel;
                return random > 0 ? random : actualLevel;
            }
            return actualLevel;
        }

        public void SetRandomLevel(int actualLevel, int randomLevel)
        {
            GetOrCreateLevelData(actualLevel).RandomLevel = randomLevel;
        }

        public int GetLevelStars(int level)
        {
            int index = level - 1;
            return PlayedLevels != null && index >= 0 && index < PlayedLevels.Count ? PlayedLevels[index].Stars : 0;
        }

        public void SetLevelStars(int level, int stars)
        {
            var data = GetOrCreateLevelData(level);
            if (stars > data.Stars) data.Stars = stars;
        }

        public int GetLevelAttempts(int level)
        {
            int index = level - 1;
            return PlayedLevels != null && index >= 0 && index < PlayedLevels.Count ? PlayedLevels[index].Attempts : 0;
        }

        public int IncrementLevelAttempts(int level)
        {
            var data = GetOrCreateLevelData(level);
            data.Attempts++;
            return data.Attempts;
        }

        public bool IsLevelComplete(int level)
        {
            int index = level - 1;
            return PlayedLevels != null && index >= 0 && index < PlayedLevels.Count && PlayedLevels[index].IsComplete;
        }

        public void SetLevelComplete(int level)
        {
            GetOrCreateLevelData(level).IsComplete = true;
        }

        public bool IsTutorialDone(int level)
        {
            int index = level - 1;
            return PlayedLevels != null && index >= 0 && index < PlayedLevels.Count && PlayedLevels[index].TutorialDone;
        }

        public void MarkTutorialDone(int level)
        {
            GetOrCreateLevelData(level).TutorialDone = true;
        }
    }
}
