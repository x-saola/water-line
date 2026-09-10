using UnityEngine;

namespace Delta.UI
{
    public class UIGroup : MonoBehaviour
    {
        private bool _isVisible;
        protected CanvasGroup canvasGroup;
        protected bool _isCurrentScene;

        public void Switch(bool active)
        {
            if (active && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else if (!active && gameObject.activeSelf && canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }

            if (enabled != active)
            {
                enabled = active;
            }
            OnSwitch(active);
        }

        public void UpdateSceneStatus(bool visible)
        {
            _isVisible = visible;
            if (_isVisible)
            {
                _isCurrentScene = true;
                OnVisible();
            }
            else
            {
                _isCurrentScene = false;
            }
        }

        protected virtual void OnVisible()
        {

        }

        protected bool IsVisible()
        {
            return _isVisible;
        }

        protected virtual void OnSwitch(bool active)
        {

        }
    }
}

