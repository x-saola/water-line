using Delta.Common;
using Delta.Core;
using Delta.Services;
using Delta.Common.UI;
using Delta.GameOps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delta.ProjectName
{
    public class GameState_Play : GameState
    {
        private IAdService _adService;
        private IAudioService _audioService;
        private ITrackingService _trackingService;
        private IUserDataService _userDataService;

        private UserData Data => (UserData)_userDataService.Data;

        private ILevelController _gameplayController;
        private TimeSpentTracker _timeSpentTracker;
        private EventBinding<AtomEvent> _atomEventBinding;

        private string _gameMode;
        private string _levelId;
        private string _levelUniqueId;
        private int _attempts;

        private SettingsUI _settingsUI;

        protected override void OnEnter()
        {
            _adService = ServiceLocator.Get<IAdService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _trackingService = ServiceLocator.Get<ITrackingService>();
            _userDataService = ServiceLocator.Get<IUserDataService>();

            _adService.SetBannerPlacement("Gameplay");
            _gameMode = "Normal";
            _timeSpentTracker = new TimeSpentTracker();

            SceneManager.LoadSceneAsync("Gameplay").completed += async _ =>
            {
                InitializeGameplay();

                _settingsUI = UIManager.Instance.ShowUIOnTop<SettingsUI>("SettingsUI");
                _settingsUI.OnHomeButtonClick += BackToMainMenu;
                _settingsUI.OnRetryButtonClick += RetryLevel;
            };

            _atomEventBinding = new EventBinding<AtomEvent>(_timeSpentTracker.OnEvent);
            EventBus<AtomEvent>.Register(_atomEventBinding);
            DeltaApp.Instance.OnAppPaused += OnAppPaused;
        }

        protected override void OnExit()
        {
            DeltaApp.Instance.OnAppPaused -= OnAppPaused;
            EventBus<AtomEvent>.Deregister(_atomEventBinding);
            UIManager.Instance.ReleaseAllUIInstances();
        }

        private void OnAppPaused(bool isPaused)
        {
            if (!_gameplayController.IsStarted || _gameplayController.IsOver)
                return;

            if (isPaused)
            {
                AtomEvent.Trigger(AtomEventType.AppPaused);
                _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            }
            else
            {
                AtomEvent.Trigger(AtomEventType.AppResumed);
            }
        }

        private void InitializeGameplay()
        {
            var goGameplayController = new GameObject("GameplayController");
            _gameplayController = goGameplayController.AddComponent<GamePlayController>();
            _gameplayController.OnLevelStarted += OnLevelStarted;
            _gameplayController.OnLevelWin += OnLevelWin;
            _gameplayController.OnLevelLose += OnLevelLose;
        }

        private void OnLevelStarted()
        {
            AtomEvent.Trigger(AtomEventType.LevelStarted);

            _attempts = Data.ClassicLevelModeData.IncrementLevelAttempts(_gameplayController.CurrentLevel);

            var now = System.DateTime.UtcNow;
            var todayString = $"{now.Year}{now.Month:00}{now.Day:00}";
            _levelId = _gameplayController.CurrentLevel.ToString();
            _levelUniqueId = $"{_levelId}i{_attempts}a{todayString}d";

            _trackingService.TrackGameStart(_gameMode, _levelUniqueId, _levelId, _attempts);
        }

        private void OnLevelOver(bool isWin)
        {
            if (isWin)
                _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, 1, 0, _attempts, null);
            else
                _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, 0, 0, _attempts, "TimeOut");
        }

        private void OnLevelWin()
        {
            AtomEvent.Trigger(AtomEventType.LevelWin);
            OnLevelOver(true);
        }

        private void OnLevelLose()
        {
            AtomEvent.Trigger(AtomEventType.LevelLose);
            OnLevelOver(false);
        }

        public void RetryLevel()
        {
            AtomEvent.Trigger(AtomEventType.RetryLevel);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            Machine.ChangeState<GameState_Play>();
        }

        public void BackToMainMenu()
        {
            AtomEvent.Trigger(AtomEventType.BackToMainMenu);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            Machine.ChangeState<GameState_MainMenu>();
        }
    }
}
