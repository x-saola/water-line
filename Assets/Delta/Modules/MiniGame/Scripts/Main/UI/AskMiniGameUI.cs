using Delta.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    public class AskMiniGameUI : BounceAppearUI
    {
        [Space]
        [SerializeField] RawImage _miniGameIcon;
        [SerializeField] GameButton _laterButton;
        [SerializeField] GameButton _playButton;

        public event System.Action OnClickLaterButton;
        public event System.Action OnClickPlayButton;

        private void Awake()
        {
            _laterButton.AddListener(() => OnClickLaterButton?.Invoke());
            _playButton.AddListener(() => OnClickPlayButton?.Invoke());
        }

        public void SetMiniGameIcon(Texture texture)
        {
            _miniGameIcon.texture = texture;
            _miniGameIcon.SetNativeSize();
        }
    }
}