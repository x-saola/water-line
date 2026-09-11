using System;
using System.Collections.Generic;
using System.Linq;

namespace Delta.ProjectName
{
    // Owns board layout + runtime per-cell/per-pipe state for one attempt. The authored
    // LevelConfig/ObstacleConfig list is never mutated - RestartLevel just builds a fresh
    // GridManager from the same LevelConfig instance.
    public class GridManager
    {
        public LevelConfig Level { get; }
        public RuntimeCellState Runtime { get; } = new();
        public IReadOnlyList<PipeRuntimeState> Pipes => _pipes;

        readonly List<PipeRuntimeState> _pipes = new();
        readonly Dictionary<(int, int), ObstacleType> _authoredObstacles = new();

        public event Action<GridCell> OnValveOpened;
        public event Action<GridCell, GridCell> OnBoulderPushed; // (from, to)
        public event Action<GridCell, PipeId> OnPlusOnePipeClaimed;
        public event Action<GridCell> OnPlusOnePipeReleased;

        public GridManager(LevelConfig level)
        {
            Level = level;

            foreach (var obstacle in level.Obstacles)
            {
                var key = (obstacle.Cell.Row, obstacle.Cell.Column);
                _authoredObstacles[key] = obstacle.Type;

                if (obstacle.Type == ObstacleType.Rock) Runtime.RockCells.Add(key);
                else if (obstacle.Type == ObstacleType.Boulder) Runtime.BoulderCells.Add(key);
            }

            foreach (var pipeConfig in level.Pipes)
                _pipes.Add(new PipeRuntimeState(pipeConfig));
        }

        public bool InBounds(GridCell cell) =>
            cell.Row >= 0 && cell.Row < Level.Rows && cell.Column >= 0 && cell.Column < Level.Columns;

        public ObstacleType? AuthoredObstacleAt(GridCell cell) =>
            _authoredObstacles.TryGetValue((cell.Row, cell.Column), out var type) ? type : null;

        public PipeRuntimeState GetPipe(PipeId id) => _pipes.First(p => p.Id == id);

        public bool AnyOtherPipeOccupiesNonHead(GridCell cell, PipeId excluding)
        {
            foreach (var pipe in _pipes)
            {
                if (pipe.Id == excluding) continue;
                // All but the head - another pipe's current head is a legal entry (the meeting
                // mechanic), only a BURIED cell of another live pipe's path is off-limits.
                for (int i = 0; i < pipe.Path.Count - 1; i++)
                {
                    var c = pipe.Path[i];
                    if (c.Row == cell.Row && c.Column == cell.Column) return true;
                }
            }
            return false;
        }

        public void PushBoulder(GridCell from, GridCell to)
        {
            Runtime.BoulderCells.Remove((from.Row, from.Column));
            Runtime.BoulderCells.Add((to.Row, to.Column));
            OnBoulderPushed?.Invoke(from, to);
        }

        public void OpenValve(GridCell cell)
        {
            if (Runtime.OpenValves.Add((cell.Row, cell.Column)))
                OnValveOpened?.Invoke(cell);
        }

        public void ClaimPlusOnePipe(GridCell cell, PipeId claimant)
        {
            Runtime.PlusOnePipeClaims[(cell.Row, cell.Column)] = claimant;
            OnPlusOnePipeClaimed?.Invoke(cell, claimant);
        }

        public void ReleasePlusOnePipe(GridCell cell)
        {
            if (Runtime.PlusOnePipeClaims.Remove((cell.Row, cell.Column)))
                OnPlusOnePipeReleased?.Invoke(cell);
        }

        public bool TryGetPlusOnePipeClaimant(GridCell cell, out PipeId claimant) =>
            Runtime.PlusOnePipeClaims.TryGetValue((cell.Row, cell.Column), out claimant);
    }
}
