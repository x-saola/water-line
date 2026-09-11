namespace Delta.ProjectName
{
    public class RockRule : IObstacleEntryRule
    {
        public bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int, int) direction, out bool refundsMove)
        {
            refundsMove = false;
            return false; // permanently impassable, static for the whole attempt
        }
    }
}
