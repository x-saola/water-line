using Delta.Common.UI;
using DG.Tweening;
using UnityEngine;

namespace Delta.Common.UI
{
    public class BounceAppearUI : UIController
    {
        [Space]
        [SerializeField] protected CanvasGroup _parentContent;

        protected virtual void Awake()
        {
            if (_parentContent == null)
            {
                _parentContent = gameObject.AddComponent<CanvasGroup>();
            }
        }

        protected virtual void OnEnable()
        {
            PlayAppearAnimation();
        }

        public virtual void PlayAppearAnimation()
        {
            _parentContent.interactable = false;
            _parentContent.transform.localScale = Vector2.zero;
            _parentContent.transform.DOScale(1, 0.33f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _parentContent.interactable = true;
                });
        }
    }
}
