using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ATT
{
    public class PopupSoftATT : MonoBehaviour
    {
        public Button BtnPrivacy;
        public Button BtnContinue;
        public TextMeshProUGUI TxtDescription;
        private UnityAction callback = null;
        private string Description = "To use \"{0}\" you must agree to our <u>Privacy Policy</u>. We put a lot of love into this app and we hope you enjoy it!";
        void Awake()
        {
            BtnContinue.onClick.AddListener(HandleBtnContinueClick);
            BtnPrivacy.onClick.AddListener(HandleBtnPrivacyClick);
            TxtDescription.text = string.Format(Description, ATTSettings.GameName);
        }
        void OnDestroy()
        {
            BtnContinue.onClick.RemoveAllListeners();
            BtnPrivacy.onClick.RemoveAllListeners();
        }
        public void OpenSoftATTPopup(UnityAction cb = null)
        {
            callback = cb;
        }
        private void HandleBtnContinueClick()
        {
            gameObject.SetActive(false);

            if (callback != null)
            {
                callback();
            }
        }
        private void HandleBtnPrivacyClick()
        {
#if UNITY_IOS
            Application.OpenURL(ATTSettings.PrivacyLink);
#elif UNITY_ANDROID
        Application.OpenURL(ATTSettings.PrivacyLink);
#endif
        }
    }
}
