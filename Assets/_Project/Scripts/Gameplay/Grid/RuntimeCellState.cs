using System.Collections.Generic;

namespace Delta.ProjectName
{
    // Runtime per-cell facts, tracked separately from the authored ObstacleConfig list (which
    // GridManager never mutates) per the spec's explicit requirement: a cell's authored obstacle
    // type is fixed, but "claimed"/"open"/"pushed" evolve during an attempt and must reset
    // cleanly on retry by just rebuilding a fresh RuntimeCellState from the same LevelConfig.
    public class RuntimeCellState
    {
        public readonly HashSet<(int Row, int Column)> RockCells = new();
        public readonly HashSet<(int Row, int Column)> BoulderCells = new();
        public readonly HashSet<(int Row, int Column)> OpenValves = new();
        public readonly Dictionary<(int Row, int Column), PipeId> PlusOnePipeClaims = new();

        public bool IsRock((int, int) cell) => RockCells.Contains(cell);
        public bool IsBoulder((int, int) cell) => BoulderCells.Contains(cell);
        public bool IsValveOpen((int, int) cell) => OpenValves.Contains(cell);
    }
}
