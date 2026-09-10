#if USE_APPSFLYER
using UnityEngine;
using System.Collections.Generic;
using AppsFlyerSDK;

[System.Serializable]
public class AppsFlyerConversionData
{
    public bool is_first_launch;
    public string af_status;
    public string media_source;
}

public class AppsFlyerTrackerCallbacks : MonoBehaviour, IAppsFlyerConversionData, IAppsFlyerValidateReceipt
{
    public static System.Action<AppsFlyerConversionData> OnReceivedConversionData;

    // Mark AppsFlyer CallBacks
    public void onConversionDataSuccess(string conversionData)
    {
        AppsFlyer.AFLog("didReceiveConversionData", conversionData);
        // Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
        // add deferred deeplink logic here

        if (OnReceivedConversionData != null)
        {
            try
            {
                var data = JsonUtility.FromJson<AppsFlyerConversionData>(conversionData);
                OnReceivedConversionData.Invoke(data);
            }
            catch (System.Exception e)
            {
                AppsFlyer.AFLog("onConversionDataSuccess", e.Message);
            }
        }
        else
        {
            AppsFlyer.AFLog("onConversionDataSuccess", "OnReceivedConversionData is NULL!");
        }
    }

    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        // add direct deeplink logic here
    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }

    public void didFinishValidateReceipt(string result)
    {
        AppsFlyer.AFLog("didFinishValidateReceipt", result);
    }

    public void didFinishValidateReceiptWithError(string error)
    {
        AppsFlyer.AFLog("didFinishValidateReceiptWithError", error);
    }
}
#endif