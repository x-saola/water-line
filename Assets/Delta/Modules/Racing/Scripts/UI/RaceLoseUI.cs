using Delta.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.Racing
{
    public class RaceLoseUI : UIController
    {
        [SerializeField] 
        private Button _closeButton;

        public event System.Action OnClosed;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(HandleCloseButtonClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }

        private void HandleCloseButtonClicked()
        {
            OnClosed?.Invoke();
            UIManager.Instance.ReleaseUI(this, true);
            RaceSystem.Instance.HideAllUI();
        }
    }
}