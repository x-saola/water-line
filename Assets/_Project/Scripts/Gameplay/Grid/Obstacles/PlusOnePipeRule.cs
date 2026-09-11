namespace Delta.ProjectName
{
    public class PlusOnePipeRule : IObstacleEntryRule
    {
        public bool TryEnter(GridManager grid, PipeRuntimeState pipe, GridCell targetCell, (int, int) direction, out bool refundsMove)
        {
            if (grid.TryGetPlusOnePipeClaimant(targetCell, out _))
            {
                // Already claimed (by this pipe or another) - ordinary passable cell now.
                refundsMove = false;
                return true;
            }

            grid.ClaimPlusOnePipe(targetCell, pipe.Id);
            pipe.GrantBonusMove();
            refundsMove = true;
            return true;
        }
    }
}
