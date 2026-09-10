using DG.Tweening;
using UnityEngine;

namespace Delta.Modules.DailyChallenge_VerB
{
    public class CheckmarkToggleUI : MonoBehaviour
    {
        public void ShowCheckmark()
        {
            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOScale(1.2f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
