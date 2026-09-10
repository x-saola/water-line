using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.Reward
{
    public class RewardItem : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        
        [SerializeField]
        private TextMeshProUGUI _amountText;

        public void Show(Sprite icon, int amount, bool hasX = true)
        {
            _icon.sprite = icon;
            _amountText.text = $"x{amount}";

            if (!hasX)
            {
                _amountText.text = $"{amount}";
            }
            
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
