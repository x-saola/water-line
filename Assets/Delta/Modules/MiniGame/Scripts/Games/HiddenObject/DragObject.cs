using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    public class DragObject : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IEndDragHandler, IDragHandler
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private bool isKeyObject = false;
        private bool isDone = false;
        private bool isReadyToFind = false;
        private int keyIndex = -1;
        public string Idvalue = "-1";
        public Action<int> OnKeyObjectComplete;
        public float canvasScale = 1;
        public RectTransformData originalRectData;
        public bool isStopDragging = false;
        
        [SerializeField] public Image Avatar;


        public struct RectTransformData
        {
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 pivot;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
        }

        public void SetUp(bool isKey,float scale, int index = -1)
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            isKeyObject = isKey;
            canvasScale = scale;
            if (isKeyObject)
            {
                keyIndex = index;
            }
        }
        
        public void LoadAvatar(string id)
        {
            Idvalue = id;
            //Todo: FindWay to set image here
            //Avatar.SetAvatar(id);

            SaveOriginalRectState();
        }
        
        void SaveOriginalRectState()
        {
            originalRectData.anchoredPosition = _rectTransform.anchoredPosition;
            originalRectData.sizeDelta = _rectTransform.sizeDelta;
            originalRectData.pivot = _rectTransform.pivot;
            originalRectData.anchorMin = _rectTransform.anchorMin;
            originalRectData.anchorMax = _rectTransform.anchorMax;
        }
        public void ResetToOriginalRectState()
        {
            Debug.Log("HiddenObject Debug :"+originalRectData.anchoredPosition);
            _rectTransform.anchoredPosition = originalRectData.anchoredPosition;
            _rectTransform.sizeDelta = originalRectData.sizeDelta;
            _rectTransform.pivot = originalRectData.pivot;
            _rectTransform.anchorMin = originalRectData.anchorMin;
            _rectTransform.anchorMax = originalRectData.anchorMax;
        }

        public void FindThisObject()
        {
            isReadyToFind = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDone && !isStopDragging)
            {
                   var parent = transform.parent;
                   transform.SetParent(transform.parent.parent);
                   transform.SetParent(parent);


                   _rectTransform.anchoredPosition += eventData.delta/canvasScale;
            }
        }

        public bool CheckComplete()
        {
                 if (isKeyObject && isReadyToFind)
                 {
                     OnKeyObjectComplete?.Invoke(keyIndex);
                     isDone = true;
                     transform.gameObject.SetActive(false);
                     return true;
                 }
                 return false;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 0.5f;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            if (!isDone && isStopDragging)
            {
                isStopDragging = false;
            }
        }
    }
}