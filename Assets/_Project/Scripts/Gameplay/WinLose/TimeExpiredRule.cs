namespace Delta.ProjectName
{
    // No move-based lose condition exists in Waterline - this is the only ILoseRule for MVP,
    // built as a rule (not an inline check) so a future rule (e.g. a stuck-board detector)
    // slots in without restructuring the evaluator.
    public class TimeExpiredRule : ILoseRule
    {
        public bool IsSatisfied(GameplaySessionState state) =>
            state.Level.TimeLimitSeconds > 0 && state.Timer.RemainingSeconds <= 0;
    }
}
