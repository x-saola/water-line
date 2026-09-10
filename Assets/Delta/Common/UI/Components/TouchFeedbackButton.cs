using Delta.Core;
using Delta.Services;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delta.Common
{
    [RequireComponent(typeof(Button))]
    public class TouchFeedbackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
#if UNITY_EDITOR
    , IPointerEnterHandler, IPointerExitHandler
#endif
    {
        private IVibrationService _vibrationService;
        private IAudioService _audioService;

        private Button _button;

        private const float SCALE_DURATION = 0.1f;
        private const float SCALE_FACTOR = 0.95f;

        private Vector3 _originalScale;

        private void Awake()
        {
            _vibrationService = ServiceLocator.Get<IVibrationService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _originalScale = _button.transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable)
                return;

            _button.transform.DOScale(_originalScale * SCALE_FACTOR, SCALE_DURATION);

            _audioService.PlaySfx("button_click");
            _vibrationService.LightImpact();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_button.interactable)
                return;

            _button.transform.DOScale(_originalScale, SCALE_DURATION);
        }

#if UNITY_EDITOR
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable)
                return;

            _button.transform.DOScale(_originalScale * SCALE_FACTOR, SCALE_DURATION);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_button.interactable)
                return;

            _button.transform.DOScale(_originalScale, SCALE_DURATION);
        }
#endif
    }
}
