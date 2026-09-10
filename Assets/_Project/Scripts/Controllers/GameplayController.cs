using System.Threading.Tasks;
using UnityEngine;

namespace Delta.ProjectName
{
    public class GamePlayController : MonoBehaviour, IGameplayController, ILevelController
    {
        private IGameplayUIController _gameplayUI;

        private int _currentLevel;
        private bool _isStarted;
        private bool _isPaused;
        private bool _isOver;

        public int CurrentLevel => _currentLevel;
        public bool IsStarted => _isStarted;
        public bool IsPaused => _isPaused;
        public bool IsOver => _isOver;

        public event System.Action OnLevelStarted;
        public event System.Action OnLevelPaused;
        public event System.Action OnLevelResumed;
        public event System.Action OnLevelWin;
        public event System.Action OnLevelLose;

        public void Initialize(IGameplayUIController gameplayUI)
        {
            _gameplayUI = gameplayUI;
        }

        public async Task LoadLevelAssetAsync(int level)
        {
            // Load level assets asynchronously here
            await Task.CompletedTask;
        }

        public void SetupLevel(int level)
        {
            _isStarted = false;
            _isPaused = false;
            _isOver = false;

            _currentLevel = level;
            // Setup level logic here
        }

        public void StartLevel()
        {
            _isStarted = true;
            OnLevelStarted?.Invoke();
        }

        public void PauseLevel()
        {
            _isPaused = true;
            OnLevelPaused?.Invoke();
        }

        public void ResumeLevel()
        {
            _isPaused = false;
            OnLevelResumed?.Invoke();
        }

        private void StopGame(bool isWin)
        {
            _isOver = true;

            if (isWin)
            {
                OnLevelWin?.Invoke();
            }
            else
            {
                OnLevelLose?.Invoke();
            }
        }

        public void WinLevel()
        {
            StopGame(true);
        }

        public void LoseLevel()
        {
            StopGame(false);
        }

        public void RestartLevel()
        {
        }
    }
}
