using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ATT
{
    public class AppTrackingManager : MonoBehaviour
    {
        public Transform ParentCanvas;
        public GameObject PrefabSoftATT;
        private string ATTResultKey = "ATTResultKey";
#if UNITY_IOS && !UNITY_EDITOR
        void Awake()
        {
            try
            {
                OpenSoftATTPopup();
            }
            catch (System.Exception exc) { }
        }
        private void OpenSoftATTPopup()
        {
            bool isAlreadyGotResult = PlayerPrefs.GetInt(ATTResultKey, 0) > 0;
            if (isAlreadyGotResult) return;

            GameObject go = GameObject.Instantiate(PrefabSoftATT, Vector3.zero, Quaternion.identity, ParentCanvas);
            go.GetComponent<PopupSoftATT>()?.OpenSoftATTPopup(() =>
            {
                PlayerPrefs.SetInt(ATTResultKey, 1);
                RequestNativeATT();
            });
        }
        public void RequestNativeATT()
        {
            AppTrackingTransparency.RequestTrackingAuthorization((status) =>
        {
            if (status == AppTrackingTransparency.AuthorizationStatus.Authorized)
            {
                // Debug.Log("Authorized!");
            }
            else
            {
                // Debug.Log("Do something!");
            }
        });
        }
#endif
    }
}
