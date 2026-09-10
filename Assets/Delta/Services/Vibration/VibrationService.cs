using UnityEngine;
using MoreMountains.NiceVibrations;

namespace Delta.Services
{
    public class VibrationService : IVibrationService
    {
        private const string KEY_VIBRATION = "KEY_VIBRATION";
        private bool _isOpen = true;

        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                _isOpen = value;
                Save(KEY_VIBRATION, _isOpen);
            }
        }

        public VibrationService()
        {
            _isOpen = Load(KEY_VIBRATION, true);
        }

        private void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private bool Load(string key, bool defaultValue)
        {
            var value = defaultValue;
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key) != 0;
            }
            return value;
        }

        public void LightImpact()
        {
            if (!IsOpen)
            {
                return;
            }

            MMVibrationManager.Haptic(HapticTypes.LightImpact);

            Debug.Log("LightImpact");
        }

        public void MediumImpact()
        {
            if (!IsOpen)
            {
                return;
            }
            
            MMVibrationManager.Haptic(HapticTypes.MediumImpact);
        }

        public void HeavyImpact()
        {
            if (!IsOpen)
            {
                return;
            }
            
            MMVibrationManager.Haptic(HapticTypes.HeavyImpact);
        }
    }
}
