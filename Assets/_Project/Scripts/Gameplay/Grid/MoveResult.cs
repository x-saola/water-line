namespace Delta.ProjectName
{
    public enum MoveOutcome
    {
        Ignored,            // not a legal drag target (not orthogonally adjacent) - no feedback needed
        Extended,
        Retracted,
        BlockedBudget,
        BlockedOwnPath,
        BlockedOtherPipe,
        BlockedRock,
        BlockedBoulderPush,
    }

    public readonly struct MoveResult
    {
        public MoveOutcome Outcome { get; }
        public bool Success => Outcome == MoveOutcome.Extended || Outcome == MoveOutcome.Retracted;

        public MoveResult(MoveOutcome outcome) => Outcome = outcome;

        public static readonly MoveResult Ignored = new(MoveOutcome.Ignored);
        public static readonly MoveResult Extended = new(MoveOutcome.Extended);
        public static readonly MoveResult Retracted = new(MoveOutcome.Retracted);
        public static readonly MoveResult BlockedBudget = new(MoveOutcome.BlockedBudget);
        public static readonly MoveResult BlockedOwnPath = new(MoveOutcome.BlockedOwnPath);
        public static readonly MoveResult BlockedOtherPipe = new(MoveOutcome.BlockedOtherPipe);
        public static readonly MoveResult BlockedRock = new(MoveOutcome.BlockedRock);
        public static readonly MoveResult BlockedBoulderPush = new(MoveOutcome.BlockedBoulderPush);
    }
}
