# Mini Game Module



## Mini Game Type


```bash
 MemoryCard,
 HiddenObject,
 Connect3
 ````



## Usage

Add this Function to a file to spawn MiniGame UI

```csharp
private void StartMiniGame(MiniGameType miniGameType)
        {
            if (miniGameType == MiniGameType.None)
            {
                return;
            }

            MiniGameManager.Instance.StartMiniGame(miniGameType,
                (rewardCoin) => // On Game Over
                {
                    if (rewardCoin > 0)
                    {
                        //_rewardCoin += rewardCoin;

                        var currentCoinAmount = AppManager.Instance.GetSystem<SaveLoadSystem>().userData.Coin;
                        currentCoinAmount += rewardCoin;
                        //AppManager.Instance.GetSystem<SaveLoadSystem>().userData.Coin =  currentCoinAmount;
                        //TrackingManager.Instance.TrackSourceResourceEvent("Level", "Common", rewardCoin, _levelId, currentCoinAmount);
                    }
                },
                (rewardCoin) => // On Claim More
                {
                    //_rewardCoin += rewardCoin;

                    var currentCoinAmount = AppManager.Instance.GetSystem<SaveLoadSystem>().userData.Coin;
                    currentCoinAmount += rewardCoin;
                   // AppManager.Instance.GetSystem<SaveLoadSystem>().userData.Coin =  currentCoinAmount;
                    //TrackingManager.Instance.TrackSourceResourceEvent("Ads", "Common", rewardCoin, _levelId, currentCoinAmount);
                },
                () => // On Close Mini Game UI
                {
                    MiniGameManager.Instance.StopMiniGame();
                    //_winLevelUI.SetReceivedCoinAmout(_rewardCoin, true);
                });
        }
```


## Call Example
```csharp
StartMiniGame(MiniGameType.MemoryCard);
```