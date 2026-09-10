using Delta.Common.UI;
using TMPro;
using UnityEngine;
namespace Delta.Modules.MiniGame
{
 
  
        public class MiniGameTutorialUI : BounceAppearUI
        {
            [Space]
            [SerializeField] TMP_Text _tutorialText;
            [SerializeField] GameButton _confirmButton;

            public System.Action OnClickConfirmButton;

            protected override void Awake()
            {
                base.Awake();
                _confirmButton.AddListener(() => OnClickConfirmButton?.Invoke());
            }

            public void SetTutorialText(string text)
            {
                _tutorialText.text = text;
            }
        }

}