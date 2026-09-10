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
        // private EconomyConfig _economyConfig;
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

        private SettingsUI _settingsUI;
        private DifficultyUI _levelDifficultyUI;
        private CancellationTokenSource _initializeGameplayCts;

        protected override void OnEnter()
        {
            _adService = ServiceLocator.Get<IAdService>();
            ServiceLocator.TryGet<AdConfigSO>(out _adConfig);
            // ServiceLocator.TryGet<EconomyConfig>(out _economyConfig);
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
            // _gameplayController.SetBackgroundCanvas(false);

            _gameplayUI = UIManager.Instance.ShowUIOnTop<GamePlayUI>("GameplayUI");
            _gameplayUI.OnSettings += Settings;

            _gameplayController.Initialize(_gameplayUI);
            _gameplayController.OnLevelStarted += OnLevelStarted;
            _gameplayController.OnLevelWin += OnLevelWin;
            _gameplayController.OnLevelLose += OnLevelLose;

            InitializeGameplay();

            if(_gameplayUI != null)
                _gameplayUI.OnSettings += Settings;
        }
        
        private async void InitializeGameplay(bool releaseAllUIs = false)
        {
            _initializeGameplayCts?.Cancel();
            _initializeGameplayCts?.Dispose();
            _initializeGameplayCts = new CancellationTokenSource();
            var token = _initializeGameplayCts.Token;

            if (releaseAllUIs)
                UIManager.Instance.ReleaseAllUIs();

            _gameplayUI = UIManager.Instance.ShowUIOnTop<GamePlayUI>("GameplayUI");
            _gameplayUI.gameObject.SetActive(false);

            int requestedLevel = Data.CurrentLevel;
            int levelToLoad = Data.ClassicLevelModeData.GetRandomLevel(requestedLevel);
            int resolvedContentLevel = await _gameplayController.LoadLevelAssetAsync(levelToLoad);
            if (token.IsCancellationRequested) return;

            if (resolvedContentLevel != requestedLevel)
            {
                Data.ClassicLevelModeData.SetRandomLevel(requestedLevel, resolvedContentLevel);
                _userDataService.Save();
            }
            
            _gameplayController.SetupLevel(requestedLevel);
            if (token.IsCancellationRequested) return;

            if (LoadingUI.Instance != null)
            {
                var loadingUI = LoadingUI.Instance;
                await loadingUI.WaitForMinimumDisplay();
                await loadingUI.PlayDisappearAnimation();
                UIManager.Instance.ReleaseUI(loadingUI, true);
            }
            if (token.IsCancellationRequested) return;
            
            // // Play music
            // if (_audioService.IsMusicPaused)
            //     _audioService.ResumeBackgroundMusic();
            // else if (_audioService.IsMusicStopped)
            //     _audioService.PlayBackgroundMusic();
            //
            // // Show gameplay UI
            // _gameplayController.SetBackgroundCanvas(true);
            // _gameplayUI.gameObject.SetActive(true);
            //
            // // Start spawning bubbles
            // var appearTask = IsEarlyLevel(requestedLevel) ? _gameplayUI.PlayTutorialAppearAnimation() : _gameplayUI.PlayAppearAnimation();
            // await Task.WhenAll(_gameplayController.SpawnLevelBubbles(), appearTask);
            //
            // if (token.IsCancellationRequested) return;
            //
            // // Show pending booster introduce popups
            // bool hadBoosterIntroPending = await _gameplayController.BoosterManager.ShowPendingIntroducePopupsAsync();
            // if (token.IsCancellationRequested) return;
            //
            // if (_gameplayController.TutorialManager.HasTutorialForLevel(requestedLevel) &&
            //     _gameplayController.TutorialManager.HasTutorialHandActive(requestedLevel, 0))
            // {
            //     var tutorialDelayTime = requestedLevel == 1 ? 1500 : 100;
            //     await Task.Delay(hadBoosterIntroPending ? 0 : tutorialDelayTime, token);
            //     if (token.IsCancellationRequested) return;
            // }
            //
            // await ShowDifficultyWarningAsync(_gameplayController.CurrentDifficulty);
            // if (token.IsCancellationRequested) return;

            _gameplayController.StartLevel();

            if (_adConfig == null || requestedLevel >= _adConfig.BannerShowFromLevel)
                _adService.ActiveBannerAd();
            else
                _adService.DeactiveBannerAd();
        }

        // Early levels (per TutorialConfig 1-4) chain straight into each other instead of
        // stopping at a win screen, so their appear/win transitions use the faster tutorial variants.
        private bool IsEarlyLevel(int levelCheck) => levelCheck <= 4;

        // Normal levels skip the banner entirely; Hard/VeryHard each get their own warning prefab/art.
        // private async Task ShowDifficultyWarningAsync(LevelDifficulty difficulty)
        // {
        //     if (difficulty == LevelDifficulty.Normal)
        //         return;
        //
        //     string prefabName = difficulty == LevelDifficulty.VeryHard
        //         ? "DifficultyUI_SuperHard"
        //         : "DifficultyUI_Hard";
        //
        //     _levelDifficultyUI = UIManager.Instance.ShowUIOnTop<DifficultyUI>(prefabName);
        //     await _levelDifficultyUI.PlayAppearAnimation();
        //     UIManager.Instance.ReleaseUI(_levelDifficultyUI, true);
        //     _levelDifficultyUI = null;
        // }

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
                _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, 0, 0, _attempts, "OutOfMove");
        }

        private async void OnLevelWin()
        {
            AtomEvent.Trigger(AtomEventType.LevelWin);
            OnLevelOver(true);

            int completedLevel = _gameplayController.CurrentLevel;
            Data.ClassicLevelModeData.SetLevelComplete(completedLevel);
            if (Data.ClassicLevelModeData.CurrentLevel <= completedLevel)
                Data.ClassicLevelModeData.CurrentLevel = completedLevel + 1;
            //
            // int coinReward = _economyConfig != null ? _economyConfig.LevelWinCoinReward : 10;
            // _userDataService.AddCurrency(EconomyConfig.CoinCurrencyId, coinReward, "LevelWin", completedLevel.ToString());
            _userDataService.Save();

            if (IsEarlyLevel(completedLevel + 1))
            {
                _gameplayUI.PlayTutorialCongratulationEffect();
                await Task.Delay(1000);
                await _gameplayUI.PlayTutorialDisappearAnimation();
                InitializeGameplay(true);
                return;
            }
            
            await _gameplayUI.PlayDisappearAnimation();

            _audioService.PlaySfx("victory");
            _audioService.PauseBackgroundMusic();

            CheckShowInterstitialAd(completedLevel, () =>
            {
                var winUI = UIManager.Instance.ShowUIOnTop<WinLevelUI>("WinLevelUI");
                winUI.OnContinueButtonClicked = () => InitializeGameplay(true);
                winUI.OnHomeButtonClicked = BackToMainMenu;
                winUI.PlayAppearAnimation();
                // winUI.PlayCoinReward(0);
            });
        }

        private async void OnLevelLose()
        {
            AtomEvent.Trigger(AtomEventType.LevelLose);
            OnLevelOver(false);

            await _gameplayUI.PlayDisappearAnimation();

            _audioService.PlaySfx("lose");
            _audioService.PauseBackgroundMusic();

            var loseUI = UIManager.Instance.ShowUIOnTop<LoseLevelUI>("LoseLevelUI");
            loseUI.ShowReviveOption(false);
            loseUI.SetLoseCause("Out of moves");
            loseUI.OnRetry -= RetryLevel;
            loseUI.OnRetry += RetryLevel;
            loseUI.PlayAppearAnimation();
        }

        private void RetryLevel()
        {
            AtomEvent.Trigger(AtomEventType.RetryLevel);
            // _gameplayController.TutorialManager.StopTutorial();
            InitializeGameplay(true);
        }

        private void Settings_RetryLevel()
        {
            AtomEvent.Trigger(AtomEventType.RetryLevel);
            _trackingService.TrackGameOver(_gameMode, _levelUniqueId, _levelId, _timeSpentTracker.TotalSeconds, -1, 1, _attempts, null);
            // _gameplayController.TutorialManager.StopTutorial();
            InitializeGameplay(true);
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
