using System;

namespace Delta.Modules.Racing
{
    [Serializable]
    public class OpponentProfile
    {
        public string name;
        public int progress;
        public int targetObjectives;
        public int avatarIndex;
        public int rank; // 0 = racing, 1 = 1st place, 2 = 2nd place, 3 = 3rd place

        public OpponentProfile(string name, int targetObjectives, int avatarIndex)
        {
            this.name = name;
            this.progress = 0;
            this.targetObjectives = targetObjectives;
            this.avatarIndex = avatarIndex;
            this.rank = 0;
        }

        public float GetProgressPercent()
        {
            return targetObjectives > 0 ? (float)progress / targetObjectives : 0f;
        }

        public bool HasWon()
        {
            return progress >= targetObjectives;
        }

        public void IncrementProgress()
        {
            progress++;
        }

        public void ResetProgress()
        {
            progress = 0;
        }
    }
}
