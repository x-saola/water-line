using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.ConnectMaster
{
    public class RaceTreasureComponent : MonoBehaviour
    {
        [SerializeField] private Image _lockImage;
        [SerializeField] private Image _unlockImage;

        public void SetLocked(bool isLocked)
        {
            if (_lockImage != null)
            {
                _lockImage.gameObject.SetActive(isLocked);
            }

            if (_unlockImage != null)
            {
                _unlockImage.gameObject.SetActive(!isLocked);
            }
        }
    }
}
