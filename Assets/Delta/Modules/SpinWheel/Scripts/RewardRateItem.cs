using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardRateItem : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private TextMeshProUGUI _rateText;

    public void Setup(Sprite icon, int amount, float rate)
    {
        _icon.sprite = icon;
        _amountText.text = amount.ToString();
        _rateText.text = $"{rate}%";
    }
}
