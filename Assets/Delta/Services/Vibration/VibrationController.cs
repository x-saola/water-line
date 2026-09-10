using UnityEngine;
using MoreMountains.NiceVibrations;

namespace Delta.Services
{
    public class VibrationController : MonoBehaviour
    {
        public static VibrationController instance { get; private set; }

        public bool vibrate { get; set; }

        private void Awake()
        {
            instance = this;

            MMVibrationManager.iOSInitializeHaptics();
        }

        private void OnDestroy()
        {
			MMVibrationManager.iOSReleaseHaptics();
        }

        public void Impact()
        {
            if (!vibrate) { return; }
            MMVibrationManager.Haptic(HapticTypes.LightImpact);
        }
    }
}
