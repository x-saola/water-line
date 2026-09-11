using System.Collections.Generic;

namespace Delta.ProjectName
{
    // One pipe's live path/budget for the current attempt. Tracks per-step whether a step cost
    // a move (a parallel list, not just a single counter) because a +1 Pipe claim refunds
    // exactly one step - retraction must mirror that specific step's cost, not always -1, or
    // MovesUsed drifts out of sync with the pipe's actual path length.
    public class PipeRuntimeState
    {
        public PipeId Id { get; }
        public List<GridCell> Path { get; } = new();
        public int MovesUsed { get; private set; }
        public int MoveBudget { get; private set; }

        readonly List<bool> _stepCostMove = new();

        public GridCell HeadCell => Path[^1];
        public GridCell StartCell => Path[0];
        public int MovesRemaining => MoveBudget - MovesUsed;

        public PipeRuntimeState(PipeConfig config)
        {
            Id = config.Id;
            MoveBudget = config.MoveBudget;
            Path.Add(config.StartCell);
        }

        public void Extend(GridCell cell, bool costsMove)
        {
            Path.Add(cell);
            _stepCostMove.Add(costsMove);
            if (costsMove) MovesUsed++;
        }

        public GridCell Retract()
        {
            var removed = Path[^1];
            bool costedMove = _stepCostMove[^1];

            Path.RemoveAt(Path.Count - 1);
            _stepCostMove.RemoveAt(_stepCostMove.Count - 1);
            if (costedMove) MovesUsed--;

            return removed;
        }

        public void GrantBonusMove() => MoveBudget++;
        public void RevokeBonusMove() => MoveBudget--;

        public bool ContainsCell(GridCell cell)
        {
            foreach (var c in Path)
                if (c.Row == cell.Row && c.Column == cell.Column) return true;
            return false;
        }
    }
}
