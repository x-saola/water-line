namespace Delta.ProjectName
{
    // Works uniformly for 2 or 3 pipes: every pipe's current head must be the same cell. No
    // pre-designated meeting cell - see plan's "meeting-cell interpretation" note.
    public class AllPipesMeetRule : IWinRule
    {
        public bool IsSatisfied(GameplaySessionState state)
        {
            var pipes = state.Pipes;
            if (pipes.Count == 0) return false;

            var first = pipes[0].HeadCell;
            for (int i = 1; i < pipes.Count; i++)
            {
                var head = pipes[i].HeadCell;
                if (head.Row != first.Row || head.Column != first.Column)
                    return false;
            }
            return true;
        }
    }
}
