using Delta.Common.UI;
using TMPro;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public class WinMiniGameUI : BounceAppearUI
    {
        [Space]
        [SerializeField] private TMP_Text _rewardCoinText;
        [SerializeField] private GameButton _collectButton;
        [SerializeField] private GameButton _collectX2Button;

        public event System.Action OnClickCollectButton;
        public event System.Action OnClickCollectX2Button;

        protected override void Awake()
        {
            base.Awake();
            _collectButton.AddListener(() => OnClickCollectButton?.Invoke());
            _collectX2Button.AddListener(() => OnClickCollectX2Button?.Invoke());
        }

        public void SetRewardCoinText(int rewardCoin)
        {
            _rewardCoinText.text = $"+{rewardCoin}";
        }

        public void ShowX2Button(bool value)
        {
            _collectX2Button.gameObject.SetActive(value);
        }
    }
}