using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Delta.Common.UI
{
    public class NoticeUI : UIController
    {
        [SerializeField] private TextMeshProUGUI _noticeText;

        public void ShowNoticeText(string text)
        {
            _noticeText.text = text;

            float duration = 2.0f;
            Sequence sequence = DOTween.Sequence();

            sequence.Append(_noticeText.DOFade(0, duration).SetEase(Ease.InQuad));
            sequence.Join(_noticeText.transform.DOLocalMoveY(300, duration));

            sequence.OnComplete(() =>
            {
                UIManager.Instance.ReleaseUI(this, true);
            });

            sequence.Play();
        }
    }
}
