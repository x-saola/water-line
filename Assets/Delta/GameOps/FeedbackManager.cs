using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Delta.GameOps
{
    [System.Serializable]
    public class FeedbackRequest
    {
        public string email;
        public string feedback;
        public int rating;
        public string os;
        public string device;
        public string osVersion;
        public string packageName;
        public string projectName;
        public string country;
        public string version;
        public long timestamp;
        public string tracking;
    }

    [System.Serializable]
    public class FeedbackTrackingParams<T> where T : AnalyticsManager.UserInfo
    {
        public string[] tags;
        public T metadata;
    }

    public class FeedbackManager
    {
        const string KEY_PENDING_FEEDBACK = "PENDING_FEEDBACK";

        string _feedbackEndPoint;
        IMainAppService _appService;

        public FeedbackManager(string endPoint, IMainAppService appService)
        {
            _feedbackEndPoint = endPoint;
            _appService = appService;

            var pendingFeedback = PlayerPrefs.GetString(KEY_PENDING_FEEDBACK, "");
            if (!string.IsNullOrEmpty(pendingFeedback))
            {
                Debug.Log("[FeedbackManager] Sending pending feedback...");
                ClearPendingFeedback();
                _appService.StartCoroutine(SendFeedback(pendingFeedback));
            }
        }

        public void PostFeedback(string email, string message, int rating, string version, string tracking = null)
        {
            var request = new FeedbackRequest()
            {
                email = email,
                feedback = message,
                rating = rating,
#if UNITY_ANDROID
                os = "android",
#elif UNITY_IOS
                os = "ios",
#endif
                device = SystemInfo.deviceModel,
                osVersion = SystemInfo.operatingSystem,
                packageName = Application.identifier,
                projectName = Application.productName,
                country = NativeHelper.GetCountry(),
                version = version,
                timestamp = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds(),
                tracking = tracking
            };

            var jsonString = JsonUtility.ToJson(request);
            if (!string.IsNullOrEmpty(jsonString))
                _appService.StartCoroutine(SendFeedback(jsonString));
        }

        IEnumerator SendFeedback(string jsonString)
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            var request = new UnityWebRequest(_feedbackEndPoint, "POST");
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                Debug.Log("[FeedbackManager] Send feedback finished!");
            }
            else if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                SavePendingFeedback(jsonString);
            }
            else
            {
                Debug.LogFormat("[FeedbackManager] Send feedback error with response code: {0}", request.responseCode);
            }
        }

        void SavePendingFeedback(string feedback)
        {
            PlayerPrefs.SetString(KEY_PENDING_FEEDBACK, feedback);
            PlayerPrefs.Save();
        }

        void ClearPendingFeedback()
        {
            PlayerPrefs.DeleteKey(KEY_PENDING_FEEDBACK);
            PlayerPrefs.Save();
        }
    }
}

