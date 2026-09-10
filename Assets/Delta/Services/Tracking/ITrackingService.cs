using System.Collections.Generic;

namespace Delta.Services
{
    public interface ITrackingService
    {
        void TrackCustomEvent(string eventId, Dictionary<string, object> parameters = null);
        void TrackTutorial(string step, int status);
        void TrackGameStart(string gameMode, string id, string levelId, int attempts);
        void TrackGameOver(string gameMode, string id, string levelId, int timeSpent, object result, int levelAbandoned, int attempts, string loseCause);
        void TrackSourceResourceEvent(string from, string currency, int value, object levelId, int balance);
        void TrackSinkResourceEvent(string to, string currency, int value, object levelId, int balance);
        void TrackBusinessEvent(string productName, string productId, double localPrice, string currency, string transactionId);
    }
}
