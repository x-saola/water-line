using Delta.Core;
using Delta.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    public class SettingsUI : BounceAppearUI
    {
        [Header("Settings Panel")]
        [SerializeField] private Button _overlayButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private GameObject _navigationButtonsGroup;
        [SerializeField] private TextMeshProUGUI _infoText;

        [Header("Toggle Switch")]
        [SerializeField] private ToggleSwitch _musicToggle;
        [SerializeField] private ToggleSwitch _sfxToggle;
        [SerializeField] private ToggleSwitch _vibrationToggle;

        private bool _isInitialized = false;

        private IAudioService _audioService;
        private IVibrationService _vibrationService;

        public System.Action OnHomeButtonClick;
        public System.Action OnRetryButtonClick;
        public System.Action OnCloseButtonClick;

        protected override void Awake()
        {
            base.Awake();
            _audioService = ServiceLocator.Get<IAudioService>();
            _vibrationService = ServiceLocator.Get<IVibrationService>();
        }

        private void Start()
        {
            InitializeUI();
            RegisterListeners();
            SyncToggleStates();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_isInitialized)
            {
                SyncToggleStates();
            }
        }

        public void ShowNavigationButtons(bool isActive)
        {
            if (_navigationButtonsGroup != null)
            {
                _navigationButtonsGroup.SetActive(isActive);
            }
        }

        public void ShowSettingsPanel(bool showNavigation = false)
        {
            AtomEvent.Trigger(AtomEventType.LevelPaused);
            OpenSettingsPanel();
            ShowNavigationButtons(showNavigation);
        }

        private void InitializeUI()
        {
            _isInitialized = true;
            _infoText.text = "";
        }

        private void RegisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSettingsPanel);
            }

            if (_sfxToggle != null)
            {
                _sfxToggle.onValueChanged.RemoveAllListeners();
                _sfxToggle.AddListener(OnSfxToggleChanged);
            }

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.RemoveAllListeners();
                _musicToggle.AddListener(OnMusicToggleChanged);
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.RemoveAllListeners();
                _vibrationToggle.AddListener(OnVibrationToggleChanged);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(OnRetryButtonClicked);
            }
        }

        #region Toggle Handlers

        private void OnSfxToggleChanged(bool isOn)
        {
            _audioService.IsSfxEnable = isOn;
            _vibrationService.LightImpact();
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            _audioService.IsMusicEnable = isOn;
            _vibrationService.LightImpact();
        }

        private void OnVibrationToggleChanged(bool isOn)
        {
            _vibrationService.IsOpen = isOn;
            if (isOn) _vibrationService.LightImpact();
        }

        #endregion

        #region Button Handlers

        private void OnHomeButtonClicked()
        {
            OnHomeButtonClick?.Invoke();
        }

        private void OnRetryButtonClicked()
        {
            OnRetryButtonClick?.Invoke();
        }

        #endregion

        private void SyncToggleStates()
        {
            if (_sfxToggle != null) _sfxToggle.SetIsOn(_audioService.IsSfxEnable, notify: false);
            if (_musicToggle != null) _musicToggle.SetIsOn(_audioService.IsMusicEnable, notify: false);
            if (_vibrationToggle != null) _vibrationToggle.SetIsOn(_vibrationService.IsOpen, notify: false);
        }

        #region Panel Management

        private void CloseSettingsPanel()
        {
            AtomEvent.Trigger(AtomEventType.LevelResumed);
            OnCloseButtonClick?.Invoke();
            UIManager.Instance.ReleaseUI(this, true);
        }

        public void OpenSettingsPanel()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
                PlayAppearAnimation();
                SyncToggleStates();
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (_overlayButton != null)
                _overlayButton.onClick.RemoveAllListeners();

            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();

            if (_homeButton != null)
                _homeButton.onClick.RemoveAllListeners();

            if (_retryButton != null)
                _retryButton.onClick.RemoveAllListeners();

            if (_sfxToggle != null) _sfxToggle.onValueChanged.RemoveAllListeners();
            if (_musicToggle != null) _musicToggle.onValueChanged.RemoveAllListeners();
            if (_vibrationToggle != null) _vibrationToggle.onValueChanged.RemoveAllListeners();
        }
    }
}
