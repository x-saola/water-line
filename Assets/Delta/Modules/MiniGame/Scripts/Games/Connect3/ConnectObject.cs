using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{

    public class ConnectObject : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Action<ConnectObject> OnSelected;
        public Action<ConnectObject> OnClickDown;
        public Action OnClickUp;
        public Vector2Int Index = new Vector2Int(0, 0);
        public int Color = 0;
        public int Item = 0;
        public int Type = 0;

        [Space] [SerializeField] private Image _imgColor;
        [SerializeField] private GameObject _img;
        [SerializeField] private Image _itemImg;
        [SerializeField] private Image _hightlightFrameImg;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private List<Sprite> _itemIcons;

        [Space] [SerializeField] private GameObject _vfx;

        public void SetUp(Vector2Int index, int color, int item)
        {
            Index = index;
            Color = color;
            Item = item;
            Type = 0;
            SetSprite();
        }

        public void SetSprite()
        {
            switch (Color)
            {
                case 0: _imgColor.color = new Color(0.294f, 0.624f, 0.945f, 1); break; // blue
                case 1: _imgColor.color = new Color(0.945f, 0.294f, 0.29f, 1); break; // red
                case 2: _imgColor.color = new Color(0.945f, 0.831f, 0.294f, 1); break; // yellow
                case 3: _imgColor.color = new Color(0.945f, 0.463f, 0.294f, 1); break; // orange
                case 4: _imgColor.color = new Color(0.506f, 0.506f, 0.506f, 1); break; // grey
                case 5: _imgColor.color = new Color(0.294f, 0.847f, 0.945f, 1); break; // skyblue
                case 6: _imgColor.color = new Color(0.945f, 0.294f, 0.937f, 1); break; // pink
                case 7: _imgColor.color = new Color(0.612f, 0.431f, 0.384f, 1); break; // brown
                case 8: _imgColor.color = new Color(0.337f, 0.949f, 0.741f, 1); break; // brown
            }

            _itemImg.sprite = _itemIcons[Item];
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnSelected?.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnClickDown?.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnClickUp?.Invoke();
        }

        public void Reset()
        {
            _img.SetActive(false);
            Color = -1;
            Type = -1;
            GetComponent<Image>().enabled = false;
        }

        public void SelectVFX()
        {
            var vfx = Instantiate(_vfx, Vector3.zero, Quaternion.identity, this.transform) as GameObject;
            vfx.GetComponent<RectTransform>().position = this.GetComponent<RectTransform>().position;
        }

        public void SetData(int type, int color, int item)
        {
            Type = type;
            Color = color;
            Item = item;
            GetComponent<Image>().enabled = true;
            _img.SetActive(true);
            SetSprite();
        }

        public void Highlight(bool highlight = true)
        {
            //_canvasGroup.alpha = 0.8f;
            _hightlightFrameImg.enabled = highlight;
            // _frameImg.enabled = !highlight;
        }

        public void ScaleDown(float duration)
        {
            DOTween.Sequence()
                .Append(transform.DOScale(0, duration)).onComplete += () =>
            {
                transform.localScale = new Vector3(1, 1, 1);
                Reset();
                SelectVFX();
            };
        }
    }
}