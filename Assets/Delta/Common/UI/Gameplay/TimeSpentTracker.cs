namespace Delta.Common
{
    public class TimeSpentTracker
    {
        public static TimeSpentTracker Current { get; set; }

        private System.DateTime _startTime;
        private bool _isTracking;
        private int _accumulatedSeconds;

        public int TotalSeconds => _accumulatedSeconds + GetCurrentSessionSeconds();

        public void Start()
        {
            if (_isTracking)
            {
                return;
            }

            _startTime = System.DateTime.UtcNow;
            _isTracking = true;
        }

        public void Pause()
        {
            if (!_isTracking)
            {
                return;
            }

            _accumulatedSeconds += GetCurrentSessionSeconds();
            _isTracking = false;
        }

        public void Reset()
        {
            _accumulatedSeconds = 0;
            _isTracking = false;
        }

        private int GetCurrentSessionSeconds()
        {
            if (!_isTracking)
                return 0;

            return (int)(System.DateTime.UtcNow - _startTime).TotalSeconds;
        }

        public virtual void OnEvent(AtomEvent e)
        {
            switch (e.EventType)
            {
                case AtomEventType.LevelStarted:
                case AtomEventType.AppResumed:
                    Reset();
                    Start();
                    break;
                case AtomEventType.AppPaused:
                case AtomEventType.LevelPaused:
                case AtomEventType.LevelWin:
                case AtomEventType.LevelLose:
                case AtomEventType.RetryLevel:
                case AtomEventType.BackToMainMenu:
                    Pause();
                    break;
                case AtomEventType.LevelResumed:
                    Start();
                    break;
                case AtomEventType.LevelRevived:
                    Reset();
                    Start();
                    break;
            }
        }
    }
}
