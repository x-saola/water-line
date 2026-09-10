using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Delta.Modules.Racing
{
    public class RaceCompetitorView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private GameObject _rank1Badge; // Gold
        [SerializeField] private GameObject _rank2Badge; // Silver
        [SerializeField] private GameObject _rank3Badge; // Bronze
        [SerializeField] private GameObject _winnerIndicator;

        private OpponentProfile _opponent;
        
        public OpponentProfile Opponent => _opponent;

        public void Initialize(OpponentProfile opponent, int maxObjectives, Sprite avatarSprite)
        {
            _opponent = opponent;

            _nameText.text = opponent.name;
            _progressBar.maxValue = maxObjectives;
            _progressBar.value = opponent.progress;
            _progressText.text = $"{opponent.progress}";

            // Set avatar
            if (_avatarImage != null)
            {
                _avatarImage.sprite = avatarSprite;
            }

            if (_winnerIndicator != null)
            {
                _winnerIndicator.SetActive(false);
            }

            // Hide all rank badges initially
            if (_rank1Badge != null) _rank1Badge.SetActive(false);
            if (_rank2Badge != null) _rank2Badge.SetActive(false);
            if (_rank3Badge != null) _rank3Badge.SetActive(false);
        }

        public void UpdateProgress()
        {
            if (_opponent == null)
                return;

            _progressBar.value = _opponent.progress;
            _progressText.text = $"{_opponent.progress}";

            // Show winner indicator if completed
            if (_winnerIndicator != null && _opponent.HasWon())
            {
                _winnerIndicator.SetActive(true);
            }
        }

        public void SetRank(int rank)
        {
            // Hide all badges first
            if (_rank1Badge != null) _rank1Badge.SetActive(false);
            if (_rank2Badge != null) _rank2Badge.SetActive(false);
            if (_rank3Badge != null) _rank3Badge.SetActive(false);

            // Show the appropriate badge based on rank
            if (rank == 1 && _rank1Badge != null)
            {
                _rank1Badge.SetActive(true);
            }
            else if (rank == 2 && _rank2Badge != null)
            {
                _rank2Badge.SetActive(true);
            }
            else if (rank == 3 && _rank3Badge != null)
            {
                _rank3Badge.SetActive(true);
            }
        }

        public void PlayProgressAnimation()
        {
            // Could add visual feedback when progress increases
            // e.g., flash, scale animation, particle effect
        }
    }
}
