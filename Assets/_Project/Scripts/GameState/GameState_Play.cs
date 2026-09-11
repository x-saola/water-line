using System;
using System.Threading;
using System.Threading.Tasks;
using Delta.Common;
using Delta.Core;
using Delta.Services;
using Delta.Common.UI;
using Delta.GameOps;
using Delta.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delta.ProjectName
{
    public class GameState_Play : GameState
    {
        private IAdService _adService;
        private AdConfigSO _adConfig;
        private IAudioService _audioService;
        private ITrackingService _trackingService;
        private IUserDataService _userDataService;

        private UserData Data => (UserData)_userDataService.Data;

        private GamePlayController _gameplayController;
        private GamePlayUI _gameplayUI;
        private TimeSpentTracker _timeSpentTracker;
        private EventBinding<AtomEvent> _atomEventBinding;

        private string _gameMode;
        private string _levelId;
        private string _levelUniqueId;
        private int _attempts;

        // Set by OnNextRequested; ResolveLevelIdToPlay consumes and clears it. No player-facing
        // progression/level-select exists (out of scope, see plan) - absent an override this
        // always defaults to the first saved level.
        private string _forcedLevelId;

        private SettingsUI _settingsUI;
        private CancellationTokenSource _initializeGameplayCts;

        protected override void OnEnter()
        {
            _adService = ServiceLocator.Get<IAdService>();
            ServiceLocator.TryGet<AdConfigSO>(out _adConfig);
            _audioService = ServiceLocator.Get<IAudioService>();
            _trackingService = ServiceLocator.Get<ITrackingService>();
            _userDataService = ServiceLocator.Get<IUserDataService>();

            _adService.SetBannerPlacement("Gameplay");
            _gameMode = "Normal";
            _timeSpentTracker = new TimeSpentTracker();
            TimeSpentTracker.Current = _timeSpentTracker;

            SceneManager.LoadSceneAsync("Gameplay").completed += _ => OnSceneLoaded();

            _atomEventBinding = new EventBinding<AtomEvent>(_timeSpentTracker.OnEvent);
            EventBus<AtomEvent>.Register(_atomEventBinding);
            DeltaApp.Instance.OnAppPaused += OnAppPaused;
        }

        protected override void OnExit()
        {
            if (_gameplayController != null)
            {
                _gameplayController.OnLevelStarted -= OnLevelStarted;
                _gameplayController.OnLevelWin -= OnLevelWin;
                _gameplayController.OnLevelLose -= OnLevelLose;
            }

            DeltaApp.Instance.OnAppPaused -= OnAppPaused;
            EventBus<AtomEvent>.Deregister(_atomEventBinding);
            UIManager.Instance.ReleaseAllUIInstances();

            _initializeGameplayCts?.Cancel();
            _initializeGameplayCts?.Dispose();
            _initializeGameplayCts = null;
        }

        private void OnAppPaused(bool isPaused)
        {
            if (!_gameplayController.IsStarted || _gameplayController.IsOver)
                return;

            if (isPaused)
            {
                AtomEvent.Trigger(AtomEventType.AppPaused);
                _trackingService?.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
                _userDataService?.Save();
            }
            else
            {
                AtomEvent.Trigger(AtomEventType.AppResumed);
            }
        }

        private void OnSceneLoaded()
        {
            _gameplayController = UnityEngine.Object.FindFirstObjectByType<GamePlayController>();

            _gameplayController.OnLevelStarted += OnLevelStarted;
            _gameplayController.OnLevelWin += OnLevelWin;
            _gameplayController.OnLevelLose += OnLevelLose;

            InitializeGameplay();
        }

        private async void InitializeGameplay()
        {
            _initializeGameplayCts?.Cancel();
            _initializeGameplayCts?.Dispose();
            _initializeGameplayCts = new CancellationTokenSource();
            var token = _initializeGameplayCts.Token;

            UIManager.Instance.ReleaseAllUIs();

            _gameplayUI = UIManager.Instance.ShowUIOnTop<GamePlayUI>("GameplayUI");
            _gameplayUI.gameObject.SetActive(false);
            _gameplayUI.OnSettings -= Settings;
            _gameplayUI.OnSettings += Settings;
            _gameplayController.Initialize(_gameplayUI);

            string levelId = ResolveLevelIdToPlay();
            bool loaded = await _gameplayController.LoadLevelAsync(levelId);
            if (token.IsCancellationRequested) return;

            if (!loaded)
            {
                Debug.LogError($"[GameState_Play] Failed to load level '{levelId}'.");
                BackToMainMenu();
                return;
            }

            _gameplayController.SetupLevel(_gameplayController.LoadedLevelConfig);
            if (token.IsCancellationRequested) return;

            if (LoadingUI.Instance != null)
            {
                var loadingUI = LoadingUI.Instance;
                await loadingUI.WaitForMinimumDisplay();
                await loadingUI.PlayDisappearAnimation();
                UIManager.Instance.ReleaseUI(loadingUI, true);
            }
            if (token.IsCancellationRequested) return;

            _gameplayUI.gameObject.SetActive(true);
            await _gameplayUI.PlayAppearAnimation();
            if (token.IsCancellationRequested) return;

            _gameplayController.StartLevel();

            int levelNumber = ParseLevelNumber(_gameplayController.CurrentLevelId);
            if (_adConfig == null || levelNumber >= _adConfig.BannerShowFromLevel)
                _adService.ActiveBannerAd();
            else
                _adService.DeactiveBannerAd();
        }

        // No player-facing level sequence/progression exists (GDD leaves this undefined - see
        // plan's open questions). "level_1" is a placeholder entry point; the intended real
        // entry point is WaterlinePlaySession.PlayLevel(config), checked first inside
        // GamePlayController.LoadLevelAsync.
        private string ResolveLevelIdToPlay()
        {
            if (!string.IsNullOrEmpty(_forcedLevelId))
            {
                var id = _forcedLevelId;
                _forcedLevelId = null;
                return id;
            }
            return "level_1";
        }

        private static int ParseLevelNumber(string levelId)
        {
            const string prefix = "level_";
            if (!string.IsNullOrEmpty(levelId) && levelId.StartsWith(prefix) &&
                int.TryParse(levelId.Substring(prefix.Length), out int n))
                return n;
            return 0;
        }

        private void OnLevelStarted()
        {
            AtomEvent.Trigger(AtomEventType.LevelStarted);

            _levelId = _gameplayController.CurrentLevelId;
            _attempts = Data.WaterlineProgress.IncrementAttempts(_levelId);

            var now = System.DateTime.UtcNow;
            var todayString = $"{now.Year}{now.Month:00}{now.Day:00}";
            _levelUniqueId = $"{_levelId}i{_attempts}a{todayString}d";

            _trackingService.TrackGameStart(_gameMode, _levelUniqueId, _levelId, _attempts);
        }

        private async void OnLevelWin()
        {
            AtomEvent.Trigger(AtomEventType.LevelWin);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, 1, 0, _attempts, null);

            Data.WaterlineProgress.RecordCompletion(_levelId, _gameplayController.ElapsedSeconds);
            _userDataService.Save();

            await _gameplayUI.PlayDisappearAnimation();

            _audioService.PlaySfx("victory");
            _audioService.PauseBackgroundMusic();

            int levelNumber = ParseLevelNumber(_levelId);
            CheckShowInterstitialAd(levelNumber, () =>
            {
                var winUI = UIManager.Instance.ShowUIOnTop<WinLevelUI>("WinLevelUI");
                winUI.SetActiveHomeButton(false);
                winUI.SetActiveRetryButton(true);
                winUI.SetActiveContinueButton(true);
                winUI.SetCompletionTime(_gameplayController.FormatCompletionTime());
                winUI.OnRetryButtonClicked = RetryLevel;
                winUI.OnContinueButtonClicked = OnNextRequested;
                winUI.PlayAppearAnimation();
            });
        }

        private async void OnLevelLose()
        {
            AtomEvent.Trigger(AtomEventType.LevelLose);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, 0, 0, _attempts, "OutOfTime");
            _userDataService.Save();

            await _gameplayUI.PlayDisappearAnimation();

            _audioService.PlaySfx("lose");
            _audioService.PauseBackgroundMusic();

            var loseUI = UIManager.Instance.ShowUIOnTop<LoseLevelUI>("LoseLevelUI");
            loseUI.ShowReviveOption(false);
            loseUI.SetLoseCause("Out of Time");
            loseUI.OnRetry -= RetryLevel;
            loseUI.OnRetry += RetryLevel;
            loseUI.PlayAppearAnimation();
        }

        // Immediate reset to the authored layout - no disk reload/loading screen, matching the
        // spec's "Out of Time overlay offers an immediate retry."
        private void RetryLevel()
        {
            AtomEvent.Trigger(AtomEventType.RetryLevel);
            UIManager.Instance.ReleaseAllUIs();

            _gameplayController.RestartLevel();

            _gameplayUI = UIManager.Instance.ShowUIOnTop<GamePlayUI>("GameplayUI");
            _gameplayUI.OnSettings -= Settings;
            _gameplayUI.OnSettings += Settings;
            _gameplayController.Initialize(_gameplayUI);
        }

        // "Next" - a bare sequential disk lookup (level_{n+1}.json), not a level-select screen
        // (out of scope, see plan). Falls back Home if there's no next level saved yet.
        private void OnNextRequested()
        {
            if (LevelLoader.TryGetNextLevelId(_levelId, out var nextLevelId))
            {
                _forcedLevelId = nextLevelId;
                InitializeGameplay();
            }
            else
            {
                BackToMainMenu();
            }
        }

        private void Settings_RetryLevel()
        {
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            RetryLevel();
        }

        public void BackToMainMenu()
        {
            AtomEvent.Trigger(AtomEventType.BackToMainMenu);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            Machine.ChangeState<GameState_MainMenu>();
        }

        public void Settings()
        {
            _settingsUI = UIManager.Instance.ShowUIOnTop<SettingsUI>("SettingsUI");
            _settingsUI.OnHomeButtonClick = BackToMainMenu;
            _settingsUI.OnRetryButtonClick = Settings_RetryLevel;
            _settingsUI.ShowSettingsPanel(true);
        }

        private void CheckShowInterstitialAd(int completedLevel, Action onAdClosed)
        {
            bool showAd = !Data.HasNoAds;
#if ENABLE_CHEAT
            showAd = showAd && !CheatManager.Instance.SkipAds;
#endif
            if (showAd)
            {
                showAd = _adConfig != null
                         && completedLevel >= _adConfig.InterstitialShowFromLevel
                         && _adService.IsReadyToShowInterstitialAd(_adConfig.InterstitialIntervalSeconds);
            }

            if (showAd)
                _adService.ShowInterstitialAd("GAME_PLAY", _ => onAdClosed?.Invoke());
            else
                onAdClosed?.Invoke();
        }

        public void CheatReloadGameplay()
        {
            InitializeGameplay();
        }
    }
}
