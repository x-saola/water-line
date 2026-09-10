using UnityEngine;
using UnityEngine.UI;

namespace Delta.Common
{
    public class SwitchUI : MonoBehaviour
    {
        [SerializeField]
        private Sprite[] _stateSprites;
        [SerializeField]
        private Image _stateImage;
        [SerializeField]
        private RectTransform _slider;
        [SerializeField]
        private float _sliderOnPosition, _sliderOffPosition;

        public void SetOn()
        {
            /*
            _slider.DOKill();
            _slider.DOAnchorPosX(_sliderOnPosition, .25f).OnComplete(() => {
                _stateImage.overrideSprite = _stateSprites[0];
            });
            */
            SetOnImmediately();
        }

        public void SetOff()
        {
            /*
            _slider.DOKill();
            _slider.DOAnchorPosX(_sliderOffPosition, .25f).OnComplete(() => {
                _stateImage.overrideSprite = _stateSprites[1];
            });
            */
            SetOffImmediately();
        }

        public void SetOnImmediately()
        {
            _slider.anchoredPosition = new Vector2(_sliderOnPosition, _slider.anchoredPosition.y);
            _stateImage.overrideSprite = _stateSprites[0];
        }

        public void SetOffImmediately()
        {
            _slider.anchoredPosition = new Vector2(_sliderOffPosition, _slider.anchoredPosition.y);
            _stateImage.overrideSprite = _stateSprites[1];
        }
    }
}
