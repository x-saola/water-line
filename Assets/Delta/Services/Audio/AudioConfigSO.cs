using UnityEngine;

namespace Delta.Services
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/Audio Config")]
    public class AudioConfigSO : ScriptableObject
    {
        public AudioClipData[] MusicData => _musicClipData;
        public AudioClipData[] SfxData => _sfxClipData;

        [SerializeField] AudioClipData[] _musicClipData;
        [SerializeField] AudioClipData[] _sfxClipData;

        [System.Serializable]
        public class AudioClipData
        {
            public string AudioId;
            public AudioClip AudioClip;
        }
    }
}
