using UnityEngine;
using UnityEngine.UI;

namespace Delta.ConnectMaster
{
    public class MilestoneBox : MonoBehaviour
    {
        [SerializeField] private Image _bgLocked;
        [SerializeField] private Image _bgUnlocked;
        [SerializeField] private Image _iconLocked;
        [SerializeField] private Image _iconUnlocked;

        private bool _isLocked;

        public bool IsLocked => _isLocked;

        public void SetLockedState(bool isLocked)
        {
            _isLocked = isLocked;
            
            _bgLocked.gameObject.SetActive(isLocked);
            _iconLocked.gameObject.SetActive(isLocked);
            _bgUnlocked.gameObject.SetActive(!isLocked);
            _iconUnlocked.gameObject.SetActive(!isLocked);
        }
    }
}
