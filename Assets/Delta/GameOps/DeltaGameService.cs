using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;
using static Delta.GameOps.NativeHelper;

namespace Delta.GameOps
{
    [System.Serializable]
    public class AGSRequest
    {
        public int m_platform;
        public string email;
        public string fb_id;
        public string phone_number;
        public string project_id;
        public string ios_idfa;
        public string ios_idfv;
        public string android_advertising_id;
    }

    [System.Serializable]
    public class AGSResponse
    {
        public bool success;
        public AGSResult result;
        public AGSError err;
    }

    [System.Serializable]
    public class AGSResult
    {
        public string athena_id;
    }

    [System.Serializable]
    public class AGSError
    {
        public int err_code;
        public string message;
    }

    [System.Serializable]
    public class ReceiptAPIRequest
    {
        public string app_id;
        public string user_id;
        public string advertising_id;
        public string idfa;
        public string idfv;
        public string project_id;
        public string platform;
        public string product_id;
        public string receipt;

        public int quantity;
        public double product_price;
        public string currency;
    }

    [System.Serializable]
    public class ReceiptAPIResponse
    {
        public bool success;
        public ReceiptAPIError status;
    }

    [System.Serializable]
    public class ReceiptAPIError
    {
        public int code;
        public string message;
    }

    [System.Serializable]
    public class VerifyReceiptRequest
    {
        public string transaction_id;
        public string platform;
    }

    public class AthenaGameService
    {
        string FirebaseProjectId;

        string AGSRootAPI;
        string AGSUsersAPI;
        string AGSUsername;
        string AGSPassword;

        int MaxRetryCount;

        int TimeToNextRegister;

        public event Action<string> OnRegisterSuccessEvent;
        public event Action<string> OnRegisterIgnoreByCache;
        public event Action<ErrorCode, string> OnRegisterFailedEvent;

        private int currentRetryCount = 0;
        private Coroutine timeoutCoroutine;
        private Coroutine registerCoroutine;

        private const string kAGSLastSuccessTime = "AGSLastSuccessTime";
        private const string kAGSUserId = "AGSUserId";

        private IMainAppService _appService;

        string _IAPReportKey;
        string _IAPReportReceiptAPI;
        string _IAPVerifyReceiptAPI;
        string _IAPReportAuthUser;
        string _IAPReportAuthPass;
        int _IAPReportRetryCount;
        bool _IAPVerifyReceipt;
        string _iOSAppId;
        string _androidPackageName;

        public enum ErrorCode
        {
            #region Server Error Codes
            BodyEmpty = 1,
            PlatformNotSupport = 2,
            iOSMissingIDFAorIDFV = 3,
            AndroidMissingAID = 4,
            EmailInvalid = 5,
            MissingFirebaseId = 6,
            MissingSomeFields = 9999,
            #endregion Server Error Codes

            #region Client Error Codes
            ParseJsonException = 50,
            NoInternet = 51,
            RequestTimeOut = 52,
            NetworkError = 53,
            HttpError = 54,
            UnknownError = 99,

            #endregion Client Error Codes
        }

        public AthenaGameService(string projectId, string rootAPI, string usersAPI, string username, string password, int maxRetryCount, int timeToNextRegister, IMainAppService appService)
        {
            FirebaseProjectId = projectId;
            AGSRootAPI = rootAPI;
            AGSUsersAPI = usersAPI;
            AGSUsername = username;
            AGSPassword = password;

            MaxRetryCount = maxRetryCount;
            TimeToNextRegister = timeToNextRegister;

            _appService = appService;
        }

        public void SetupIAPReportService(string iosAppId, string androidPackageName, string encryptKey, string receiptAPI, string verifyAPI, string authUser, string authPass, int retryCount, bool verifyReceipt)
        {
            _iOSAppId = iosAppId;
            _androidPackageName = androidPackageName;
            _IAPReportKey = encryptKey;
            _IAPReportReceiptAPI = receiptAPI;
            _IAPVerifyReceiptAPI = verifyAPI;
            _IAPReportAuthUser = authUser;
            _IAPReportAuthPass = authPass;
            _IAPReportRetryCount = retryCount;
            _IAPVerifyReceipt = verifyReceipt;
        }

