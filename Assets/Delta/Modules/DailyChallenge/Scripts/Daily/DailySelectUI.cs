using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delta.Common.UI;
using Delta;
using JetBrains.Annotations;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace Delta.Modules.DailyChallenge_VerB
{
    public class DailySelectUI : UIController
    {
        public System.Action onDailyFlowFinished, onNoInternetExceptionTriggered;
        private const int DAILY_START_YEAR = 2023;
        
        [SerializeField]
        private RectTransform _calendarDatesContent, _calendarDateTextsContent;
        [SerializeField]
        private DailySelectDateUI _calendarDatePrefab;
        [SerializeField]
        private TextMeshProUGUI _bannerMonthText, _btnDateText, _statusDateText;
        [SerializeField]
        private GameObject _btnPlay, _btnComplete, _btnUnavailable, _adIcon, _playIcon;

        private System.DateTime _currentMonth;
        private System.DateTime _todayDate;

        private DailySelectDateUI _currentSelectedDateUI = null;
        [SerializeField] public List<DailySelectDateUI> _dateUIs;

        private int _winCount;
        
        private bool _rewardedPlayDisabled = false;

        private System.DateTime _dailyStartDate;

        public System.DateTime DailyStartDate
        {
            set
            {
                _dailyStartDate = value;
            }
        }

        public bool ShowAdsToPlay
        {
            get
            {
                return _adIcon.activeSelf;
            }
        }


        public System.Action<DateTime> ActionPlayDate;
        public System.Action OnClose;

        public System.DateTime CurrentSelectedDateUI
        {
            get
            {
                return _currentSelectedDateUI.GetDate();
            }
        }

        void Awake()
        {
            var now = System.DateTime.Today;
            _todayDate = new System.DateTime(now.Year, now.Month, now.Day);
            //_rewardedPlayDisabled = Global.RemoteConfigGetter.DAILY_REWARDED_PLAY_DISABLED;
            //_dailyStartDate = new System.DateTime(DAILY_START_YEAR, 1, 1);
            //_cheatBtn.SetActive(Global.RemoteConfigGetter.IS_CHEAT_ENABLED);
            //_isCheatIdEnable = false;
            //UpdateCheatUI();
        }

        private void OnEnable()
        {
            //ShowDailyLevelCompletedUI();
        }

        public void Setup(System.DateTime dateTime)
        {
            Debug.Log(dateTime);
            _currentMonth = new System.DateTime(dateTime.Year, dateTime.Month, 1);
          
            System.DateTime firstDate = new System.DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int deltaDay = (firstDate.DayOfWeek == System.DayOfWeek.Sunday) ? -6 : 1 - (int)firstDate.DayOfWeek;
            System.DateTime firstDateCalendar = firstDate.AddDays(deltaDay);
            System.DateTime lastDate = new System.DateTime(_currentMonth.Year, _currentMonth.Month, 1).AddMonths(1).AddDays(-1);
            deltaDay = (lastDate.DayOfWeek == System.DayOfWeek.Sunday) ? 0 : 7 - (int)lastDate.DayOfWeek;
            System.DateTime lastDateCalendar = lastDate.AddDays(deltaDay);
            System.TimeSpan calendarDays = lastDateCalendar.AddDays(1) - firstDateCalendar;
            int rowCount = (int)calendarDays.TotalDays / 7;
            Vector2 startPos = new Vector2(-333, 0);
            if (_dateUIs == null)
            {
                _dateUIs =  new List<DailySelectDateUI>();
            }
            
            bool hasSelected = false;
            _winCount = 0;
            for (int i = 0; i < calendarDays.TotalDays; i++)
            {
                System.DateTime date = firstDateCalendar.AddDays(i);
                //var isDone =   
                string dateKey = date.ToString("dd/MM/yyyy");
                PlayerPrefs.GetInt("DailyPuzzle_" + dateKey, 0);
                var isDone = PlayerPrefs.GetInt("DailyPuzzle_" + dateKey, 0) == 1;
                var isSkipAds= PlayerPrefs.GetInt("DailyPuzzleAds_" + dateKey, 0) == 1;
                if (date.Month != _currentMonth.Month)
                {
                    continue; //not use full calendar
                }

                var dateUI = new DailySelectDateUI();
                if(i>=_dateUIs.Count)
                {
                    dateUI = Instantiate(_calendarDatePrefab, _calendarDatesContent.transform).GetComponent<DailySelectDateUI>();
                    _dateUIs.Add(dateUI);
                }
                else
                {
                    dateUI = _dateUIs[i];
                }
                dateUI.gameObject.SetActive(true);
                RectTransform rt = dateUI.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector3(startPos.x + (i % 7) * (rt.sizeDelta.x + 5), startPos.y - (i / 7) * (rt.sizeDelta.y + 5));
                DailyDateData data = new DailyDateData();
                data.Date = date;
                data.IsCompleted = IsDailyComplete(date) && (date - lastDate).TotalDays <= 0 && (date - firstDate).TotalDays >= 0;
                if (data.IsCompleted)
                {
                    _winCount++;
                }
                if (hasSelected)
                {
                    data.IsSelected = false;
                }
                
                else if ((date - _todayDate).TotalDays == 0)
                {
                    data.IsSelected = true;
                    _currentSelectedDateUI = dateUI;
                    hasSelected = true;
                    data.IsToday = true;
                }
                else if ((date - lastDate).TotalDays == 0 && !hasSelected)
                {
                    _currentSelectedDateUI = dateUI;
                    data.IsSelected = true;
                }
                else
                {
                    data.IsSelected = false;
                }
                if ((date - lastDate).TotalDays > 0 || (date - _todayDate).TotalDays > 0 || (date - firstDate).TotalDays < 0)
                {
                    data.IsAvailable = false;
                }
                else
                {
                    data.IsAvailable = true;
                }
                
                data.IsSkipAds = isSkipAds;
                data.IsDone = isDone;
                
                dateUI.OnClickedFunc = OnSelectDate;

                dateUI.Setup(data);
            }
            UpdateBtnDateText();
            UpdateBannerMonthText();
            UpdatePlayButtonStatus();
            

            /*
            if (_willShowCompletePopup)
            {
                ShowDailyLevelCompletedUI();
            }
            */
        }

        public void FocusDateSelect(DateTime selectDate)
        {
            var curMonth = _currentMonth;
            var selectMonth = new DateTime(selectDate.Year, selectDate.Month, 1);
            if (selectMonth == curMonth)
            {
                Debug.Log("Same Month");
                ChoseDay(selectDate.Day);

            }
            else if(selectMonth == curMonth.AddMonths(-1))
            {
                Debug.Log("Prev Month");
                ChangeMonth(-1);
                ChoseDay(selectDate.Day);
            }
            else
            {
                return;
            }
        }

        private void ChoseDay(int day)
        {
            Debug.Log(day);
            Debug.Log(_dateUIs.Count);
            for(int i = 0; i < _dateUIs.Count; i++)
            {
                if (_dateUIs[i].GetDate().Day == day)
                {
                    Debug.unityLogger.Log("DP:SelectDate" + day );
                    _dateUIs[i].OnClickedFunc?.Invoke(_dateUIs[i]);
                    return;
                }
            }
            Debug.unityLogger.Log("DP:SelectDate:" + "no dateUI found");
        }

      

        private void ResetCalendar()
        {
            if (_dateUIs == null)
            {
                return;
            }
            for (int i = 0; i < _dateUIs.Count; i++)
            {
                if (_dateUIs[i] != null)
                {
                    _dateUIs[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateBtnDateText()
        {
            if (_currentSelectedDateUI == null)
            {
                return;
            }
            System.DateTime date = _currentSelectedDateUI.GetDate();
            _btnDateText.text = ToDateString(date, 10, 28);
            _statusDateText.text = _btnDateText.text;
        }

        private void UpdateBannerMonthText()
        {
            _bannerMonthText.text = _currentMonth.ToString("MMM yyy");
        }

        private void UpdatePlayButtonStatus()
        {
            var currentDate = _currentSelectedDateUI.GetDate();
            if (IsDailyComplete(_currentSelectedDateUI.GetDate()))
            {
                _btnComplete.SetActive(true);
                _btnPlay.SetActive(false);
                _btnUnavailable.SetActive(false);
            }
            else if ((currentDate - _todayDate).TotalDays > 0)
            {
                _btnComplete.SetActive(false);
                _btnPlay.SetActive(false);
                _btnUnavailable.SetActive(true);
            }
            else
            {
                if ((currentDate - _todayDate).TotalDays != 0 && !_rewardedPlayDisabled)
                {
                    _adIcon.SetActive(true);
                    _playIcon.SetActive(false);
                }
                else
                {
                    _adIcon.SetActive(false);
                    _playIcon.SetActive(true);
                }
                _btnComplete.SetActive(false);
                _btnPlay.SetActive(true);
                _btnUnavailable.SetActive(false);
            }

            if (_currentSelectedDateUI.Data.IsSkipAds)
            {
                _adIcon.SetActive(false);
                _playIcon.SetActive(true);
            }

            if (_currentSelectedDateUI.Data.IsDone)
            {
                _btnPlay.SetActive(false);
                _btnUnavailable.SetActive(false);
                _btnComplete.SetActive(true);
            }
        }

        

        public void OnSelectDate(DailySelectDateUI dateUI)
        {
            _currentSelectedDateUI.SetSelect(false);
            _currentSelectedDateUI = dateUI;
            _currentSelectedDateUI.SetSelect(true);
            UpdateBtnDateText();
            UpdatePlayButtonStatus();
        }

        private void ChangeMonth(int monthChange)
        {
            System.DateTime newMonth = _currentMonth.AddMonths(monthChange);
            
            if ((newMonth - _dailyStartDate).TotalSeconds < 0)
            {
                return;
            }
            
            //remove this to get all future months
            if ((newMonth - _todayDate).TotalDays > 0)
            {
                return;
            }
           

            //remove this to get all past months
            var lastmonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            if ((newMonth - lastmonth).TotalSeconds < 0)
            {
                return;
            }
            
            ResetCalendar();
            _currentMonth = newMonth;
            Setup(_currentMonth);
        }

        public void OnNextMonthClicked()
        {
            //AudioManager.Instance.PlaySFX(Common.AudioId.ButtonTap, usePlayIndex: true);
            ChangeMonth(1);
        }

        public void OnPrevMonthClicked()
        {
            //AudioManager.Instance.PlaySFX(Common.AudioId.ButtonTap, usePlayIndex: true);
            ChangeMonth(-1);
        }

        public void OnPlayClicked()
        {

            string dateKey = CurrentSelectedDateUI.Date.ToString("dd/MM/yyyy");
            if (CurrentSelectedDateUI.Date == _todayDate || PlayerPrefs.GetInt("DailyPuzzleAds_" + dateKey, 0) == 1)
            {
                ActionPlayDate?.Invoke(_currentSelectedDateUI.GetDate());
            }
            else
            {
                //Todo: Play Ads Here
                //
                // GameAdManager.Instance.ShowRewardedAd("DAILY", (isRewared) =>
                // {
                //     if (!isRewared) return;
                     PlayerPrefs.SetInt("DailyPuzzleAds_" + dateKey, 1);
                     PlayerPrefs.Save();
                     ActionPlayDate?.Invoke(_currentSelectedDateUI.GetDate());
                // });
            }
            
        }

        

        public bool IsDailyComplete(System.DateTime date)
        {
            // this is for streak complete day
            return false;
            /*
            string month = string.Format("{0}_{1}", date.Year, date.Month);
            if (!_dailyCountsByMonth.ContainsKey(month))
            {
                return false;
            }

            int day = date.Day;
            if (!_dailyCountsByMonth[month].ContainsKey(day))
            {
                return false;
            }
            return _dailyCountsByMonth[month][day] == 1;
            */
        }

        public string ToDateString(System.DateTime date, int offset, int subStrSize, bool useMonthFull = false, int style = 0)
        {
            string monthStr = useMonthFull ? date.ToString("MMMM") : date.ToString("MMM");
            string format = style switch
            {
                1 => "{0}<voffset={2}><size={3}>{1}</size></voffset> {4}",
                _ => "{4} {0}<voffset={2}><size={3}>{1}</size></voffset>"
            };
            return string.Format(format, date.Day, ToDateSubString(date), offset, subStrSize, monthStr);
        }

        public string ToDateSubString(System.DateTime date)
        {
            string dateSub = date.Day switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                21 => "st", 
                22 => "nd",
                23 => "rd",
                31 => "st",
                _ => "th"
            };
            return dateSub;
        }
        public void Close()
        {
            OnClose?.Invoke();
            UIManager.Instance.ReleaseUI(this, true);
        }

    }
}