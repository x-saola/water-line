using System.Collections.Generic;

namespace Delta.ProjectName
{
    public readonly struct GameplaySessionState
    {
        public LevelConfig Level { get; }
        public GridManager Grid { get; }
        public IReadOnlyList<PipeRuntimeState> Pipes { get; }
        public LevelTimer Timer { get; }

        public GameplaySessionState(LevelConfig level, GridManager grid, LevelTimer timer)
        {
            Level = level;
            Grid = grid;
            Pipes = grid.Pipes;
            Timer = timer;
        }
    }
}
