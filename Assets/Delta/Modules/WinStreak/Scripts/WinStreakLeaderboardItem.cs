using TMPro;
using UnityEngine;

namespace Delta.Modules.WinStreak
{
    public class WinStreakLeaderboardItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _streakLabel;
        

        public void SetData(LeaderboardEntry entry)
        {
            if (_rankLabel != null) _rankLabel.text = entry.rank.ToString();
            if (_nameLabel != null) _nameLabel.text = entry.name;
            if (_streakLabel != null) _streakLabel.text = entry.streak.ToString();
        }
    }
}
