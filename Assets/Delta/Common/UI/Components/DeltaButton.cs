using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    public class DeltaButton : Button
    {
        [SerializeField]
        private TextMeshProUGUI m_ButtonText;
        
        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
        }

        public void SetText(string text)
        {
            if (m_ButtonText != null)
            {
                m_ButtonText.text = text;
            }
        }
    }
}
