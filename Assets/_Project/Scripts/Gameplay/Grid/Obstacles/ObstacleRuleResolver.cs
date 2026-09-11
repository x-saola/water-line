using System.Collections.Generic;

namespace Delta.ProjectName
{
    public static class ObstacleRuleResolver
    {
        static readonly IObstacleEntryRule Empty = new EmptyCellRule();
        static readonly Dictionary<ObstacleType, IObstacleEntryRule> Rules = new()
        {
            { ObstacleType.Rock, new RockRule() },
            { ObstacleType.Boulder, new BoulderRule() },
            { ObstacleType.Valve, new ValveRule() },
            { ObstacleType.PlusOnePipe, new PlusOnePipeRule() },
        };

        // Resolved by RUNTIME state first, not authored type: a Boulder's position moves, so a
        // cell it has been pushed onto must route through BoulderRule (chain-push blocking
        // applies there) even if it was authored as something else (or nothing); conversely, a
        // cell authored as Boulder whose boulder has since been pushed away is now empty ground,
        // not still "Boulder". Every other obstacle type is static, so authored type is authoritative.
        public static IObstacleEntryRule Resolve(GridManager grid, GridCell cell)
        {
            var key = (cell.Row, cell.Column);

            if (grid.Runtime.IsBoulder(key))
                return Rules[ObstacleType.Boulder];

            var authoredType = grid.AuthoredObstacleAt(cell);
            if (authoredType == ObstacleType.Boulder)
                return Empty; // authored Boulder cell, but it has since moved away

            if (authoredType.HasValue && Rules.TryGetValue(authoredType.Value, out var rule))
                return rule;

            return Empty;
        }
    }
}
