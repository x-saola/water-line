#if USE_ADJUST
using System.Collections;
using System.Collections.Generic;
using AdjustSdk;

namespace Delta.GameOps
{
    public class AdjustAnalyticsAgent : UAAnalyticsAgent
    {
        Dictionary<string, string> _eventTokens = new Dictionary<string, string>();
        List<AdjustEvent> _pendingEvents = new List<AdjustEvent>();
        IMainAppService _appService;

        public AdjustAnalyticsAgent(string adjustAppToken, string[] eventNames, string[] eventTokens, IMainAppService appService)
        {
            _appService = appService;

            for (int i = 0; i < eventNames.Length; i++)
                _eventTokens.Add(eventNames[i], eventTokens[i]);

            _appService.StartCoroutine(WaitForAdjustReady());
        }

        public override void LogRevenue(string eventName, double value, string currency, string transactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = "")
        {
            if (_eventTokens.TryGetValue(eventName, out string eventToken))
            {
                var adjustEvent = new AdjustEvent(eventToken)
                {
                    TransactionId = transactionId,
                    ProductId = productId,
                    DeduplicationId = transactionId
                };
                adjustEvent.SetRevenue(value, currency);
                adjustEvent.AddCallbackParameter("transaction_id", transactionId);
                adjustEvent.AddCallbackParameter("product_id", productId);

                if (_appService.IsAdjustReady)
                {
                    Adjust.TrackEvent(adjustEvent);
                }
                else
                {
                    _pendingEvents.Add(adjustEvent);
                }
            }
        }
        public override void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement)
        {
            string source = "unknow";
#if USE_MAX_MEDIATION
            source = "applovin_max_sdk";
#endif
            var adjustAdRevenue = new AdjustAdRevenue(source)
            {
                AdRevenueNetwork = networkName,
                AdRevenueUnit = adUnitId,
                AdRevenuePlacement = adPlacement
            };
            adjustAdRevenue.SetRevenue(revenue, "USD");

            Adjust.TrackAdRevenue(adjustAdRevenue);
        }
        public override void LogSceneName(string sceneName, string sceneClass)
        {

        }

        public override void SetUserProperty(string name, string value)
        {

        }

        public override void LogEvent(string eventName)
        {
            string eventToken = null;
            if (_eventTokens.TryGetValue(eventName, out eventToken))
            {
                var adjustEvent = new AdjustEvent(eventToken);
                if (_appService.IsAdjustReady)
                {
                    Adjust.TrackEvent(adjustEvent);
                }
                else
                {
                    _pendingEvents.Add(adjustEvent);
                }
            }
        }

        public override void LogEventWithParameters(string eventName, Dictionary<string, object> parameters)
        {
            string eventToken = null;
            if (_eventTokens.TryGetValue(eventName, out eventToken))
            {
                var adjustEvent = new AdjustEvent(eventToken);
                if (_appService.IsAdjustReady)
                {
                    foreach (var keyPair in parameters)
                    {
                        adjustEvent.AddCallbackParameter(keyPair.Key, keyPair.Value == null ? string.Empty : keyPair.Value.ToString());
                    }
                    Adjust.TrackEvent(adjustEvent);
                }
                else
                {
                    _pendingEvents.Add(adjustEvent);
                }
            }
        }

        IEnumerator WaitForAdjustReady()
        {
            while (!_appService.IsAdjustReady)
                yield return null;

            if (_pendingEvents.Count > 0)
            {
                foreach (var adjustEvent in _pendingEvents)
                {
                    Adjust.TrackEvent(adjustEvent);
                }

                _pendingEvents.Clear();
            }
        }
    }
}

#endif