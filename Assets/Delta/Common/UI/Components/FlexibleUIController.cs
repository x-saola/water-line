using UnityEngine;
using Delta.Common.UI;
using System;

namespace Delta.Common
{
    [RequireComponent(typeof(Canvas))]
    public class FlexibleUIController : UIController, ILayerHandler, IShowHandler, IHideHandler, IFadeHandler
    {
        public Canvas Canvas { get { return _canvas; } }
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        public void ChangeLayer(string layer, int sortingOrder = -1)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingLayerName = layer;
                canvas.sortingOrder = sortingOrder > -1 ? sortingOrder : canvas.sortingOrder;
            }
        }

        public void ChangeSortingOrder(int sortingOrder)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (!canvas.overrideSorting)
            {
                canvas.overrideSorting = true;
            }
            canvas.sortingOrder = sortingOrder > -1 ? sortingOrder : canvas.sortingOrder;
        }

        #region ANIMATIONS
        public Action onShowStarted
        {
            get { return _onShowStarted; }
            set { _onShowStarted = value; }
        }
        private Action _onShowStarted;

        public Action onShowFinished
        {
            get { return _onShowFinished; }
            set { _onShowFinished = value; }
        }
        private Action _onShowFinished;

        public Action onHideStarted
        {
            get { return _onHideStarted; }
            set { _onHideStarted = value; }
        }
        private Action _onHideStarted;

        public Action onHideFinished
        {
            get { return _onHideFinished; }
            set { _onHideFinished = value; }
        }
        private Action _onHideFinished;

        public void DoShow()
        {
            Show();
        }
        public virtual void Show() { }

        public void DoHide()
        {
            Hide();
        }
        public virtual void Hide() { }

        public Action onFadeOutStarted
        {
            get { return _onFadeOutStarted; }
            set { _onFadeOutStarted = value; }
        }
        private Action _onFadeOutStarted;

        public Action onFadeOutFinished
        {
            get { return _onFadeOutFinished; }
            set { _onFadeOutFinished = value; }
        }
        private Action _onFadeOutFinished;

        public Action onFadeInStarted
        {
            get { return _onFadeInStarted; }
            set { _onFadeInStarted = value; }
        }
        private Action _onFadeInStarted;

        public Action onFadeInFinished
        {
            get { return _onFadeInFinished; }
            set { _onFadeInFinished = value; }
        }
        private Action _onFadeInFinished;

        public void DoFadeOut()
        {
            FadeOut();
        }
        public virtual void FadeOut() { }

        public void DoFadeIn()
        {
            FadeIn();
        }
        public virtual void FadeIn() { }
        #endregion
    }
}