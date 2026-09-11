using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Delta.ProjectName
{
    [Serializable]
    public class LevelConfig
    {
        public int SchemaVersion { get; set; } = 1;
        public string LevelId { get; set; } = "";
        public string LevelName { get; set; } = "";

        public int Columns { get; set; }
        public int Rows { get; set; }

        // 0 means untimed - the level editor's Time Freeze booster stays hidden/disabled
        // on levels where this is 0, since there is no countdown to pause.
        public int TimeLimitSeconds { get; set; }

        // Always contains Pipe A and Pipe B; Pipe C is present only when the designer
        // opted into a third anchor for this level.
        public List<PipeConfig> Pipes { get; set; } = new();

        public List<ObstacleConfig> Obstacles { get; set; } = new();

        [JsonIgnore]
        public bool HasThirdAnchor
        {
            get
            {
                for (int i = 0; i < Pipes.Count; i++)
                    if (Pipes[i].Id == PipeId.C)
                        return true;
                return false;
            }
        }

        // Win condition only checks Valve open-state when the level actually authored one -
        // levels with no Valve obstacles win on pipe-meeting alone.
        [JsonIgnore]
        public bool HasValves
        {
            get
            {
                for (int i = 0; i < Obstacles.Count; i++)
                    if (Obstacles[i].Type == ObstacleType.Valve)
                        return true;
                return false;
            }
        }
    }
}
