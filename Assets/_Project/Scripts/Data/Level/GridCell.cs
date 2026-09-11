using System;

namespace Delta.ProjectName
{
    [Serializable]
    public struct GridCell
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public GridCell(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public override string ToString() => $"({Row}, {Column})";
    }
}
