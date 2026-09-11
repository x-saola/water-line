using UnityEngine;

namespace Delta.ProjectName
{
    // Shared cell -> local-position math so GridView's board cells and PipeView's pipe segments
    // align pixel-for-pixel without either depending on the other's internal state. Row 0 is
    // the top row, matching the Level Editor's own board rendering convention.
    public static class BoardLayout
    {
        public static Vector2 CellToLocalPosition(GridCell cell, int columns, int rows, float cellStep)
        {
            float boardWidth = columns * cellStep;
            float boardHeight = rows * cellStep;
            float originX = -boardWidth / 2f + cellStep / 2f;
            float originY = boardHeight / 2f - cellStep / 2f;
            return new Vector2(originX + cell.Column * cellStep, originY - cell.Row * cellStep);
        }
    }
}
