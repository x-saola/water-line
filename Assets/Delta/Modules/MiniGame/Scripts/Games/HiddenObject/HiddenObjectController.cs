using System.Collections.Generic;
using Delta.Common.UI;
using UnityEngine;
using Random = System.Random;

namespace Delta.Modules.MiniGame
{
    public sealed class HiddenObjectController : BaseMiniGameController
    {
        private HiddenObjectLevel _level;

        private HiddenObjectGameState _gameState;
        private float _remainTime;
        private int _maxScore;
        private int _score;

        public HiddenObjectGameState State => _gameState;
        
        private const string _KEY_DONE_TUTORIAL = "KEY_DONE_MINI_GAME_FIND_OBJECT";

        private int UniqueAvatarCount = 51;
        private int LevelCount = 10;
        private string prefabPath = "HiddenObjectLevel/LvlHiddenObject "; // Name or path within "Resources"

        private HiddenObjectUI _hiddenObjectUI;

        public enum HiddenObjectGameState
        {
            Setup,
            Playing,
            Over,
            Win
        }

        public override void StartGame()
        {
            _hiddenObjectUI = UIManager.Instance.ShowUIOnTop<HiddenObjectUI>("HiddenObjectMiniGameUI");

            Random random = new();
            var levelIndex = random.Next(0, LevelCount);

            _remainTime = 60;
            _maxScore = 5;
            _score = 0;

            prefabPath += levelIndex.ToString();
            GameObject loadedPrefab = Resources.Load<GameObject>(prefabPath);
            if (loadedPrefab != null)
            {
                _level = Instantiate(loadedPrefab, Vector3.zero, Quaternion.identity).GetComponent<HiddenObjectLevel>();
                _level.transform.SetParent(_hiddenObjectUI.ParentContent);
                _level.transform.position = Vector3.zero;
                _level.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("HiddenObject: Level + " + levelIndex + " not found");
                return;
            }
            _level.SetUp(_maxScore, _hiddenObjectUI.MainCanvas.scaleFactor);
            _level.OnObjectCompleted = CompleteObject;

            LoadAvatar();
            _hiddenObjectUI.DragSlot.LoadAvatar(_level.Keys[0].Idvalue);

            var doneTutorial = PlayerPrefs.GetInt(_KEY_DONE_TUTORIAL, 0) == 1;
            if (doneTutorial)
            {
                _gameState = HiddenObjectGameState.Playing;
            }
            else
            {
                var tutorialUI = UIManager.Instance.ShowUIOnTop<MiniGameTutorialUI>("MiniGameTutorialUI");
                tutorialUI.SetTutorialText("Your goal is to drag as many designated people to the required area as possible within the time limit. People might be hidden under.");
                tutorialUI.OnClickConfirmButton += () =>
                {
                    PlayerPrefs.SetInt(_KEY_DONE_TUTORIAL, 1);

                    _gameState = HiddenObjectGameState.Playing;
                    UIManager.Instance.ReleaseUI(tutorialUI, true);
                };
            }
        }

        public override void StopGame()
        {
            UIManager.Instance.ReleaseUI(_hiddenObjectUI, true);
            UIManager.Instance.ReleaseUI(_winMiniGameUI, true);
        }

        private void LoadAvatar()
        {

            List<int> avatarValues = new List<int>();
            for (int i = 0; i < UniqueAvatarCount; i++)
            {
                avatarValues.Add(i);
            }
            // Shuffle the values
            System.Random rand = new System.Random();
            for (int i = avatarValues.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (avatarValues[i], avatarValues[j]) = (avatarValues[j], avatarValues[i]);
            }

            int valueIndex = 0;
            int index = 0;
            while (valueIndex < UniqueAvatarCount && index < _level.DragObjects.Count)
            {
                if (_level.DragObjects[index].Avatar != null)
                {
                    _level.DragObjects[index].LoadAvatar( avatarValues[valueIndex].ToString());
                    valueIndex++;
                    index++;
                }
                else
                {
                    index++;
                }
            }

        }

        private void Update()
        {
            if (_gameState == HiddenObjectGameState.Playing)
            {
                _remainTime -= Time.deltaTime;

                _hiddenObjectUI.SetTimerText(Mathf.CeilToInt(_remainTime));

                if (_remainTime <= 0)
                {
                    _gameState = HiddenObjectGameState.Over;
                    HandleGameOver(false);
                }
            }
        }

        private void CompleteObject(int index)
        {
            _score++;
            _hiddenObjectUI.SetScoreText(_score, _maxScore);

            if (_score >= _maxScore)
            {
                _gameState = HiddenObjectGameState.Win;
                HandleGameOver(true);
            }
            else
            {
                _level.Keys[_score].FindThisObject();
                _hiddenObjectUI.DragSlot.LoadAvatar(_level.Keys[_score].Idvalue);
            }
        }

        private void HandleGameOver(bool isWin)
        {
            _hiddenObjectUI.SetTimerText(0);

            CalculateRewardCoin();
            ShowGameOverUI();
        }

        private void CalculateRewardCoin()
        {
            var maxCoinReward = 10;
            var maxScore = _maxScore;
            _rewardCoin = Mathf.CeilToInt((float)_score / maxScore * maxCoinReward);
        }
    }
}