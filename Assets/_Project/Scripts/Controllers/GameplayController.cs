using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Delta.ProjectName
{
    // Orchestrates one Waterline attempt: loads a LevelConfig, builds a fresh GridManager for
    // it, wires the board views + input, and runs the Win/Lose evaluators every accepted move
    // and every timer tick. Scene-found by GameState_Play (FindFirstObjectByType), not a
    // ServiceLocator-registered service - matches the existing controller pattern in this codebase.
    public class GamePlayController : MonoBehaviour, IGameplayController, ILevelController
    {
        [SerializeField] GridView _gridView;
        [SerializeField] PipeView _pipeView;
        [SerializeField] PipeInputController _pipeInput;
        [SerializeField] WaterFlowAnimator _waterFlowAnimator;

        IGameplayUIController _gameplayUI;

        LevelConfig _levelConfig;
        GridManager _grid;
        LevelTimer _timer;
        readonly WinEvaluator _winEvaluator = new();
        readonly LoseEvaluator _loseEvaluator = new();

        bool _isStarted;
        bool _isPaused;
        bool _isOver;
        bool _anyMoveMade;

        public LevelConfig LoadedLevelConfig { get; private set; }
        public string CurrentLevelId => _levelConfig?.LevelId;
        public bool IsStarted => _isStarted;
        public bool IsPaused => _isPaused;
        public bool IsOver => _isOver;
        public float ElapsedSeconds => _timer?.Elapsed ?? 0f;

        public event Action OnLevelStarted;
        public event Action OnLevelPaused;
        public event Action OnLevelResumed;
        public event Action OnLevelWin;
        public event Action OnLevelLose;

        void Awake()
        {
            if (_pipeInput != null)
                _pipeInput.OnStepResolved += OnStepResolved;
        }

        void OnDestroy()
        {
            if (_pipeInput != null)
                _pipeInput.OnStepResolved -= OnStepResolved;
        }

        void Update()
        {
            if (!_isStarted || _isOver || _isPaused || _timer == null) return;

            _timer.Tick(Time.deltaTime);
            _gameplayUI?.UpdateTimerDisplay(FormatTimerDisplay());

            if (_timer.IsTimed)
            {
                var loseResult = _loseEvaluator.Evaluate(BuildSessionState());
                if (loseResult.IsLose)
                    LoseLevel();
            }
        }

        public void Initialize(IGameplayUIController gameplayUI) => _gameplayUI = gameplayUI;

        // Checks WaterlinePlaySession first (manual verification today, a future Level Editor
        // "Play this level" button later), falling back to disk by levelId.
        public Task<bool> LoadLevelAsync(string levelId)
        {
            var pending = WaterlinePlaySession.ConsumePendingLevelConfig();
            if (pending != null)
            {
                if (!LevelLoader.ValidateForPlay(pending, out var pendingError))
                {
                    Debug.LogError($"[GamePlayController] Pending level failed validation: {pendingError}");
                    return Task.FromResult(false);
                }

                LoadedLevelConfig = pending;
                return Task.FromResult(true);
            }

            if (!LevelLoader.TryLoadFromDisk(levelId, out var config, out var error))
            {
                Debug.LogError($"[GamePlayController] {error}");
                return Task.FromResult(false);
            }

            LoadedLevelConfig = config;
            return Task.FromResult(true);
        }

        public void SetupLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[GamePlayController] SetupLevel called with a null config.");
                return;
            }

            _levelConfig = config;
            _isStarted = false;
            _isPaused = false;
            _isOver = false;
            _anyMoveMade = false;

            _grid = new GridManager(_levelConfig);
            _timer = new LevelTimer(_levelConfig.TimeLimitSeconds);

            _gridView.Build(_grid);
            _pipeView.Clear();
            _pipeView.Attach(_grid);
            _pipeView.RefreshAll();
            _pipeInput.Detach();
            _pipeInput.Attach(_grid);
            _pipeInput.SetInputLocked(true); // unlocked in StartLevel

            _gameplayUI?.UpdateTimerDisplay(FormatTimerDisplay());
        }

        public void StartLevel()
        {
            _isStarted = true;
            _pipeInput.SetInputLocked(false);
            OnLevelStarted?.Invoke();
        }

        public void PauseLevel()
        {
            _isPaused = true;
            _pipeInput.SetInputLocked(true);
            OnLevelPaused?.Invoke();
        }

        public void ResumeLevel()
        {
            _isPaused = false;
            if (_isStarted && !_isOver) _pipeInput.SetInputLocked(false);
            OnLevelResumed?.Invoke();
        }

        public void WinLevel() => StopGame(true);
        public void LoseLevel() => StopGame(false);

        // Immediate reset to the authored layout - re-runs SetupLevel/StartLevel against the
        // same LevelConfig instance rather than re-loading from disk, so retry has no loading flash.
        public void RestartLevel()
        {
            if (_levelConfig == null) return;
            SetupLevel(_levelConfig);
            StartLevel();
        }

        public string FormatCompletionTime() => _timer?.FormatCompletionTime() ?? "0:00";

        void StopGame(bool isWin)
        {
            _isOver = true;
            _pipeInput.SetInputLocked(true);
            _timer?.Stop();

            if (isWin) _ = PlayWinAnimationThenNotify();
            else OnLevelLose?.Invoke();
        }

        async Task PlayWinAnimationThenNotify()
        {
            if (_waterFlowAnimator != null)
                await _waterFlowAnimator.PlayAsync(_pipeView, _grid);
            OnLevelWin?.Invoke();
        }

        void OnStepResolved(PipeId pipeId, MoveResult result)
        {
            if (!result.Success) return;

            if (!_anyMoveMade)
            {
                _anyMoveMade = true;
                _timer.Start();
            }

            _pipeView.RefreshAll();

            var winResult = _winEvaluator.Evaluate(BuildSessionState());
            if (winResult.IsWin)
                WinLevel();
        }

        string FormatTimerDisplay()
        {
            if (!_timer.IsTimed) return _timer.FormatCompletionTime(); // count-up display for untimed levels
            int remaining = Mathf.Max(0, Mathf.CeilToInt(_timer.RemainingSeconds));
            return $"{remaining / 60}:{remaining % 60:00}";
        }

        GameplaySessionState BuildSessionState() => new(_levelConfig, _grid, _timer);
    }
}
