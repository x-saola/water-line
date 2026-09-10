using System;
using System.Collections;
using System.Collections.Generic;
using Delta.Common.UI;
using Delta.GameOps;
using Delta;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public abstract class BaseMiniGameController : MonoBehaviour, IMiniGameController
    {
        public event Action<int> OnMiniGameOver;
        public event Action<int> OnClaimMoreReward;
        public event Action OnCloseMiniGameUI;

        public abstract void StartGame();
        public abstract void StopGame();


        protected int _rewardCoin;
        protected WinMiniGameUI _winMiniGameUI;

        protected void ShowGameOverUI()
        {
            OnMiniGameOver?.Invoke(_rewardCoin);

            _winMiniGameUI = UIManager.Instance.ShowUIOnTop<WinMiniGameUI>("WinMiniGameUI");
            _winMiniGameUI.SetRewardCoinText(_rewardCoin);
            _winMiniGameUI.OnClickCollectButton += () =>
            {
                OnCloseMiniGameUI?.Invoke();
            };

            if (_rewardCoin > 0)
            {
                _winMiniGameUI.ShowX2Button(true);
                _winMiniGameUI.OnClickCollectX2Button += () =>
                {
                    // GameAdManager.Instance.ShowRewardedAd("MINI_GAME", (isRewarded) =>
                    // {
                    //     if (!isRewarded) return;
                    //
                    //     OnClaimMoreReward?.Invoke(_rewardCoin);
                    //     _rewardCoin *= 2;
                    //
                    //     OnCloseMiniGameUI?.Invoke();
                    // });
                };
            }
            else
            {
                _winMiniGameUI.ShowX2Button(false);
            }
        }
    }
}
