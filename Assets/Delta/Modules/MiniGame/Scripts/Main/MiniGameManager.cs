using System;
using System.Collections.Generic;
using Delta.Common.UI;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public enum MiniGameType
    {
        None,
        MemoryCard,
        HiddenObject,
        Connect3
    }

    public class MiniGameManager : MonoBehaviour
    {
        private static MiniGameManager _instance;
        public static MiniGameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new("MiniGameManager");
                    obj.AddComponent<MiniGameManager>();
                }

                return _instance;
            }
        }

        private Dictionary<MiniGameType, Func<BaseMiniGameController>> _miniGameFactory = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _miniGameFactory = new()
            {
                // { MiniGameType.MemoryCard, () => new GameObject("MemoryCardController").AddComponent<MemoryCardsController>()},
                // { MiniGameType.HiddenObject, () => new GameObject("HiddenObjectController").AddComponent<HiddenObjectController>()},
                // { MiniGameType.Connect3, () => new GameObject("Connect3Controller").AddComponent<Connect3Controller>()},

            };
        }

        private BaseMiniGameController _currentMiniGameController;

        public void StartMiniGame(MiniGameType miniGameType, Action<int> onGameOver, Action<int> onClaimDoubleReward, Action onEnd)
        {
            if (_currentMiniGameController != null)
            {
                return;
            }

            if (_miniGameFactory.TryGetValue(miniGameType, out Func<BaseMiniGameController> miniGameCreator))
            {
                _currentMiniGameController = miniGameCreator();
                _currentMiniGameController.OnMiniGameOver += onGameOver;
                _currentMiniGameController.OnClaimMoreReward += onClaimDoubleReward;
                _currentMiniGameController.OnCloseMiniGameUI += onEnd;
                _currentMiniGameController.StartGame();
            }
            else
            {
                Debug.LogError($"No mini game controller found for type: {miniGameType}");
            }
        }

        public void StopMiniGame()
        {
            if (_currentMiniGameController != null)
            {
                _currentMiniGameController.StopGame();
                Destroy(_currentMiniGameController.gameObject);
                _currentMiniGameController = null;
            }
            else
            {
                Debug.LogError("No mini game controller to stop.");
            }
        }

        public void ShowAskMiniGameUI(MiniGameType miniGameType, System.Action onPlay)
        {
            var path = "";
            path = miniGameType switch
            {
                MiniGameType.MemoryCard => "AskMiniGameMemoryCardUI",
                MiniGameType.HiddenObject => "AskMiniGameHiddenObjectUI",
                MiniGameType.Connect3 => "AskMiniGameConnect3UI",
                _ => "AskMiniGameMemoryCardUI",
            };

            var askMiniGameUI = UIManager.Instance.ShowUIOnTop<AskMiniGameUI>(path);
            askMiniGameUI.OnClickLaterButton += () =>
            {
                UIManager.Instance.ReleaseUI(askMiniGameUI, true);
            };

            askMiniGameUI.OnClickPlayButton += () =>
            {
                onPlay?.Invoke();
                UIManager.Instance.ReleaseUI(askMiniGameUI, true);
            };
        }
        

    }
}
