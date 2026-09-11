namespace Delta.ProjectName
{
    public class ValveRule : IObstacleEntryRule
    {
        public bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int, int) direction, out bool refundsMove)
        {
            refundsMove = false;
            grid.OpenValve(targetCell); // idempotent - starts closed, flips open on first entry, never re-closes
            return true;
        }
    }
}
