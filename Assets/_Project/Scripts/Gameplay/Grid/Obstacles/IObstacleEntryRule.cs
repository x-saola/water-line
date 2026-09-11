namespace Delta.ProjectName
{
    // One rule per obstacle behavior, dispatched by ObstacleRuleResolver. Kept separate from
    // MoveResolver's cell-legality checks (adjacency/budget/own-path/other-pipe), which apply
    // uniformly regardless of what's on the target cell.
    public interface IObstacleEntryRule
    {
        // direction is (targetCell - pipe.HeadCell), needed only by BoulderRule to compute the
        // push target. refundsMove is true only for a first-time +1 Pipe claim.
        bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int dRow, int dCol) direction,
            out bool refundsMove);
    }
}
