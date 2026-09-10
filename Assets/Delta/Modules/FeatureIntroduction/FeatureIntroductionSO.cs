using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.Modules
{
    [CreateAssetMenu(fileName = "FeatureIntroductionConfig", menuName = "Config/FeatureIntroductionConfig", order = 1)]
    public class FeatureIntroductionSO : ScriptableObject
    {
        public List<FeatureData> Features = new List<FeatureData>();

        public FeatureData GetFeatureUnlockedAtLevel(int level)
        {
            return Features.Find(f => f.unlockLevel == level);
        }

        // Returns the next feature that hasn't been unlocked yet (unlockLevel > level).
        public FeatureData GetNextPendingFeature(int level)
        {
            FeatureData result = null;

            foreach (var feature in Features)
            {
                if (feature.unlockLevel > level)
                {
                    if (result == null || feature.unlockLevel < result.unlockLevel)
                    {
                        result = feature;
                    }
                }
            }

            return result;
        }

        // Returns [0, 1] progress toward the next pending feature from the given level.
        public float GetProgressTowardNextFeature(int level)
        {
            if (Features.Exists(f => f.unlockLevel == level))
            {
                return 1f;
            }

            var next = GetNextPendingFeature(level);

            if (next == null)
            {
                return 1f;
            }

            int prevUnlockLevel = 0;

            foreach (var feature in Features)
            {
                if (feature.unlockLevel <= level && feature.unlockLevel > prevUnlockLevel)
                {
                    prevUnlockLevel = feature.unlockLevel;
                }
            }

            // segmentStart is the first level of the current segment.
            // When there is no previous unlock (prevUnlockLevel == 0), the segment begins at level 1.
            // Otherwise it begins at the previous unlock level (e.g. segment after unlock-at-5 starts at 5).
            int segmentStart = prevUnlockLevel > 0 ? prevUnlockLevel : 1;
            int span = next.unlockLevel - segmentStart;

            if (span <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)(level - segmentStart) / span);
        }
    }

    [Serializable]
    public class FeatureData
    {
        public string displayName;
        public string description;
        public Sprite sprite;
        public int unlockLevel;
    }
}