        public void RegisterUser()
        {
            if (registerCoroutine != null)
                return;

            float now = (float)DateTimeOffset.Now.ToUnixTimeSeconds();
            float lastSuccessTime = PlayerPrefs.GetFloat(kAGSLastSuccessTime, 0);

            //Use cached data if needed
            if (now - lastSuccessTime < TimeToNextRegister)
            {
                string agsUserId = PlayerPrefs.GetString(kAGSUserId, "");
                // Debug.LogWarningFormat("AGS Use user id from cache - Don't register now. Old User id: {0}", agsUserId);
                OnRegisterIgnoreByCache?.Invoke(agsUserId);
                return;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("[AGS] No internet!");
                onRegisterFailed(ErrorCode.NoInternet, ErrorCode.NoInternet.ToString());
                return;
            }

            Debug.Log("[AGS] Registering user ...");
            registerCoroutine = _appService.StartCoroutine(registerUserCoroutine(FirebaseProjectId, "", "", ""));
        }

        public void ReportIAPReceipt(string productId, string receipt, System.Action<bool> callback = null, int quantity = 1, double productPrice = 0, string currency = null)
        {
#if UNITY_EDITOR
            callback?.Invoke(true);
#else
            _appService.StartCoroutine(reportIAPReceipt(productId, receipt, quantity, productPrice, currency, callback));
#endif
        }

        public void VerifyIAPReceipt(string transactionId, System.Action<bool> callback)
        {
#if UNITY_EDITOR
            callback?.Invoke(true);
#else
            if (_IAPVerifyReceipt)
                _appService.StartCoroutine(verifyIAPReceipt(transactionId, callback));
            else
            {
                callback?.Invoke(true);
            }
#endif
        }

        #region RegisterAGSUser
        IEnumerator registerUserCoroutine(string projectId, string email, string phone, string fbID)
        {
            // url
            string urlRegisterAPI = System.IO.Path.Combine(AGSRootAPI, AGSUsersAPI);

            // request body
            var requestBody = new AGSRequest()
            {
                email = string.Empty,
                fb_id = string.Empty,
                phone_number = string.Empty,
                project_id = FirebaseProjectId,
            };

#if UNITY_IOS
            requestBody.m_platform = 1;
            requestBody.ios_idfa = NativeHelper.getIDFA();
            requestBody.ios_idfv = NativeHelper.getIDFV();
            requestBody.android_advertising_id = string.Empty;
#elif UNITY_ANDROID
            requestBody.m_platform = 2;
            requestBody.ios_idfa = string.Empty;
            requestBody.ios_idfv = string.Empty;
            bool adIdFinished = false;
            string androidAdvertisingId = string.Empty;
            Application.RequestAdvertisingIdentifierAsync((advertisingId, trackingEnabled, error) =>
            {
                adIdFinished = true;
                if (string.IsNullOrEmpty(error))
                    androidAdvertisingId = advertisingId;
            });
            while (!adIdFinished)
                yield return null;

            requestBody.android_advertising_id = androidAdvertisingId;
#endif
            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyData = Encoding.UTF8.GetBytes(jsonBody);

            // http request
            UnityWebRequest request = new UnityWebRequest(urlRegisterAPI, UnityWebRequest.kHttpVerbPOST);
            updateHeaders(request);

            request.uploadHandler = new UploadHandlerRaw(bodyData);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return waitForResponse(request, onResponseSuccess, onResponseFailed);

            registerCoroutine = null;
        }

