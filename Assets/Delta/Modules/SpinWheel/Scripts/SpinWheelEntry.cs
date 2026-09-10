using Delta.Modules.SpinWheel;
using UnityEngine;
using UnityEngine.UI;

public class SpinWheelEntry : MonoBehaviour
{
    [SerializeField] private Button btnEntry;
    [SerializeField] private Text textValidTime;
    [SerializeField] private GameObject reddot;

    private void OnEnable()
    {
        btnEntry.onClick.AddListener(ClickEntry);
        RefreshEntry();
    }

    private void OnDisable()
    {
        btnEntry.onClick.RemoveListener(ClickEntry);
    }

    private void RefreshEntry()
    {
        HandleTimeChange();
    }

    private void HandleTimeChange()
    {
        textValidTime.text = "Lucky Wheel";
        reddot.SetActive(!SpinWheel.Instance.IsDailySpinLimitReached());
    }

    private void ClickEntry()
    {
        var spinWheel = SpinWheel.Instance;
        spinWheel.Show();
    }
}
