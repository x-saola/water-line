using System;
using UnityEngine;

namespace Delta.Modules.Utilities
{
    public class TimeManager : MonoBehaviour
    {
        private const string PREF_LAST_EXIT_TIME = "TimeManager_LastExitTime";

        private static TimeManager _instance;
        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("TimeManager");
                    _instance = go.AddComponent<TimeManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        [Tooltip("Time scale multiplier")]
        [SerializeField] private float _timeScale = 1f;
        [Tooltip("Tick interval in seconds")]
        [SerializeField] private float _tickInterval = 1f;

        // Event
        public event System.Action OnTick;
        public event System.Action OnSecondPassed;

        public double OfflineSeconds { get; private set; }
        public double OnlineSeconds { get; private set; }

        private float _secondTimer;
        private float _tickTimer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            UpdateOfflineTime();
            OnlineSeconds = 0;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime * _timeScale;

            OnlineSeconds += deltaTime;

            // Handle Second Passed
            _secondTimer += deltaTime;
            if (_secondTimer >= 1f)
            {
                _secondTimer -= 1f;
                OnSecondPassed?.Invoke();
            }

            // Handle Tick
            if (_tickInterval > 0)
            {
                _tickTimer += deltaTime;
                if (_tickTimer >= _tickInterval)
                {
                    _tickTimer -= _tickInterval;
                    OnTick?.Invoke();
                }
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveExitTime();
            }
            else
            {
                UpdateOfflineTime();
            }
        }

        private void OnApplicationQuit()
        {
            SaveExitTime();
        }

        private void UpdateOfflineTime()
        {
            string lastExitTimeStr = PlayerPrefs.GetString(PREF_LAST_EXIT_TIME, string.Empty);
            if (!string.IsNullOrEmpty(lastExitTimeStr))
            {
                if (long.TryParse(lastExitTimeStr, out long binaryTime))
                {
                    DateTime lastExitTime = DateTime.FromBinary(binaryTime);
                    TimeSpan span = DateTime.UtcNow - lastExitTime;
                    OfflineSeconds = span.TotalSeconds;
                }
            }
            else
            {
                OfflineSeconds = 0;
            }
        }

        private void SaveExitTime()
        {
            PlayerPrefs.SetString(PREF_LAST_EXIT_TIME, DateTime.UtcNow.ToBinary().ToString());
            PlayerPrefs.Save();
        }
    }
}
