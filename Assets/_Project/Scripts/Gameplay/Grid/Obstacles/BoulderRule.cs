namespace Delta.ProjectName
{
    public class BoulderRule : IObstacleEntryRule
    {
        public bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int dRow, int dCol) direction, out bool refundsMove)
        {
            refundsMove = false;

            var pushTo = new GridCell(targetCell.Row + direction.dRow, targetCell.Column + direction.dCol);
            if (!grid.InBounds(pushTo)) return false;

            var pushToKey = (pushTo.Row, pushTo.Column);
            if (grid.Runtime.IsRock(pushToKey)) return false;
            if (grid.Runtime.IsBoulder(pushToKey)) return false; // no chain-push - blocked

            grid.PushBoulder(targetCell, pushTo);
            return true;
        }
    }
}
