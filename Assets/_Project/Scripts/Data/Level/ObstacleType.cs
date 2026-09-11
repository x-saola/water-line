namespace Delta.ProjectName
{
    public enum ObstacleType
    {
        Rock,

        // Passable, unlike Rock - entering the cell pushes it one cell further in the
        // pipe's travel direction if that cell is free, otherwise the step is blocked.
        // Stays at its pushed position even if the push move is later retracted.
        Boulder,

        // Passable; if the level has any Valve, every Valve must be routed over (open)
        // for the level to be won, in addition to all pipes meeting.
        Valve,

        // Shares the obstacle palette in the level editor but never blocks movement -
        // the first pipe to route through it permanently claims a +1 move bonus.
        PlusOnePipe,
    }
}
