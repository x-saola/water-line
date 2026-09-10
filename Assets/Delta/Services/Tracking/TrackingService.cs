using System.Collections.Generic;
using Delta.GameOps;

namespace Delta.Services
{
    public class TrackingService : ITrackingService
    {
        public void TrackCustomEvent(string eventId, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventId))
                return;

            if (parameters == null || parameters.Count == 0)
                DeltaApp.Instance.AnalyticsManager.TrackEvent(eventId);
            else
                DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(eventId, parameters);
        }

        public void TrackTutorial(string step, int status)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.TUTORIAL, new Dictionary<string, object>()
            {
                {"step", step},
                {"status", status}
            });
        }

        public void TrackGameStart(string gameMode, string id, string levelId, int attempts)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.GAME_START, new Dictionary<string, object>()
            {
                {"game_mode", gameMode},
                {"ID", id},
                {"level_id", levelId},
                {"attempts", attempts}
            });
        }

        public void TrackGameOver(string gameMode, string id, string levelId, int timeSpent, object result, int levelAbandoned, int attempts, string loseCause)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.GAME_OVER, new Dictionary<string, object>()
            {
                {"game_mode", gameMode},
                {"ID", id},
                {"level_id", levelId},
                {"time_spent", timeSpent},
                {"context", result},
                {"level_abandoned", levelAbandoned},
                {"attempts", attempts},
                {"lose_cause", loseCause},
            });
        }

        public void TrackSourceResourceEvent(string from, string currency, int value, object levelId, int balance)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.BI_RESOURCE_EVENT, new Dictionary<string, object>()
            {
                {"flow_type", "source"},
                {"from", from},
                {"virtual_currency_name", currency},
                {"value", value},
                {"level_id", levelId},
                {"balance", balance}
            });
        }

        public void TrackSinkResourceEvent(string to, string currency, int value, object levelId, int balance)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.BI_RESOURCE_EVENT, new Dictionary<string, object>()
            {
                {"flow_type", "sink"},
                {"to", to},
                {"virtual_currency_name", currency},
                {"value", value},
                {"level_id", levelId},
                {"balance", balance}
            });
        }

        public void TrackBusinessEvent(string productName, string productId, double localPrice, string currency, string transactionId)
        {
            DeltaApp.Instance.AnalyticsManager.TrackEventWithParameters(TrackingEventName.BI_BUSINESS_EVENT, new Dictionary<string, object>()
            {
                {"product_name", productName},
                {"product_id", productId},
                {"quantity", 1},
                {"local_price", localPrice},
                {"currency", currency},
                {"value", localPrice},
                {"transaction_id", transactionId}
            });
        }
    }
}
