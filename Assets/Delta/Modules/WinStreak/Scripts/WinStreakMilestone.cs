using System;
using System.Collections;
using System.Collections.Generic;
using Delta.Modules.Reward;
using UnityEngine;

namespace Delta.Modules.WinStreak
{
    [Serializable]
    public class WinStreakMilestone
    {
        public int startValue;
        public int interval;
        public RewardData[] rewards;
    }
}
