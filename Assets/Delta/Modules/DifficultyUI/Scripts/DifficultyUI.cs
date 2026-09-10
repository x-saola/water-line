using Delta.Common.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class DifficultyUI : UIController
    {
        [SerializeField] Image _overlayImage;
        [SerializeField] Image _flashImage;
        [SerializeField] Image _difficultyIcon;
        [SerializeField] RectTransform _panel;

        public Tween PlayAppearAnimation()
        {
            _flashImage.DOFade(1f, 0.33f).SetEase(Ease.InOutCubic).SetLoops(-1, LoopType.Yoyo);

            Sequence sequence = DOTween.Sequence();

            _overlayImage.color = new Color(0, 0, 0, 0);
            _flashImage.color = new Color(1, 1, 1, 0);
            _difficultyIcon.rectTransform.localScale = Vector3.zero;

            sequence.Append(_panel.DOAnchorPosY(0f, 0.5f).From(new Vector2(0, 300f)).SetEase(Ease.OutCubic));
            sequence.Join(_overlayImage.DOFade(253f / 255f, 0.5f).SetEase(Ease.OutCubic));
            sequence.Append(_difficultyIcon.rectTransform.DOScale(1f, 0.33f).SetEase(Ease.OutBack));
            sequence.AppendInterval(1.3f);
            sequence.Append(_difficultyIcon.rectTransform.DOScale(0f, 0.33f).SetEase(Ease.InBack));
            sequence.Join(_panel.DOAnchorPosY(-Screen.height/2 - 500, 0.5f).SetEase(Ease.InCubic));
            sequence.Join(_overlayImage.DOFade(0f, 0.5f).SetEase(Ease.InCubic));

            return sequence;
        }
    }
}
