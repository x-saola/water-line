/*
Example use case:
//Resource tracking
OneSDKBridge.Instance.TrackingBIResourceEvent(ResourceFlow.source, "rewarded video", "", "lives", GameConfigs.DEFAULT_AMOUNT_RESTORE_HEART, 0);
OneSDKBridge.Instance.TrackingBIResourceEvent(ResourceFlow.sink, "", "use booster", BoosterConfig.TRACKING_BOMB_NAME, BombBoosterAmountUsed, BombBoosterAmountRemain);
//Tutorial tracking
TrackingTutorial("tutorial_gameplay", TutorialStatus.start, "classic");
TrackingTutorial("tutorial_gameplay", TutorialStatus.end, "classic");
//tracking gamestart
//If game have saved data then call:
                OneSDKBridge.Instance.ResetTimeStartGame();
                otherwise call:
OneSDKBridge.Instance.TrackingGameStart();
 //tracking gameover
 GameLose:
  OneSDKBridge.Instance.TrackingGameEnd(0, true); //win level 0
  GameWin:
  OneSDKBridge.Instance.TrackingGameEnd(context: 1, isEndLevel: true);
  Back to home:
OneSDKBridge.Instance.TrackingGameEnd(isEndLevel: false);
  In GamePlayUI.cs call track when user pause apps:
  private void OnApplicationPause(bool isPause)
        {
           OneSDKBridge.Instance.TriggerApplicationPause(bool isPause, levelId, gameMode)
        }
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delta.GameOps;
using System;
using CustomUtils;
using UnityEngine.Purchasing;

public enum ResourceFlow
{
    source,
    sink
}
public enum TutorialStatus
{
    start = 0,
    end = 1
}
[Serializable]
public class LevelDataTracking
{
    [SerializeField]
    public string levelUniqueID;
    [SerializeField]
    public int attemptsGameStart;
    [SerializeField]
    public int attemptsGameOver;
    [SerializeField]
    public string timeStart;
}
public class LevelDataTempTracking
{
    public string gameMode;//classic/dailychallenge/...
    public string levelId;//which level do users start? classic_1,classic_2,.....
    public string gameMissionId;//using this param for the mission gameplay
    public string metaInfo;//using this param for the additional params : {"param1": "1", "param2": "2"}
    public string keyLevelSaveData;
    public LevelDataTempTracking()
    {
        gameMode = "";
        levelId = string.Empty;
        gameMissionId = string.Empty;
        metaInfo = string.Empty;
        keyLevelSaveData = string.Empty;
    }
}

public partial class OneSDKBridge : SingletonMono<OneSDKBridge>
{
    const string EVENT_TUTORIAL = "tutorial";
    const string EVENT_GAME_START = "game_start";
    const string EVENT_GAME_OVER = "game_over";
    const string EVENT_BI_RESOURCE_EVENT = "bi_resource_event";

    private void TrackEventNoParam(string eventName)
    {
        _deltaApp.AnalyticsManager.TrackEvent(eventName, allAgents: true);
    }
    private void TrackEventWithParameters(string eventName, Dictionary<string, object> parameters)
    {
        _deltaApp.AnalyticsManager.TrackEventWithParameters(eventName, parameters, allAgents: true);
    }
    private void SetUserProperty(string name, string value)
    {
        if (_deltaApp.IsFirebaseReady)
        {
            _deltaApp.AnalyticsManager.SetUserProperty(name, value);
        }
        else
        {
            _deltaApp.WaitToFirebaseInit(() =>
            {
                _deltaApp.AnalyticsManager.SetUserProperty(name, value);
            });
        }
    }
    //IAP tracking
    public void TrackRevenueAdjust(Product purchasedProduct, string name, string sku, string screenName)
    {
        _deltaApp.TrackRevenueAdjust(purchasedProduct, name, sku, screenName);
    }
    //Adjust log bi_business_event
    public void LogBussinessEvent(string productName, string productId, double localPrice, string currency, string unityReceipt, string unityTransactionId, string screenName, string promoCode = null)
    {
        _deltaApp.AnalyticsManager.LogBussinessEvent(productName, productId, localPrice, currency, unityReceipt, unityTransactionId, screenName, promoCode);
    }
    public void LogRevenue(double value, string currency, string transactionId, string productId)
    {
        _deltaApp.AnalyticsManager.LogRevenue(value, currency, transactionId, productId);
    }
    //Tracking in game
    public LevelDataTempTracking GetLevelInfoTracking(string levelId, string gameMode)
    {
        string levelIDTracking = levelId;

        LevelDataTempTracking levelInfo = new LevelDataTempTracking();

        levelInfo.gameMode = gameMode;
        levelInfo.levelId = levelIDTracking;

        levelInfo.keyLevelSaveData = gameMode + "_" + levelIDTracking;

        return levelInfo;
    }

    public void ResetTimeStartGame(string levelId, string gameMode = "classic")
    {
        string keyLevelData = GetLevelInfoTracking(levelId, gameMode).keyLevelSaveData;
        string dataLevelTracking = PlayerPrefs.GetString(keyLevelData, string.Empty);
        if (dataLevelTracking == string.Empty)
        {
            Debug.LogError("Data empty!");
            return;
        }
        LevelDataTracking tempLevelData = JsonUtility.FromJson<LevelDataTracking>(dataLevelTracking);

        tempLevelData.timeStart = DateTime.UtcNow.ToString();
        PlayerPrefs.SetString(keyLevelData, JsonUtility.ToJson(tempLevelData));
        Debug.Log("ResetTimeStartGame");
    }
    public void TrackingGameStart(string levelId, string gameMode)
    {
        LevelDataTempTracking levelInfo = GetLevelInfoTracking(levelId, gameMode);
        string keyLevelData = levelInfo.keyLevelSaveData;
        string dataLevelTracking = PlayerPrefs.GetString(keyLevelData, string.Empty);

        LevelDataTracking tempLevelData = new LevelDataTracking();
        if (dataLevelTracking == string.Empty)
        {
            tempLevelData.attemptsGameStart = 1;
            tempLevelData.attemptsGameOver = 1;
        }
        else
        {
            tempLevelData = JsonUtility.FromJson<LevelDataTracking>(dataLevelTracking);
            tempLevelData.attemptsGameStart += 1;
            tempLevelData.attemptsGameOver += 1;
        }
        tempLevelData.timeStart = DateTime.UtcNow.ToString();

        string levelUniqueID = Guid.NewGuid().ToString();
        tempLevelData.levelUniqueID = levelUniqueID;

        TrackingEventGameStart(tempLevelData.levelUniqueID, levelInfo, tempLevelData.attemptsGameStart);
        PlayerPrefs.SetString(keyLevelData, JsonUtility.ToJson(tempLevelData));
    }
    public void TrackingGameEnd(string levelId, string gameMode, int context = -1, bool isEndLevel = false)
    {
        //tracking gameover
        //context: 0 lose, 1 win
        LevelDataTempTracking levelInfo = GetLevelInfoTracking(levelId, gameMode);
        string keyLevelData = levelInfo.keyLevelSaveData;
        string dataLevelTracking = PlayerPrefs.GetString(keyLevelData, string.Empty);

        LevelDataTracking tempLevelData = new LevelDataTracking();
        if (dataLevelTracking == string.Empty)
        {
            Debug.LogError("===Level data empty!");
            return;
        }
        else
        {
            tempLevelData = JsonUtility.FromJson<LevelDataTracking>(dataLevelTracking);
            // tempLevelData.attemptsGameOver += 1;
            DateTime timeStart = DateTime.UtcNow;
            DateTime.TryParse(tempLevelData.timeStart, out timeStart);
            double totalSecondPlayGame = (DateTime.UtcNow - timeStart).TotalSeconds;

            PlayerPrefs.SetString(keyLevelData, JsonUtility.ToJson(tempLevelData));

            int levelAbandoned = isEndLevel ? 0 : 1;
            string loseCause = "";

            if (context < 0) levelAbandoned = 1; //if context < 0 then user leaving level

            if (isEndLevel && context == 0)
            {
                loseCause = "out of move";
            }

            TrackingEventGameOver(tempLevelData.levelUniqueID, levelInfo, (int)totalSecondPlayGame, context, levelAbandoned, tempLevelData.attemptsGameOver, loseCause: loseCause);
        }
    }
    //Call Tracking
    //example: TrackingTutorial("tutorial_1", TutorialStatus.start, "classic");
    public void TrackingTutorial(string tutorialId, TutorialStatus status, string gameMode = "")
    {
        //status 0: tutorial start
        //status 1: tutorial end
        Dictionary<string, object> tutorialData = new Dictionary<string, object>()
        {
            { "step", tutorialId},
            {"status", (int)status}
        };

        if (gameMode.Length > 0)
        {
            tutorialData.Add("game_mode", gameMode);
        }
        TrackEventWithParameters(EVENT_TUTORIAL, tutorialData);
    }
    private void TrackingEventGameStart(string levelUniqueID, LevelDataTempTracking levelInfo, int attempts)
    {
        Dictionary<string, object> gameStartData = new Dictionary<string, object>()
        {  {"game_mode",  levelInfo.gameMode},//default
            {"ID", levelUniqueID},
            {"level_id", levelInfo.levelId},
            {"attempts", attempts}
        };

        TrackEventWithParameters(EVENT_GAME_START, gameStartData);

#if CHEAT
        string eventName = EVENT_GAME_START;
        string d = "";
        foreach (var a in gameStartData)
        {
            d += "\n" + a.Key + ": " + a.Value;
        }
        Debug.Log(eventName + ": " + d);
#endif
    }
    private void TrackingEventGameOver(string levelUniqueID, LevelDataTempTracking levelInfo, int timeSpent, int context, int levelAbandoned, int attempts, string loseCause = "")
    {
        //context: 0 lose, 1 win
        //levelAbandoned: user leaving a level before failing or completing  0: no 1: yes
        Dictionary<string, object> gameOverData = new Dictionary<string, object>()
        {
            {"game_mode", levelInfo.gameMode},//default
            {"ID", levelUniqueID},
            {"level_id", levelInfo.levelId},
            {"time_spent", timeSpent},
            {"context", context},
            {"level_abandoned", levelAbandoned},
            {"attempts", attempts},
        };

        if (loseCause.Length > 0)
        {
            gameOverData.Add("lose_cause", loseCause);
        }
        TrackEventWithParameters(EVENT_GAME_OVER, gameOverData);

#if CHEAT
        string eventName = EVENT_GAME_OVER;

        string d = "";
        foreach (var a in gameOverData)
        {
            d += "\n" + a.Key + ": " + a.Value;
        }
        Debug.Log(eventName + ": " + d);
#endif
    }
    public void TrackingBIResourceEvent(ResourceFlow flow_type, string from, string to, string currency, int value, int balance, string levelId = "")
    {
        //flow_type: source(gain)/sink(reduce)
        Dictionary<string, object> dataResource = new Dictionary<string, object>()
        {
            {"flow_type", flow_type.ToString()},
            {"virtual_currency_name",currency},
            {"value",value},
            // {"level_id",level},
            {"balance",balance}
        };
        if (flow_type == ResourceFlow.sink)
        {
            dataResource.Add("to", to);
        }
        if (flow_type == ResourceFlow.source)
        {
            dataResource.Add("from", from);
        }
        if (!string.IsNullOrEmpty(levelId) && levelId.Length > 0)
        {
            dataResource.Add("level_id", levelId.ToString());
        }
        TrackEventWithParameters(EVENT_BI_RESOURCE_EVENT, dataResource);
#if CHEAT
        string d = "";
        foreach (var a in dataResource)
        {
            d += "\n" + a.Key + ": " + a.Value;
        }
        Debug.Log("TrackingBIResourceEvent: " + d);
#endif
    }
    //just call in OnApplicationPause of gameplay
    public void TriggerApplicationPause(bool isPause, string levelId, string gameMode)
    {
        if (isPause)
        {
            TrackingGameEnd(levelId, gameMode, isEndLevel: false);
        }
        else
        {
            ResetTimeStartGame(levelId, gameMode);
        }
    }
}
