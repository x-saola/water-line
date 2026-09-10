using Delta.Common.UI;
using TMPro;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public class Connect3UI : UIController
    {
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [Space]
        [SerializeField] private Transform _connectObjectsParent;
        [SerializeField] private Transform _linesParent;

        public Transform ConnectObjectsParent => _connectObjectsParent;
        public Transform linesParent => _linesParent;
        
        public void SetTimerText(int seconds)
        {
            var minute = seconds / 60;
            var second = seconds % 60;

            _timerText.text = $"{minute:00}:{second:00}";
        }
        public void SetScoreText(int currentScore, int maxScore)
        {
            _scoreText.text = $"{currentScore}/{maxScore}";
        }
    }
}