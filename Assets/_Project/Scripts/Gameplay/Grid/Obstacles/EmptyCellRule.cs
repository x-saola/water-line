namespace Delta.ProjectName
{
    public class EmptyCellRule : IObstacleEntryRule
    {
        public bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int, int) direction, out bool refundsMove)
        {
            refundsMove = false;
            return true;
        }
    }
}
