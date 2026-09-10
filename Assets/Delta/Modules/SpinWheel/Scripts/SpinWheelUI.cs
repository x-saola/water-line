using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Delta.Common.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.SpinWheel
{
    public class SpinWheelUI : UIController
    {
        [SerializeField][Range(.2f, 2f)] private float wheelSize = 1f;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TextMeshProUGUI _remainingSpinsText;
        [Header("References :")]
        [SerializeField] private Transform _linesParent;
        [SerializeField] private Button _closeButton;
        [Space]
        [SerializeField] private Transform _spinWheelTransform;
        [SerializeField] private Transform _wheelCircle;
        [SerializeField] private Transform _wheelPiecesParent;

        [Header("Rates")]
        [SerializeField] private Transform _rewardRateItemsParent;
        [SerializeField] private GameObject _rewardRateItemPrefab;

        [Header("Result screen :")]
        [SerializeField] private GameObject _resultScreen;
        [SerializeField] private TextMeshProUGUI _resultLabel;
        [SerializeField] private Image _resultIcon;
        [SerializeField] private TextMeshProUGUI _resultAmout;

        [Header("Animation")]
        [SerializeField] private List<Image> _lights;
        [SerializeField] private float _lightInterval = 0.4f;
        [SerializeField] private float _minLightInterval = 0.05f;
        [SerializeField] private float _accelerationFactor = 0.5f;    // Smaller is faster

        [Header("Other")]
        [SerializeField] private GameObject _appleText;
        [SerializeField] private TextMeshProUGUI _spinInstructionText;
        [SerializeField] private Image _buttonImage;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _disabledSprite;
        [SerializeField] private GameObject _adIcon;

        private Sequence _lightAnimationTween;
        private float _currentLightInterval;
        private Sequence _idleLightSequence;

        public Transform LinesParent { get => _linesParent; set => _linesParent = value; }
        public Transform SpinWheelTransform { get => _spinWheelTransform; set => _spinWheelTransform = value; }
        public Transform WheelCircle { get => _wheelCircle; set => _wheelCircle = value; }
        public Transform WheelPiecesParent { get => _wheelPiecesParent; set => _wheelPiecesParent = value; }
        public Button SpinButton { get => _spinButton; set => _spinButton = value; }

        void Start()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            _appleText.SetActive(false);
#endif
        }

        void OnEnable()
        {
            _closeButton.onClick.AddListener(Hide);
        }

        public void ShowResultScreen(SpinWheelReward reward)
        {
            _resultScreen.SetActive(true);
            if (reward != null)
            {
                _resultLabel.text = reward.label;
                _resultIcon.sprite = reward.icon;
                _resultAmout.text = "x" + reward.amount.ToString();
            }
        }

        private void OnValidate()
        {
            if (SpinWheelTransform != null)
                SpinWheelTransform.localScale = new Vector3(wheelSize, wheelSize, 1f);
        }

        public void Hide()
        {
            UIManager.Instance.ReleaseUI(this, true);
        }



        public void PlayLightAnimation()
        {
            StopLightAnimation();
            _currentLightInterval = _lightInterval;
            // Ensure all lights are initially disabled
            foreach (Image light in _lights)
            {
                light.gameObject.SetActive(false);
            }

            CreateLightSequence();
        }

        private void CreateLightSequence()
        {
            _lightAnimationTween = DOTween.Sequence();


            for (int i = 0; i < _lights.Count; i++)
            {
                int index = i;
                _lightAnimationTween
                    .AppendCallback(() => _lights[index].gameObject.SetActive(true))
                    .AppendInterval(_currentLightInterval)
                    .AppendCallback(() => _lights[index].gameObject.SetActive(false))
                    .AppendCallback(() => _currentLightInterval = Mathf.Max(_minLightInterval, _currentLightInterval * _accelerationFactor));
            }

            _lightAnimationTween.OnComplete(() =>
            {
                //_currentLightInterval = Mathf.Max(_minLightInterval, _currentLightInterval * _accelerationFactor);
                CreateLightSequence();
            });
        }

        public void StopLightAnimation()
        {
            // Kill the animation if it exists
            if (_lightAnimationTween != null && _lightAnimationTween.IsActive())
            {
                _lightAnimationTween.Kill();
                _lightAnimationTween = null;

                // Turn off all lights when stopping
                TurnOffLights();
            }
        }

        private void TurnOffLights()
        {
            foreach (Image light in _lights)
            {
                light.gameObject.SetActive(false);
            }
        }

        public void PlayIdleLightEffect()
        {
            bool toggle = false;
            _idleLightSequence = DOTween.Sequence();
            TurnOffLights();
            // Create a sequence that will repeat indefinitely
            _idleLightSequence
                .AppendCallback(() => toggle = !toggle)
                .AppendCallback(() => ActivateLightsAlternately(toggle))
                .AppendInterval(1f)
                .SetLoops(-1); // -1 means infinite loops
        }
        private void ActivateLightsAlternately(bool toggle)
        {
            TurnOffLights();

            for (int j = 0; j < _lights.Count; j++)
            {
                // Turn on lights based on alternating pattern
                if ((j % 2 == 0) == toggle)
                {
                    _lights[j].gameObject.SetActive(true);
                }
            }
        }

        public void StopIdleLightEffect()
        {
            if (_idleLightSequence != null && _idleLightSequence.IsActive())
            {
                _idleLightSequence.Kill();
                _idleLightSequence = null;
            }
        }


        void OnDisable()
        {
            SpinButton.onClick.RemoveAllListeners();
            StopIdleLightEffect();
            StopLightAnimation();
            _closeButton.onClick.RemoveListener(Hide);
        }

        public void UpdateRemainingSpinsText(int remainingSpins)
        {
            _remainingSpinsText.text = $"Daily Spin Limit {remainingSpins}/5";
            _buttonImage.sprite = _normalSprite;
            _spinButton.interactable = true;
            _adIcon.SetActive(remainingSpins != 5);

            if (remainingSpins <= 0)
            {
                _spinInstructionText.text = "Daily Spin Limit Reached";
                _buttonImage.sprite = _disabledSprite;
                _spinButton.interactable = false;
            }
        }

        public void SetupInfoPanel(WheelPiece[] wheelPieces)
        {
            wheelPieces = wheelPieces.OrderByDescending(x => x.Chance).ToArray();
            int numItems = wheelPieces.Length;
            for (int i = 0; i < numItems; i++)
            {
                if (wheelPieces[i].reward != null)
                {
                    GameObject item = Instantiate(_rewardRateItemPrefab, _rewardRateItemsParent);
                    item.GetComponent<RewardRateItem>().Setup(wheelPieces[i].reward.icon, wheelPieces[i].reward.amount, wheelPieces[i].Chance);
                }
            }
        }

        [ContextMenu("Reset Daily Spins")]
        public void ResetDailySpins()
        {
            SpinWheel.Instance.ResetDailySpins();
        }
    }
}
