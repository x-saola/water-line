using System;

namespace Delta.ProjectName
{
    // Pure legality + effect resolution for one drag step (extend or retract). No MonoBehaviour/
    // UnityEngine coupling - PipeInputController is the only caller, and every branch here is
    // independently unit-testable.
    public static class MoveResolver
    {
        public static MoveResult TryStep(GridManager grid, PipeRuntimeState pipe, GridCell targetCell)
        {
            var head = pipe.HeadCell;
            int dRow = targetCell.Row - head.Row;
            int dCol = targetCell.Column - head.Column;

            bool orthogonalAdjacent = Math.Abs(dRow) + Math.Abs(dCol) == 1;
            if (!orthogonalAdjacent)
                return MoveResult.Ignored;

            // Retraction: dragging back over the pipe's own immediately-previous cell. Never
            // budget-gated - retracting always frees a move, it doesn't spend one.
            if (pipe.Path.Count >= 2)
            {
                var previous = pipe.Path[^2];
                if (previous.Row == targetCell.Row && previous.Column == targetCell.Column)
                    return Retract(grid, pipe);
            }

            if (!grid.InBounds(targetCell))
                return MoveResult.Ignored;

            if (pipe.MovesRemaining <= 0)
                return MoveResult.BlockedBudget;

            // Distinct from retraction - re-entering ANY earlier cell in the pipe's own path
            // (not just the immediately-previous one) is always blocked.
            if (pipe.ContainsCell(targetCell))
                return MoveResult.BlockedOwnPath;

            // A buried (non-head) cell of another live pipe's path is off-limits; another
            // pipe's CURRENT head is allowed to be entered - that's the meeting mechanic.
            if (grid.AnyOtherPipeOccupiesNonHead(targetCell, excluding: pipe.Id))
                return MoveResult.BlockedOtherPipe;

            var rule = ObstacleRuleResolver.Resolve(grid, targetCell);
            if (!rule.TryEnter(grid, pipe, targetCell, (dRow, dCol), out bool refundsMove))
                return rule is BoulderRule ? MoveResult.BlockedBoulderPush : MoveResult.BlockedRock;

            pipe.Extend(targetCell, costsMove: !refundsMove);
            return MoveResult.Extended;
        }

        static MoveResult Retract(GridManager grid, PipeRuntimeState pipe)
        {
            var releasedCell = pipe.Retract();

            // Only the pipe that claimed a +1 Pipe cell releases it by retracting back through
            // it - a different pipe merely passing through later never un-claims someone else's
            // bonus. Boulder position and Valve open-state are intentionally NEVER touched here;
            // each obstacle type has its own explicit (non-)retraction behavior per the spec.
            if (grid.TryGetPlusOnePipeClaimant(releasedCell, out var claimant) && claimant == pipe.Id)
            {
                grid.ReleasePlusOnePipe(releasedCell);
                pipe.RevokeBonusMove();
            }

            return MoveResult.Retracted;
        }
    }
}
