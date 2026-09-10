using UnityEngine;

namespace Delta.Services
{
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Config/Ad Config")]
    public class AdConfigSO : ScriptableObject
    {
        [Tooltip("Banner shows when entering a level >= this number (1-based, inclusive)")]
        [SerializeField] private int _bannerShowFromLevel = 1;

        [Tooltip("Interstitial is eligible after completing a level >= this number (1-based, inclusive)")]
        [SerializeField] private int _interstitialShowFromLevel = 5;

        [Tooltip("Minimum seconds that must pass between two consecutive interstitial ads")]
        [SerializeField] private int _interstitialIntervalSeconds = 30;

        public int BannerShowFromLevel => _bannerShowFromLevel;
        public int InterstitialShowFromLevel => _interstitialShowFromLevel;
        public int InterstitialIntervalSeconds => _interstitialIntervalSeconds;
    }
}
