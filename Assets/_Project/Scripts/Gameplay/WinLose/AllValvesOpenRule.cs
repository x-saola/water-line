namespace Delta.ProjectName
{
    // Vacuously true for levels with no Valve obstacles - they win on pipe-meeting alone.
    public class AllValvesOpenRule : IWinRule
    {
        public bool IsSatisfied(GameplaySessionState state)
        {
            foreach (var obstacle in state.Level.Obstacles)
            {
                if (obstacle.Type != ObstacleType.Valve) continue;
                if (!state.Grid.Runtime.IsValveOpen((obstacle.Cell.Row, obstacle.Cell.Column)))
                    return false;
            }
            return true;
        }
    }
}
