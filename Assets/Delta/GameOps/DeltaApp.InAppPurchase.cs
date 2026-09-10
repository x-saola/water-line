using UnityEngine;
using UnityEngine.Purchasing;
using System;
#if USE_ADJUST
using AdjustSdk;
#endif

#if USE_ADJUST_PURCHASE
using com.adjust.sdk.purchase;
#endif

namespace Delta.GameOps
{
#if USE_ADJUST
 [Serializable]
    class UnityPurchaseReceipt
    {
        public string Store;
        public string TransactionID;
        public string Payload;
    }
 [Serializable]
    class UnityGooglePlayReceipt
    {
        public string json;
        public string signature;
    }

    [Serializable]
    class GooglePurchaseData
    {
        public string purchaseToken;
        public string developerPayload;
        public string orderId;
    }

    public partial class DeltaApp : MonoBehaviour, IMainAppService, IAdEventsListener
    {

        private string _adjustVerifyingProductId;
        private float _adjustVerifyingPrice;
        private string _adjustVerifyingCurrency;
        private string _adjustVerifyingTransactionId;
        public void TrackRevenueAdjust(Product purchasedProduct, string name, string sku, string screenName)
        {
            _adjustVerifyingProductId = purchasedProduct.definition.id;
            _adjustVerifyingPrice = (float)(purchasedProduct.metadata.localizedPrice);
            _adjustVerifyingCurrency = purchasedProduct.metadata.isoCurrencyCode;
            _adjustVerifyingTransactionId = purchasedProduct.transactionID;

            var unityReceipt = JsonUtility.FromJson<UnityPurchaseReceipt>(purchasedProduct.receipt);

            string transactionID = _adjustVerifyingTransactionId;
#if UNITY_ANDROID
            transactionID = GetOrderIdAndroid(purchasedProduct.receipt);
#endif

#if UNITY_IOS
            // AdjustPurchase.VerifyPurchaseiOS(unityReceipt.Payload,purchasedProduct.transactionID,
            // purchasedProduct.definition.id,VerificationInfoDelegate);

            string payloadEncrypt = "{\"Payload\": \"" + unityReceipt.Payload + "\"}";
            // Debug.Log("[ReceiptAPI] unityReceipt.Payload encrypt json: " + payloadEncrypt);
            LogAdjustRevenue();
            int quantity = 1;
            DeltaApp.Instance.AthenaGameService.ReportIAPReceipt(sku, payloadEncrypt, CallBackReportIAPReceipt, quantity, _adjustVerifyingPrice, _adjustVerifyingCurrency);
#elif UNITY_ANDROID
            var googleReceipt = JsonUtility.FromJson<UnityGooglePlayReceipt>(unityReceipt.Payload);
            var googlePurchaseData = JsonUtility.FromJson<GooglePurchaseData>(googleReceipt.json);

            string developerPayload = googlePurchaseData.developerPayload;
            if (string.IsNullOrEmpty(developerPayload))
                developerPayload = "";

            ReceiptAndroidData data = new ReceiptAndroidData();
            data.packageName = name;
            data.productId = purchasedProduct.definition.id;
            data.purchaseToken = googlePurchaseData.purchaseToken;
            data.orderId = GetOrderIdAndroid(purchasedProduct.receipt);
            string receiptData = JsonUtility.ToJson(data);

            ReceiptAndroidJson dataJson = new ReceiptAndroidJson();
            dataJson.json = receiptData;
            string payloadEncrypt1 = JsonUtility.ToJson(dataJson);

            PayloadAndroidJson payloadJson = new PayloadAndroidJson();
            payloadJson.Payload = payloadEncrypt1;
            string payloadEncrypt = JsonUtility.ToJson(payloadJson);
            Debug.Log("[ReceiptAPI] Payload receiptData: " + payloadEncrypt);


            // AdjustPurchase.VerifyPurchaseAndroid(purchasedProduct.definition.id,
            //                                 googlePurchaseData.purchaseToken,
            //                                 developerPayload,
            //                                 VerificationInfoDelegate);
            LogAdjustRevenue();
            int quantity = 1;
            DeltaApp.Instance.AthenaGameService.ReportIAPReceipt(sku, payloadEncrypt, CallBackReportIAPReceipt, quantity, _adjustVerifyingPrice, _adjustVerifyingCurrency);
#endif
            AnalyticsManager.LogBussinessEvent(name, sku,
                                  _adjustVerifyingPrice, _adjustVerifyingCurrency, purchasedProduct.receipt, transactionID, screenName);

        }
        public void CallBackReportIAPReceipt(bool result)
        {
            Debug.Log("[ReceiptAPI] API IAP Result: " + result + "\n ProductId: " + _adjustVerifyingProductId);
            // if (result)
            // {
            //     AthenaApp.Instance.AnalyticsManager.LogRevenue(_adjustVerifyingPrice, _adjustVerifyingCurrency, _adjustVerifyingTransactionId, _adjustVerifyingProductId);
            // }
        }
        private void LogAdjustRevenue()
        {
            DeltaApp.Instance.AnalyticsManager.LogRevenue(_adjustVerifyingPrice, _adjustVerifyingCurrency, _adjustVerifyingTransactionId, _adjustVerifyingProductId);
        }
#if USE_ADJUST_PURCHASE
        public void VerificationInfoDelegate(ADJPVerificationInfo verificationInfo)
        {
            if (verificationInfo.VerificationState == ADJPVerificationState.ADJPVerificationStatePassed)
            {
                AnalyticsManager.LogRevenue(_adjustVerifyingPrice, _adjustVerifyingCurrency, _adjustVerifyingTransactionId, _adjustVerifyingProductId);
            }
        }
#endif
#if UNITY_ANDROID
        [System.Serializable]
        public class AndroidUnityReceipt
        {
            public string Payload;
        }

        [System.Serializable]
        public class AndroidPayload
        {
            public string json;
        }

        [System.Serializable]
        public class AndroidReceipt
        {
            public string orderId;
        }
        private string GetOrderIdAndroid(string unityReceipt)
        {
            try
            {
                var receipt = JsonUtility.FromJson<AndroidUnityReceipt>(unityReceipt);
                var payload = JsonUtility.FromJson<AndroidPayload>(receipt.Payload);
                var androidReceipt = JsonUtility.FromJson<AndroidReceipt>(payload.json);
                return androidReceipt.orderId;
            }
            catch (System.Exception)
            {
                return "unknow";
            }
        }
        //android IAP payload json send BI  
        [Serializable]
        public class ReceiptAndroidData
        {
            public string orderId;
            public string packageName;
            public string productId;
            public string purchaseToken;
        }
        [Serializable]
        public class ReceiptAndroidJson
        {
            public string json;
        }
        [Serializable]
        public class PayloadAndroidJson
        {
            public string Payload;
        }
#endif
    }
#endif
}
