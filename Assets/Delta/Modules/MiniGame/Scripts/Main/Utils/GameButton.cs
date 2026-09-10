using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    [RequireComponent(typeof(CanvasRenderer))] 
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class GameButton : MonoBehaviour, IGameButton
    {
        protected CanvasGroup _cavasGroup;
        protected bool _isEntered = false;
        protected bool _isPressed = false;
        protected bool _interactable = true;
        [SerializeField]protected bool _isUsingAlpha = false;

        public event System.Action OnClick;
        public bool Interactable
        {
            get
            {
                return _interactable;
            }

            set
            {
                _interactable = value;
                if(_isUsingAlpha && _cavasGroup !=null)
                {
                    _cavasGroup.alpha = value ? 1 : 0.2f;
                }
            }
        }

        protected virtual void Awake()
        {
            _cavasGroup = GetComponent<CanvasGroup>();
            if (_cavasGroup == null)
            {
                _cavasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void AddListener(System.Action listener)
        {
            OnClick += listener;
        }

        public void RemoveListener(System.Action listener)
        {
            OnClick -= listener;
        }

        public void SetAlpha(float alpha)
        {
            _cavasGroup = GetComponent<CanvasGroup>();
            if (_cavasGroup == null)
            {
                _cavasGroup = gameObject.AddComponent<CanvasGroup>();
                _cavasGroup.interactable = true;
            }

            _cavasGroup.alpha = alpha;
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            _isEntered = true;
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            _isEntered = false;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            _isPressed = true;
            transform.localScale = Vector3.one * 0.95f;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            _isPressed = false;
            transform.localScale = Vector3.one;

            if (_isEntered)
            {
                OnClick?.Invoke();
            }
        }
    }

}
