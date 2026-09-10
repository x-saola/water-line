using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    public class MemoryCard : MonoBehaviour
    {
        public System.Action<int> OnCardClick;
        [SerializeField] private Image _avatar;


        [SerializeField] private Transform _parentTransform;
        [SerializeField] private Transform _revealTransform;
        [SerializeField] private Transform _hiddenTransform;
        [SerializeField] private Transform _doneTransform;

        public int index;
        public bool isDone = false;
        public float animTimeScale = 0.5f;

        private void SetHidden()
        {
            _hiddenTransform.gameObject.SetActive(true);
            _revealTransform.gameObject.SetActive(false);
            _doneTransform.gameObject.SetActive(false);
        }

        private void SetReveal()
        {
            _hiddenTransform.gameObject.SetActive(false);
            _revealTransform.gameObject.SetActive(true);
            _doneTransform.gameObject.SetActive(false);
        }

        private void SetDone()
        {
            _revealTransform.gameObject.SetActive(true);
            _hiddenTransform.gameObject.SetActive(false);
            _doneTransform.gameObject.SetActive(true);
        }

        public void LoadAvatar(string id)
        {
            //Todo: Find way to Load Image here
           // _avatar.SetAvatar(id);
        }
        

        public void PlayFlip(System.Action onCompleted = null)
        {
            _parentTransform.transform.DOScaleX(0, 10 / 60f).onComplete += () =>
            {
                SetReveal();
                _parentTransform.transform.DOScaleX(1, 10 / 60f).onComplete += () =>
                {
                    onCompleted?.Invoke();
                };
            };
        }
        

        public void PlayFlipFail(System.Action onCompleted = null)
        {
            DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(0, 0, 10), animTimeScale * 4 / 60f))
                .Append(transform.DORotate(new Vector3(0, 0, 0), animTimeScale * 2 / 60f))
                .Append(transform.DORotate(new Vector3(0, 0, -10), animTimeScale * 4 / 60f))
                .Append(transform.DORotate(new Vector3(0, 0, 0), animTimeScale * 2 / 60f))
                .Play().SetLoops(2, LoopType.Restart).onComplete += () =>
                {
                    _parentTransform.transform.DOScaleX(0, animTimeScale * 10 / 60f).onComplete += () =>
                    {
                        SetHidden();
                        _parentTransform.transform.DOScaleX(1, animTimeScale * 10 / 60f).onComplete += () =>
                        {
                            onCompleted?.Invoke();
                        };
                    };
                };
        }

        public void PlayFlipCorrect(System.Action onCompleted = null)
        {
            _parentTransform.transform.DOScale(1.2f, animTimeScale * 10 / 60f).onComplete += () =>
            {
                _parentTransform.transform.DOScale(1f, animTimeScale * 10 / 60f).onComplete += () =>
                {
                    _parentTransform.transform.DOScaleX(0, animTimeScale * 10 / 60f).onComplete += () =>
                    {
                        SetDone();
                        _parentTransform.transform.DOScaleX(1, animTimeScale * 10 / 60f).onComplete += () =>
                        {
                            onCompleted?.Invoke();
                        };
                    };
                };
            };
        }

        public void ClickCard()
        {
            Debug.Log("Memory Card: ClickCard" + index);
            OnCardClick?.Invoke(index);
        }
    }
}