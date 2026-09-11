using System.Collections.Generic;
using System.Linq;

namespace Delta.ProjectName
{
    public class LevelValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsValid => Errors.Count == 0;
    }

    // Pure C# (no UnityEditor dependency) so both the Level Editor (live) and the future runtime
    // loader share one source of truth for what makes a level valid.
    public static class LevelValidator
    {
        public const int MinColumns = 3;
        public const int MaxColumns = 12;
        public const int MinRows = 3;
        public const int MaxRows = 16;

        public static LevelValidationResult Validate(LevelConfig level)
        {
            var result = new LevelValidationResult();

            if (level == null)
            {
                result.Errors.Add("Level is null.");
                return result;
            }

            if (level.Columns < MinColumns || level.Columns > MaxColumns)
                result.Errors.Add($"Columns {level.Columns} is outside [{MinColumns}, {MaxColumns}].");

            if (level.Rows < MinRows || level.Rows > MaxRows)
                result.Errors.Add($"Rows {level.Rows} is outside [{MinRows}, {MaxRows}].");

            if (level.TimeLimitSeconds < 0)
                result.Errors.Add("Time limit cannot be negative.");

            ValidatePipes(level, result);
            ValidateObstacles(level, result);

            return result;
        }

        private static void ValidatePipes(LevelConfig level, LevelValidationResult result)
        {
            if (level.Pipes == null || level.Pipes.Count == 0)
            {
                result.Errors.Add("Level has no pipes.");
                return;
            }

            bool hasA = false, hasB = false, hasC = false;
            var seenCells = new HashSet<(int, int)>();

            foreach (var pipe in level.Pipes)
            {
                switch (pipe.Id)
                {
                    case PipeId.A: hasA = true; break;
                    case PipeId.B: hasB = true; break;
                    case PipeId.C:
                        if (hasC) result.Errors.Add("Level has more than one Pipe C.");
                        hasC = true;
                        break;
                }

                if (pipe.MoveBudget <= 0)
                    result.Errors.Add($"Pipe {pipe.Id} has a move budget of {pipe.MoveBudget}; it must be greater than 0.");

                if (!IsInsideGrid(level, pipe.StartCell))
                {
                    result.Errors.Add($"Pipe {pipe.Id}'s start cell {pipe.StartCell} is outside the {level.Columns}x{level.Rows} grid.");
                    continue;
                }

                if (!seenCells.Add((pipe.StartCell.Row, pipe.StartCell.Column)))
                    result.Errors.Add($"Pipe {pipe.Id} shares its start cell with another pipe.");
            }

            if (!hasA)
                result.Errors.Add("Level is missing Pipe A.");
            if (!hasB)
                result.Errors.Add("Level is missing Pipe B.");
        }

        private static void ValidateObstacles(LevelConfig level, LevelValidationResult result)
        {
            if (level.Obstacles == null) return;

            var anchorCells = new HashSet<(int, int)>(
                (level.Pipes ?? Enumerable.Empty<PipeConfig>())
                    .Select(p => (p.StartCell.Row, p.StartCell.Column)));

            var seenCells = new HashSet<(int, int)>();
            foreach (var obstacle in level.Obstacles)
            {
                if (!IsInsideGrid(level, obstacle.Cell))
                {
                    result.Errors.Add($"A {obstacle.Type} obstacle at {obstacle.Cell} is outside the {level.Columns}x{level.Rows} grid.");
                    continue;
                }

                var key = (obstacle.Cell.Row, obstacle.Cell.Column);
                if (!seenCells.Add(key))
                    result.Errors.Add($"Two obstacles occupy the same cell {obstacle.Cell}.");

                if (anchorCells.Contains(key))
                    result.Errors.Add($"A {obstacle.Type} obstacle at {obstacle.Cell} overlaps a pipe's start cell.");
            }
        }

        private static bool IsInsideGrid(LevelConfig level, GridCell cell)
            => cell.Row >= 0 && cell.Row < level.Rows && cell.Column >= 0 && cell.Column < level.Columns;
    }
}
