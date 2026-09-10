using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coffee.UIEffects;
using Delta.Common;
using Delta.Common.UI;
using Delta.Core;
using Delta.ProjectName;
using Delta.Services;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class GamePlayUI : UIController, IGameplayUIController
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _settingsBtn;
        [SerializeField] private RectTransform _pondZone;
        [SerializeField] private RectTransform _top;
        [SerializeField] private RectTransform _bottom;
        [SerializeField] private Image _categoryProgressImageFill;
        [SerializeField] private TextMeshProUGUI _categoryCompleteText;
        [SerializeField] private ParticleSystem _categoryCompleteEffect;
        [SerializeField] private Image _mergeStreakImageFill;
        [SerializeField] private ParticleSystem _mergeStreakParticleSystem;
        [SerializeField] private UIEffect _mergeStreakUIEffect;

        [Header("Booster Buttons")]
        [SerializeField] private RectTransform _boosterButtonZone;
        // [SerializeField] private BoosterButtonUI _boosterButtonPrefab;
        private const int BoosterDisplayMax = 4; // Hint + AutoMerge today, headroom for a future Freeze

        // private readonly Dictionary<BoosterType, BoosterButtonUI> _boosterButtons = new();

        [Header("Avatar Stacks")]
        [SerializeField] private Transform _stackContainer;

        [Header("Moves")]
        [SerializeField] private GameObject _moveContainer;
        [SerializeField] private TextMeshProUGUI _moveText;

        [Header("Level Difficulty")]
        [SerializeField] private Image _timerContainerImage;
        [SerializeField] private Sprite _normalTimerSprite;
        [SerializeField] private Sprite _hardTimerSprite;
        [SerializeField] private Sprite _veryHardTimerSprite;
        [Space]
        [SerializeField] private Image _levelContainerImage;
        [SerializeField] private Sprite _normalLevelContainerSprite;
        [SerializeField] private Sprite _hardLevelContainerSprite;
        [SerializeField] private Sprite _veryHardLevelContainerSprite;

        [Header("Congratulation Effect")]
        [SerializeField] private GameObject _congratulationEffect;

        public GameObject MoveContainer => _moveContainer;

        public void SetMoveTutorialHighlight(bool active)
        {
            // TutorialHighlightHelper.SetTutorialHighlight(_moveContainer, active);
        }

        public RectTransform PondZone => _pondZone;
        public Transform StackContainer => _stackContainer;

        public event Action OnSettings;
        public event Action OnShuffleRequested;
        public event Action OnAutoSolveRequested;
        public event Action OnAddStackRequested;

        // public void Setup(int level, int startingMoves, LevelDifficulty difficulty = LevelDifficulty.Normal)
        // {
        //     _levelText.text = $"Lv.{level}";
        //     UpdateMovesDisplay(startingMoves);
        //
        //     if (_timerContainerImage != null)
        //     {
        //         _timerContainerImage.sprite = difficulty switch
        //         {
        //             LevelDifficulty.Hard => _hardTimerSprite,
        //             LevelDifficulty.VeryHard => _veryHardTimerSprite,
        //             _ => _normalTimerSprite,
        //         };
        //     }
        //
        //     if (_levelContainerImage != null)
        //     {
        //         _levelContainerImage.sprite = difficulty switch
        //         {
        //             LevelDifficulty.Hard => _hardLevelContainerSprite,
        //             LevelDifficulty.VeryHard => _veryHardLevelContainerSprite,
        //             _ => _normalLevelContainerSprite,
        //         };
        //     }
        //
        //     _settingsBtn.onClick.RemoveAllListeners();
        //     _settingsBtn.onClick.AddListener(OnSettingsClicked);
        //
        //     // Boosters (Shuffle/Auto-solve/Add-stack) are out of scope for now - hide, don't wire.
        //     // _shuffleBtn.gameObject.SetActive(false);
        //     // _autoSolveBtn.gameObject.SetActive(false);
        //     // _addStackBtn.gameObject.SetActive(false);
        //
        //     _congratulationEffect.SetActive(false);
        // }
        //
        // // Bubble Pond has no timer (spec §5) - this slot shows the remaining moves budget instead.
        // public void UpdateMovesDisplay(int remainingMoves)
        // {
        //     _moveText.text = $"{remainingMoves}";
        // }
        //
        // public void SetUpBoosterButtons(int currentLevel, BoosterConfig[] boosterConfigs, Action<BoosterType> boosterCallback)
        // {
        //     foreach (Transform child in _boosterButtonZone)
        //         Destroy(child.gameObject);
        //
        //     _boosterButtons.Clear();
        //
        //     if (boosterConfigs == null) return;
        //
        //     for (int i = 0; i < BoosterDisplayMax && i < boosterConfigs.Length; i++)
        //     {
        //         var config = boosterConfigs[i];
        //         if (config == null) continue;
        //
        //         var button = Instantiate(_boosterButtonPrefab, _boosterButtonZone);
        //         bool unlocked = currentLevel >= config.UnlockLevel;
        //         button.Setup(config, unlocked, boosterCallback);
        //         button.SetInteractableState(false);
        //         _boosterButtons[config.Type] = button;
        //     }
        // }
        //
        // public BoosterButtonUI GetBoosterButton(BoosterType type) =>
        //     _boosterButtons.TryGetValue(type, out var button) ? button : null;

        protected override void OnUIRemoved()
        {
            // if (_controller == null) return;
            // _controller.OnLevelStarted -= OnLevelStarted;
            // _controller.OnLevelPaused  -= OnLevelPaused;
            // _controller.OnLevelResumed -= OnLevelResumed;
            // _controller.OnLevelWin     -= OnLevelStopped;
            // _controller.OnLevelLose    -= OnLevelStopped;
            // _controller = null;
        }

        private void OnSettingsClicked()
        {
            OnSettings?.Invoke();
        }

        private void OnShuffleClicked() => OnShuffleRequested?.Invoke();
        private void OnAutoSolveClicked() => OnAutoSolveRequested?.Invoke();
        private void OnAddStackClicked() => OnAddStackRequested?.Invoke();

        private float GetSafeAreaTop()
        {
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return (Screen.height - Screen.safeArea.yMax) / scale;
        }

        private float GetSafeAreaBottom()
        {
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return Screen.safeArea.yMin / scale;
        }

        public Task PlayAppearAnimation(float duration = 0.4f)
        {
            var topPos = _top.anchoredPosition;
            topPos.y = _top.rect.size.y + GetSafeAreaTop();
            _top.anchoredPosition = topPos;

            var botPos = _bottom.anchoredPosition;
            botPos.y = -(_bottom.rect.size.y + GetSafeAreaBottom());
            _bottom.anchoredPosition = botPos;

            var tcs = new TaskCompletionSource<bool>();
            DOTween.Sequence()
                .Join(_top.DOAnchorPosY(0f, duration).SetEase(Ease.OutCubic))
                .Join(_bottom.DOAnchorPosY(0f, duration).SetEase(Ease.OutCubic))
                .OnComplete(() => tcs.SetResult(true));
            return tcs.Task;
        }

        public Task PlayDisappearAnimation(float duration = 0.4f)
        {
            var tcs = new TaskCompletionSource<bool>();
            DOTween.Sequence()
                .Join(_top.DOAnchorPosY(_top.rect.size.y + GetSafeAreaTop(), duration).SetEase(Ease.InCubic))
                .Join(_bottom.DOAnchorPosY(-(_bottom.rect.size.y + GetSafeAreaBottom() + 200f), duration).SetEase(Ease.InCubic))
                .OnComplete(() => tcs.SetResult(true));
            return tcs.Task;
        }

        private const float TutorialTransitionDuration = 0.4f;

        public Task PlayTutorialAppearAnimation() => PlayAppearAnimation(TutorialTransitionDuration);
        public Task PlayTutorialDisappearAnimation() => PlayDisappearAnimation(TutorialTransitionDuration);

        public void PlayTutorialCongratulationEffect()
        {
            _congratulationEffect.SetActive(true);
        }

        private const float FillTweenDuration = 0.3f;

        // animate=false snaps the fill instantly - used on level setup so it doesn't tween
        // from whatever value was left over from the previous level.
        public async void UpdateMergeStreakProgress(int current, int required, bool animate = true, bool isComplete = false, System.Action onComplete = null)
        {
            if (_mergeStreakImageFill == null) return;

            float fill = required > 0 ? (float)current / required : 0f;
            _mergeStreakImageFill.DOKill();

            if (isComplete && animate)
            {
                await PlayMergeStreakCompleteAnimation();
                onComplete?.Invoke();
            }

            if (animate)
                _mergeStreakImageFill.DOFillAmount(fill, FillTweenDuration).SetEase(Ease.OutCubic);
            else
                _mergeStreakImageFill.fillAmount = fill;
        }

        private Task PlayMergeStreakCompleteAnimation()
        {
            var tcs = new TaskCompletionSource<bool>();

            var sequence = DOTween.Sequence()
                .Join(_mergeStreakImageFill.DOFillAmount(1, FillTweenDuration).SetEase(Ease.OutCubic));

            if (_mergeStreakUIEffect != null)
            {
                sequence.Join(DOVirtual.Float(1f, 0f, 0.5f, value => _mergeStreakUIEffect.transitionRate = value)
                    .SetEase(Ease.OutCubic));
            }
            sequence.OnComplete(() => tcs.SetResult(true));

            if (_mergeStreakParticleSystem != null)
                _mergeStreakParticleSystem.Play();

            return tcs.Task;
        }
    }
}
