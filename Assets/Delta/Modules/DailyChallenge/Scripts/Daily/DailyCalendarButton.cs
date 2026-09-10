using System;
using UnityEngine;

namespace Delta.Modules.DailyChallenge_VerB
{
    public class DailyCalendarButton : MonoBehaviour
    {
        [SerializeField] private CheckmarkToggleUI _checkmarkToggleUI;

        void OnEnable()
        {
            RefreshNotice();
        }
        
        private void RefreshNotice()
        {
            string dateKey = DateTime.Now.ToString("dd/MM/yyyy");
            PlayerPrefs.GetInt("DailyPuzzle_" + dateKey, 0);
            var isDone = PlayerPrefs.GetInt("DailyPuzzle_" + dateKey, 0) == 1;
            if(isDone)
            {
                _checkmarkToggleUI.Hide();
            }
            else
            {
                _checkmarkToggleUI.ShowCheckmark();
            }
        }
    }
}
