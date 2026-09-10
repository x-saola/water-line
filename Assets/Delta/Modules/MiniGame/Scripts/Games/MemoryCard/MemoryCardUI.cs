using System.Collections.Generic;
using Delta.Common.UI;
using TMPro;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public class MemoryCardUI : UIController
    {
        [Space]
        [SerializeField] private TMP_Text _pairCountText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private List<MemoryCard> _cards;

        public List<MemoryCard> Cards => _cards;

        public void SetPairText(int remainPair)
        {
            _pairCountText.text = remainPair.ToString();
        }

        public void SetTimerText(int seconds)
        {
            var minute = seconds / 60;
            var second = seconds % 60;

            _timerText.text = $"{minute:00}:{second:00}";
        }
    }
}