        private void onResponseSuccess(UnityWebRequest response)
        {
            try
            {
                var agsResponse = JsonUtility.FromJson<AGSResponse>(response.downloadHandler.text);
                if (agsResponse != null)
                {
                    if (agsResponse.success)
                    {
                        Debug.LogFormat("[AGS] Success with athena id: {0}", agsResponse.result.athena_id);
                        onRegisterSuccess(agsResponse.result.athena_id);
                    }
                    else
                    {
                        int serverErrorCode = agsResponse.err.err_code;
                        string serverErrorMessage = agsResponse.err.message;

                        Debug.LogFormat("[AGS] Failed with error: {0}({1})!", serverErrorMessage, serverErrorCode);
                        onRegisterFailed(ErrorCode.MissingSomeFields, serverErrorCode + "_" + serverErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("[AGS] JSON exception: {0}", ex.Message);
                if (response != null && response.downloadHandler != null)
                {
                    Debug.LogError("[AGS] AGS Parse Json Error: " + response.downloadHandler.text);
                }

                onRegisterFailed(ErrorCode.ParseJsonException, ErrorCode.ParseJsonException.ToString());
            }
        }

        private IEnumerator retryRegisterUser()
        {
            if (currentRetryCount < MaxRetryCount)
            {
                currentRetryCount++;
                yield return new WaitForSeconds(3f);
                RegisterUser();
            }
            else
            {
                Debug.LogWarning("[AGS] No more chances to retry!");
            }
        }

        private void onResponseFailed(UnityWebRequest reponse)
        {
            // retry
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                _appService.StartCoroutine(retryRegisterUser());
                return;
            }

            // no chance for this launch, let just retry on next launch
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return;

            ErrorCode errorCode = ErrorCode.RequestTimeOut;
            string errorMessage = errorCode.ToString();

            if (reponse != null)
            {
                if (reponse.result == UnityWebRequest.Result.ConnectionError)
                {
                    errorCode = ErrorCode.NetworkError;
                    errorMessage = errorCode.ToString() + "_" + reponse.error;
                }
                else if (reponse.result == UnityWebRequest.Result.ProtocolError)
                {
                    errorCode = ErrorCode.HttpError;
                    errorMessage = errorCode.ToString() + "_" + reponse.error;
                }
                else
                {
                    errorCode = ErrorCode.UnknownError;
                    errorMessage = reponse.responseCode.ToString() + "_" + reponse.error;
                }
            }

            onRegisterFailed(errorCode, errorMessage);
        }

        private void updateHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("AUTHORIZATION", "Basic "
                + System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1")
                    .GetBytes(AGSUsername + ":" + AGSPassword)));
        }

        private void onRegisterSuccess(string userId)
        {
            currentRetryCount = 0;
            float now = (float)(DateTime.Now.Ticks / TimeSpan.TicksPerSecond);
            PlayerPrefs.SetFloat(kAGSLastSuccessTime, now);
            PlayerPrefs.SetString(kAGSUserId, userId);
            PlayerPrefs.Save();

            if (OnRegisterSuccessEvent != null)
            {
                OnRegisterSuccessEvent(userId);
            }
        }

        private void onRegisterFailed(ErrorCode code, string message)
        {
            if (OnRegisterFailedEvent != null)
            {
                OnRegisterFailedEvent(code, message);
            }
        }

        IEnumerator waitForResponse(UnityWebRequest www, System.Action<UnityWebRequest> onSuccess, System.Action<UnityWebRequest> onFailure)
        {
            timeoutCoroutine = _appService.StartCoroutine(waitForTimeOut(www, onFailure));
            yield return www.SendWebRequest();

            if (timeoutCoroutine != null)
            {
                _appService.StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
#if UNITY_EDITOR
                    Debug.LogWarningFormat("[AGS] HTTP request failed with error {0}", www.error);
#endif
                    onFailure(www);
                }
                else
                {
                    onSuccess(www);
                }
            }
        }

        IEnumerator waitForTimeOut(UnityWebRequest www, System.Action<UnityWebRequest> onFailure)
        {
            yield return new WaitForSeconds(30f);

            timeoutCoroutine = null;

            if (string.IsNullOrEmpty(www.error) && !www.isDone)
            {
                Debug.LogWarning("[AGS] Register user has been timeout!");
                onFailure(null);
            }
        }
        #endregion

