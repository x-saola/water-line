using System;

namespace Delta.Modules.MiniGame
{
    public interface IMiniGameController
    {
        void StartGame();
        void StopGame();

        event Action<int> OnMiniGameOver;
        event Action<int> OnClaimMoreReward;
        event Action OnCloseMiniGameUI;
    }
}
