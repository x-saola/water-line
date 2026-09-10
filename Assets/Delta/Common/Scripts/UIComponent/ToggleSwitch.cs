using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [Header("Knob")]
        [SerializeField] RectTransform _knob;
        [SerializeField] float _knobOffX = -30f;
        [SerializeField] float _knobOnX = 30f;
        [SerializeField] Image _knobImageOn;
        [SerializeField] Image _knobImageOff;

        [Header("Background")]
        [SerializeField] Image _bgImageOn;
        [SerializeField] Image _bgImageOff;

        [Header("Labels")]
        [SerializeField] TMP_Text _textOn;
        [SerializeField] TMP_Text _textOff;

        [Header("Animation")]
        [SerializeField] float _animDuration = 0.2f;

        [System.Serializable] public class ToggleEvent : UnityEvent<bool> { }
        public ToggleEvent onValueChanged = new ToggleEvent();

        private bool _isOn;
        public bool IsOn => _isOn;

        private void Awake()
        {
            SetVisualImmediate(_isOn);
        }

        public void SetIsOn(bool value, bool notify = true)
        {
            _isOn = value;
            AnimateKnob(value);
            AnimateVisuals(value);
            if (notify) onValueChanged.Invoke(value);
        }

        public void AddListener(UnityAction<bool> call) => onValueChanged.AddListener(call);
        public void RemoveListener(UnityAction<bool> call) => onValueChanged.RemoveListener(call);

        public void OnPointerClick(PointerEventData _) => SetIsOn(!_isOn);

        private void AnimateKnob(bool on)
        {
            if (_knob == null) return;
            float targetX = on ? _knobOnX : _knobOffX;
            _knob.DOAnchorPosX(targetX, _animDuration).SetEase(Ease.OutCubic);
        }

        private void AnimateVisuals(bool on)
        {
            FadeImage(_knobImageOn, on ? 1f : 0f);
            FadeImage(_knobImageOff, on ? 0f : 1f);
            FadeImage(_bgImageOn, on ? 1f : 0f);
            FadeImage(_bgImageOff, on ? 0f : 1f);
            FadeText(_textOn, on ? 1f : 0f);
            FadeText(_textOff, on ? 0f : 1f);
        }

        private void SetVisualImmediate(bool on)
        {
            SetImageAlpha(_knobImageOn, on ? 1f : 0f);
            SetImageAlpha(_knobImageOff, on ? 0f : 1f);
            SetImageAlpha(_bgImageOn, on ? 1f : 0f);
            SetImageAlpha(_bgImageOff, on ? 0f : 1f);
            SetTextAlpha(_textOn, on ? 1f : 0f);
            SetTextAlpha(_textOff, on ? 0f : 1f);

            if (_knob != null)
            {
                var pos = _knob.anchoredPosition;
                pos.x = on ? _knobOnX : _knobOffX;
                _knob.anchoredPosition = pos;
            }
        }

        private void FadeImage(Image img, float alpha)
        {
            if (img == null) return;
            img.DOFade(alpha, _animDuration).SetEase(Ease.OutCubic);
        }

        private void FadeText(TMP_Text text, float alpha)
        {
            if (text == null) return;
            text.DOFade(alpha, _animDuration).SetEase(Ease.OutCubic);
        }

        private void SetImageAlpha(Image img, float alpha)
        {
            if (img == null) return;
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }

        private void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text == null) return;
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