        #region Report IAP process
        IEnumerator verifyIAPReceipt(string transactionId, System.Action<bool> callback)
        {
            var request = new VerifyReceiptRequest()
            {
#if UNITY_ANDROID
                platform = "android",
#elif UNITY_IOS
                platform = "ios",
#endif
                transaction_id = transactionId
            };

            string jsonBody = JsonUtility.ToJson(request);
            byte[] bodyData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            yield return VerifyIAPReceipt(bodyData, _IAPReportRetryCount, _IAPVerifyReceiptAPI, _IAPReportAuthUser, _IAPReportAuthPass, callback);
        }

        IEnumerator reportIAPReceipt(string productId, string receipt, int quantity, double productPrice, string currency, System.Action<bool> callback)
        {
            var strEncrypted = receipt;
            try
            {
                strEncrypted = AES256Encrypt(receipt, _IAPReportKey);
            }
            catch (System.Exception ex)
            {
                Debug.Log("[ReceiptAPI] Encrypt receipt with exeption: " + ex.Message);
                callback?.Invoke(false);
                yield break;
            }

            var request = new ReceiptAPIRequest()
            {
#if UNITY_ANDROID
                platform = "android",
                app_id = _androidPackageName,
#elif UNITY_IOS
                platform = "ios",
                app_id = _iOSAppId,
#endif
                project_id = FirebaseProjectId,
                product_id = productId,
                quantity = quantity,
                product_price = productPrice,
                currency = currency,
                receipt = strEncrypted,
                user_id = PlayerPrefs.GetString("playfabUserIDKey", "") // add playfab user id in project here!
            };
            Debug.LogError("Add playfab user id in project here! if use ReportIAP!");
#if UNITY_IOS
            request.idfa = NativeHelper.getIDFA();
            request.idfv = NativeHelper.getIDFV();
#elif UNITY_ANDROID && !UNITY_EDITOR
            bool adIdFinished = false;
            string androidAdvertisingId = string.Empty;

            Debug.Log("[AGS] RequestAdvertisingIdentifierAsync...");
            AndroidRequestAdvertisingIdentifierAsync.Request((advertisingId, trackingEnabled, error) =>
            {
                Debug.Log("[ReceiptAPI] advertisingId: " + advertisingId + " - trackingEnabled: " + trackingEnabled + " - error: " + error);
                adIdFinished = true;
                if (string.IsNullOrEmpty(error))
                    androidAdvertisingId = advertisingId;
            });
            while (!adIdFinished)
                yield return null;

            request.advertising_id = androidAdvertisingId;
#endif
            string jsonBody = JsonUtility.ToJson(request);
            byte[] bodyData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            yield return ReportIAPReceipt(bodyData, _IAPReportRetryCount, _IAPReportReceiptAPI, _IAPReportAuthUser, _IAPReportAuthPass, callback);
        }

        IEnumerator ReportIAPReceipt(byte[] bodyData, int retryCount, string endPointAPI, string authUser, string authPassword, System.Action<bool> callback)
        {
            UnityWebRequest request = new UnityWebRequest(endPointAPI, UnityWebRequest.kHttpVerbPOST);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("AUTHORIZATION", "Basic "
                + System.Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(authUser + ":" + authPassword)));


            request.uploadHandler = new UploadHandlerRaw(bodyData);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogFormat("[ReceiptAPI] HTTP request failed with error {0}", request.error);

