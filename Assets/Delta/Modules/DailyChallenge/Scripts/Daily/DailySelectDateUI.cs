
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Delta.Modules.DailyChallenge_VerB
{
    public class DailyDateData
    {
        public System.DateTime Date;
        public bool IsSelected;
        public bool IsCompleted;
        public bool IsAvailable;
        public bool IsDone;
        public bool IsToday;
        public bool IsSkipAds;
    }

    public class DailySelectDateUI : MonoBehaviour
    {
        [SerializeField]
        private Image _selectedImage;

        [SerializeField]
        private Image _checkImage;

        [SerializeField]
        private Image _bgImage;
        
        [SerializeField]
        private Image _adsImage;

        [SerializeField]
        private TextMeshProUGUI _dayText;
        [SerializeField]
        private TextMeshProUGUI _disableDayText;
        [SerializeField]
        private Sprite _bgSprite, _bgGreySprite,_bgOrangeSprite;

        public System.Action<DailySelectDateUI> OnClickedFunc
        {
            get { return _onClickedFunc; }
            set { _onClickedFunc = value; }
        }
        private System.Action<DailySelectDateUI> _onClickedFunc = null;

        private DailyDateData _data;
        public DailyDateData Data  => _data; 
        
        public void Setup(DailyDateData data)
        {
            _data = data;
            _dayText.text = _data.Date.Day.ToString();
            _disableDayText.text = _data.Date.Day.ToString();
            _selectedImage.gameObject.SetActive(_data.IsSelected);
            
            _checkImage.gameObject.SetActive(_data.IsCompleted);
            _adsImage.gameObject.SetActive(!_data.IsCompleted);
            if (!_data.IsAvailable)
            {
                _checkImage.gameObject.SetActive(false);
                _adsImage.gameObject.SetActive(false);
            }
            
            if (_data.IsSkipAds ||_data.IsToday)
            {
                _adsImage.gameObject.SetActive(false);
            }
            if (_data.IsDone)
            {
                _adsImage.gameObject.SetActive(false);
                _checkImage.gameObject.SetActive(true);
            }
            _bgImage.sprite = _data.IsAvailable ? _bgSprite : _bgGreySprite;
          
            _dayText.gameObject.SetActive(_data.IsAvailable);
            _disableDayText.gameObject.SetActive(!_data.IsAvailable);
            
            
            if (_data.IsToday)
            {
                _bgImage.sprite = _bgOrangeSprite;
                _disableDayText.gameObject.SetActive(true);
                _disableDayText.gameObject.SetActive(false);
            }
        }

        public System.DateTime GetDate()
        {
            return _data.Date;
        }

        public void SetSelect(bool isSelected)
        {
            _data.IsSelected = isSelected;
            _selectedImage.gameObject.SetActive(_data.IsSelected);
        }

        public void SetTextColor(Color color)
        {
            _dayText.color = color;
        }

        public void SetTextColorAlpha(float alpha)
        {
            _dayText.color = new Color(_dayText.color.r, _dayText.color.g, _dayText.color.b, alpha);
        }

        public void SetStarEnable(bool enable)
        {
            _data.IsCompleted = enable;
            _checkImage.gameObject.SetActive(_data.IsCompleted);
        }

        public void OnClicked()
        {
            if (_onClickedFunc != null)
            {
                _onClickedFunc(this);
            }
        }

        public Vector3 GetStarIconWorldPos()
        {
            return _checkImage.transform.position;
        }
    }
}