using Delta.Common.UI;
using TMPro;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public class HiddenObjectUI : UIController
    {
        [Space]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Transform _parentContent;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timerText;

        [Space]
        [SerializeField] private DragSlot _dragSlot;
        
        public Canvas MainCanvas => _mainCanvas;
        public Transform ParentContent => _parentContent;
        public DragSlot DragSlot => _dragSlot;

        public System.Action<int> OnUIQuit;
        
        public void SetScoreText(int currentScore, int maxScore)
        {
            _scoreText.text = $"{currentScore}/{maxScore}";
        }

        public void SetTimerText(int seconds)
        {
            var minute = seconds / 60;
            var second = seconds % 60;

            _timerText.text = $"{minute:00}:{second:00}";
        }
    }
}