                if (retryCount > 0)
                {
                    yield return new WaitForSeconds(5f);
                    yield return ReportIAPReceipt(bodyData, retryCount - 1, endPointAPI, authUser, authPassword, callback);
                }
                else
                {
                    callback?.Invoke(false);
                }
            }
            else
            {
                var response = JsonUtility.FromJson<ReceiptAPIResponse>(request.downloadHandler.text);
                if (response != null && response.success)
                {
                    Debug.Log("[ReceiptAPI] - OK");
                    callback?.Invoke(true);
                }
                else if (response != null)
                {
                    if (response.status != null && response.status.code != 0)
                    {
                        Debug.LogFormat("[ReceiptAPI] Server returned error: {0}", response.status.code);

                        // Retry for internal server error
                        if (response.status.code == 1000 || response.status.code == 21002 || response.status.code == 21009)
                        {
                            if (retryCount > 0)
                            {
                                yield return new WaitForSeconds(5f);
                                yield return ReportIAPReceipt(bodyData, retryCount - 1, endPointAPI, authUser, authPassword, callback);
                            }
                            // Internal server error -> let treat as a valid receipt
                            else
                            {
                                Debug.LogFormat("[ReceiptAPI] Internal server error -> let treat as a valid receipt: {0}", response.status.code);
                                callback?.Invoke(true);
                            }
                        }
                        // Otherwise, this is an invalid receipt
                        else
                        {
                            Debug.LogFormat("[ReceiptAPI] This is an invalid receipt: {0}", response.status.code);
                            callback?.Invoke(false);
                        }
                    }
                    // Wrong response data format
                    else
                    {
                        Debug.LogFormat("[ReceiptAPI] Wrong response data format: {0}", request.downloadHandler.text);
                        callback?.Invoke(false);
                    }
                }
            }
        }

        IEnumerator VerifyIAPReceipt(byte[] bodyData, int retryCount, string endPointAPI, string authUser, string authPassword, System.Action<bool> callback)
        {
            UnityWebRequest request = new UnityWebRequest(endPointAPI, UnityWebRequest.kHttpVerbPOST);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("AUTHORIZATION", "Basic "
                + System.Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(authUser + ":" + authPassword)));

            request.uploadHandler = new UploadHandlerRaw(bodyData);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogFormat("[VerifyAPI] HTTP request failed with error {0}", request.error);

                if (retryCount > 0)
                {
                    yield return new WaitForSeconds(5f);
                    yield return VerifyIAPReceipt(bodyData, retryCount - 1, endPointAPI, authUser, authPassword, callback);
                }
                else
                {
                    callback?.Invoke(false);
                }
            }
            else
            {
                var response = JsonUtility.FromJson<ReceiptAPIResponse>(request.downloadHandler.text);
                if (response != null && response.success)
                {
                    Debug.Log("[VerifyAPI] - OK");
#if UNITY_IOS
                    callback?.Invoke(true);
#elif UNITY_ANDROID
                    callback?.Invoke(response.status != null && response.status.code == 201);
#endif
                }
                else if (response != null)
                {
                    if (response.status != null && response.status.code != 0)
                    {
                        Debug.LogFormat("[VerifyAPI] Server returned error: {0}", response.status.code);

                        // Retry for internal server error
                        if (response.status.code == 1000 || response.status.code == 21002 || response.status.code == 21009)
                        {
                            if (retryCount > 0)
                            {
                                yield return new WaitForSeconds(5f);
                                yield return VerifyIAPReceipt(bodyData, retryCount - 1, endPointAPI, authUser, authPassword, callback);
                            }
                            // Internal server error -> let treat as a valid transaction
                            else
                            {
                                Debug.LogFormat("[VerifyAPI] Internal server error -> let treat as a valid transaction: {0}", response.status.code);
                                callback?.Invoke(true);
                            }
                        }
                        // Otherwise, this is an invalid transaction
                        else
                        {
                            Debug.LogFormat("[VerifyAPI] This is an invalid transaction: {0}", response.status.code);
                            callback?.Invoke(false);
                        }
                    }
                    // Wrong response data format
                    else
                    {
                        Debug.LogFormat("[VerifyAPI] Wrong response data format: {0}", response.status.code);
                        callback?.Invoke(false);
                    }
                }
            }
        }

        public static string AES256Encrypt(string plainText, string keyString)
        {
            byte[] cipherData;
            Aes aes = Aes.Create();
            aes.Key = System.Text.Encoding.UTF8.GetBytes(keyString);
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            ICryptoTransform cipher = aes.CreateEncryptor(aes.Key, aes.IV);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, cipher, CryptoStreamMode.Write))
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                }

                cipherData = ms.ToArray();
            }

            byte[] combinedData = new byte[aes.IV.Length + cipherData.Length];
            System.Array.Copy(aes.IV, 0, combinedData, 0, aes.IV.Length);
            System.Array.Copy(cipherData, 0, combinedData, aes.IV.Length, cipherData.Length);
            return System.Convert.ToBase64String(combinedData);
        }
        #endregion
    }
}