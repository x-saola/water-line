using UnityEngine;
using System.Collections.Generic;

namespace Delta.Common.UI
{
    public interface IActiveUIListener
    {
        void OnStartUI(UIController controller);
        void OnActiveUI(UIController controller);
        void OnDeactiveUI(UIController controller);
        void OnRemoveUI(UIController controller);
    }

    public interface IRefreshUIListener
    {
        void OnRefreshUI(UIController controller);
    }

    public class UIController : MonoBehaviour
    {
        [SerializeField]
        private bool _ignoredBackKey;
        public bool IgnoredBackKey { get { return _ignoredBackKey; } }

        public int UILayer { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsStarted { get; private set; }

        private List<IActiveUIListener> ActiveListeners;
        private List<IRefreshUIListener> RefreshListeners;

        public void RegisterActiveListener(IActiveUIListener listener)
        {
            if (ActiveListeners == null)
            {
                ActiveListeners = new List<IActiveUIListener>();
            }
            ActiveListeners.Add(listener);

            if (IsActive)
            {
                listener.OnActiveUI(this);
            }
        }

        public void UnregisterActiveListener(IActiveUIListener listener)
        {
            if (ActiveListeners == null)
                return;
            ActiveListeners.Remove(listener);
        }

        public void RegisterRefreshListener(IRefreshUIListener listener)
        {
            if (RefreshListeners == null)
            {
                RefreshListeners = new List<IRefreshUIListener>();
            }
            RefreshListeners.Add(listener);
        }

        public void UnregisterRefreshListener(IRefreshUIListener listener)
        {
            if (RefreshListeners == null)
                return;
            RefreshListeners.Remove(listener);
        }

        public void UIActive(int layer)
        {
            UILayer = layer;

            if (!IsActive)
            {
                OnUIActive();
                IsActive = true;

                if (ActiveListeners != null)
                {
                    foreach (var listener in ActiveListeners)
                    {
                        listener.OnActiveUI(this);
                    }
                }
            }
        }

        public void UIStart()
        {
            OnUIStart();
            IsStarted = true;

            if (ActiveListeners != null)
            {
                for (int i = ActiveListeners.Count - 1; i >= 0; i--)
                {
                    ActiveListeners[i].OnStartUI(this);
                }
            }
        }

        public void UIDeactive()
        {
            IsActive = false;
            OnUIDeactive();

            if (ActiveListeners != null)
            {
                for (int i = ActiveListeners.Count - 1; i >= 0; i--)
                {
                    ActiveListeners[i].OnDeactiveUI(this);
                }
            }
        }

        public void UIRefresh()
        {
            OnUIRefresh();

            if (RefreshListeners != null)
            {
                for (int i = RefreshListeners.Count - 1; i >= 0; i--)
                {
                    RefreshListeners[i].OnRefreshUI(this);
                }
            }
        }

        public void Back()
        {
            OnBack();
        }

        public void UIRemoved()
        {
            if (ActiveListeners != null)
            {
                for (int i = ActiveListeners.Count - 1; i >= 0; i--)
                {
                    ActiveListeners[i].OnRemoveUI(this);
                }

                ActiveListeners.Clear();
            }

            if (RefreshListeners != null)
            {
                RefreshListeners.Clear();
            }

            OnUIRemoved();

            IsActive = false;
            IsStarted = false;
        }

        protected virtual void OnUIStart()
        {

        }

        protected virtual void OnUIActive()
        {

        }

        protected virtual void OnUIDeactive()
        {

        }

        protected virtual void OnUIRefresh()
        {

        }

        protected virtual void OnBack()
        {

        }

        protected virtual void OnUIRemoved()
        {

        }
    }

}