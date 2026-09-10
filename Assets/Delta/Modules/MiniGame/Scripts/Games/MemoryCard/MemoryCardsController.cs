using System.Collections.Generic;
using Delta.Common.UI;
using Delta;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public sealed class MemoryCardsController : BaseMiniGameController
    {
        public enum MemoryCardGameState
        {
            Setup,
            Playing,
            Over
        }

        [SerializeField] private int UniquePairsCount = 0;
        public List<int> CardValue;

        private MemoryCardGameState _gameState = MemoryCardGameState.Setup;
        private float _remainTime;
        private int _totalCards;
        private bool _selectable = true;
        private int _score;

        private List<MemoryCard> _cards;

        private int _firstSelectedCardIndex = -1;

        private const string _characterConfigPath = "character_mg.json";
        private const string _KEY_DONE_TUTORIAL = "KEY_DONE_MINI_GAME_MATCH_PAIR";

        private MemoryCardUI _memoryCardUI;

        public override void StartGame()
        {
            _remainTime = 60;
            _totalCards = 16;
            _score = 0;

            _memoryCardUI = UIManager.Instance.ShowUIOnTop<MemoryCardUI>("MemoryCardMiniGameUI");
            _memoryCardUI.SetPairText(_totalCards / 2 - _score);
            _memoryCardUI.SetTimerText(Mathf.CeilToInt(_remainTime));

            _cards = _memoryCardUI.Cards;
            UniquePairsCount = 51;

            CardValue = new List<int>(_totalCards);
            for (int i = 0; i < _totalCards; i++)
            {
                CardValue.Add(0);
                _cards[i].index = i;
                _cards[i].OnCardClick = SelectCard;
            }
            FillCardValues(_totalCards);

          
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].LoadAvatar(CardValue[i].ToString());
            }

            var doneTutorial = PlayerPrefs.GetInt(_KEY_DONE_TUTORIAL, 0) == 1;
            if (doneTutorial)
            {
                _gameState = MemoryCardGameState.Playing;
            }
            else
            {
                var tutorialUI = UIManager.Instance.ShowUIOnTop<MiniGameTutorialUI>("MiniGameTutorialUI");
                tutorialUI.SetTutorialText("Your goal is to find as many matching pairs as possible within the challenge time!");
                tutorialUI.OnClickConfirmButton += () =>
                {
                    PlayerPrefs.SetInt(_KEY_DONE_TUTORIAL, 1);

                    _gameState = MemoryCardGameState.Playing;
                    UIManager.Instance.ReleaseUI(tutorialUI, true);
                };
            }
        }

        public override void StopGame()
        {
            UIManager.Instance.ReleaseUI(_memoryCardUI, true);
            UIManager.Instance.ReleaseUI(_winMiniGameUI, true);
        }

        void FillCardValues(int totalElements)
        {
            if (totalElements % 2 != 0)
            {
                Debug.LogError("MiniGame: Grid size must be even to create pairs!");
                return;
            }

            int pairCount = totalElements / 2;

            List<int> values = new List<int>();

            List<int> pairValues = new List<int>();
            for (int i = 0; i < UniquePairsCount; i++)
            {
                pairValues.Add(i);
            }
            // Shuffle the values
            System.Random rand = new System.Random();
            for (int i = pairValues.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (pairValues[i], pairValues[j]) = (pairValues[j], pairValues[i]);
            }


            for (int i = 0; i < pairCount; i++)
            {
                values.Add(pairValues[i]);
                values.Add(pairValues[i]);
            }

            // Shuffle the values
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            // Fill CardValue array
            for (int i = 0; i < _totalCards; i++)
            {
                CardValue[i] = values[i];
            }
        }

        public void SelectCard(int index)
        {
            if (!_selectable || _cards[index].isDone || index == _firstSelectedCardIndex || _gameState != MemoryCardGameState.Playing)
            {
                return;
            }
            _selectable = false;

            if (_firstSelectedCardIndex == -1) // Select first card
            {
                _selectable = true;
                _firstSelectedCardIndex = index;
                _cards[_firstSelectedCardIndex].PlayFlip();
            }
            else // Select second card
            {
                var firstSelectedCardIndex = _firstSelectedCardIndex;
                var secondSelectedCardIndex = index;

                if (CardValue[firstSelectedCardIndex] == CardValue[secondSelectedCardIndex]) // Matched
                {
                    _cards[firstSelectedCardIndex].isDone = true;
                    _cards[secondSelectedCardIndex].isDone = true;
                    _score++;

                    if (_score == _totalCards / 2)
                    {
                        _gameState = MemoryCardGameState.Over;
                    }

                    _cards[secondSelectedCardIndex].PlayFlip(() =>
                    {
                        _cards[firstSelectedCardIndex].PlayFlipCorrect();
                        _cards[secondSelectedCardIndex].PlayFlipCorrect(() =>
                        {
                            _selectable = true;
                            _firstSelectedCardIndex = -1;

                            _memoryCardUI.SetPairText(_totalCards / 2 - _score);
                            if (_gameState == MemoryCardGameState.Over)
                            {
                                HandleGameOver(true);
                            }
                        });
                    });
                }
                else // Unmatched
                {
                    _cards[secondSelectedCardIndex].PlayFlip(() =>
                    {
                        _cards[firstSelectedCardIndex].PlayFlipFail();
                        _cards[secondSelectedCardIndex].PlayFlipFail(() =>
                        {
                            _selectable = true;
                            _firstSelectedCardIndex = -1;
                        });
                    });
                }
            }
        }

        private void Update()
        {
            if (_gameState == MemoryCardGameState.Playing)
            {
                _remainTime -= Time.deltaTime;

                _memoryCardUI.SetTimerText(Mathf.CeilToInt(_remainTime));

                if (_remainTime <= 0)
                {
                    _gameState = MemoryCardGameState.Over;
                    HandleGameOver(false);
                }
            }
        }

        private void HandleGameOver(bool isWin)
        {
            _memoryCardUI.SetTimerText(0);
            
            CalculateRewardCoin();
            ShowGameOverUI();
        }

        private void CalculateRewardCoin()
        {
            var maxCoinReward = 10;
            var maxScore = _totalCards / 2;
            _rewardCoin = Mathf.CeilToInt((float)_score / maxScore * maxCoinReward);
        }
    }
}
