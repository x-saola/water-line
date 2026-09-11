namespace Delta.ProjectName
{
    // Starts on the player's first accepted move, not on level load (spec: core loop step 3).
    // Always counts up regardless of IsTimed, so untimed levels still record a completion time
    // for comparison ("Piped in mm:ss") even though they have no fail threshold.
    public class LevelTimer
    {
        public bool IsRunning { get; private set; }
        public bool IsTimed { get; }
        public float Elapsed { get; private set; }

        readonly float _limitSeconds;

        public LevelTimer(int timeLimitSeconds)
        {
            IsTimed = timeLimitSeconds > 0;
            _limitSeconds = timeLimitSeconds;
        }

        public float RemainingSeconds => IsTimed ? _limitSeconds - Elapsed : float.PositiveInfinity;

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Elapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            Elapsed += deltaTime;
        }

        public void Stop() => IsRunning = false;

        public string FormatCompletionTime()
        {
            int totalSeconds = (int)Elapsed;
            return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
        }
    }
}
