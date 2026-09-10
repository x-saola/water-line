using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Delta.Modules.Racing.Editor
{
    public class RacePrefabCreator : EditorWindow
    {
        [MenuItem("Tools/Racing/Create UI Prefabs")]
        public static void CreateAllUIPrefabs()
        {
            string resourcesPath = "Assets/Resources";
            
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            CreateRaceEntryUIPrefab(resourcesPath);
            CreateRaceActiveUIPrefab(resourcesPath);
            CreateRaceResultUIPrefab(resourcesPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RacePrefabCreator] All UI prefabs created successfully in Resources folder!");
            EditorUtility.DisplayDialog("Success", "Race UI prefabs created:\n- RaceEntryUI\n- RaceActiveUI\n- RaceResultUI\n\nCheck the Resources folder.", "OK");
        }

        private static void CreateRaceEntryUIPrefab(string path)
        {
            // Create root Canvas
            GameObject prefab = new GameObject("RaceEntryUI");
            Canvas canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefab.AddComponent<CanvasScaler>();
            prefab.AddComponent<GraphicRaycaster>();

            // Add RaceEntryUI script
            var raceUI = prefab.AddComponent<RaceEntryUI>();

            // Create Background
            GameObject background = CreateUIElement("Background", prefab.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            SetFullScreen(background.GetComponent<RectTransform>());

            // Create Panel
            GameObject panel = CreateUIElement("Panel", prefab.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800, 600);

            // Create Title
            GameObject titleText = CreateTextElement("TitleText", panel.transform, "RACE ENTRY", 36);
            PositionElement(titleText.GetComponent<RectTransform>(), 0, 250, 700, 60);

            // Create Tier Text
            GameObject tierText = CreateTextElement("TierText", panel.transform, "Bronze Tier", 28);
            PositionElement(tierText.GetComponent<RectTransform>(), 0, 170, 600, 50);

            // Create Objectives Text
            GameObject objectivesText = CreateTextElement("ObjectivesText", panel.transform, "Complete 3 objectives to win!", 20);
            PositionElement(objectivesText.GetComponent<RectTransform>(), 0, 120, 600, 40);

            // Create Daily Limit Text
            GameObject dailyLimitText = CreateTextElement("DailyLimitText", panel.transform, "Daily Races: 5/5", 18);
            PositionElement(dailyLimitText.GetComponent<RectTransform>(), 0, 70, 400, 30);

            // Create Tier Progress Text
            GameObject tierProgressText = CreateTextElement("TierProgressText", panel.transform, "0/3 wins to promote", 18);
            PositionElement(tierProgressText.GetComponent<RectTransform>(), 0, 30, 400, 30);

            // Create Cooldown Panel
            GameObject cooldownPanel = CreateUIElement("CooldownPanel", panel.transform);
            Image cooldownPanelImage = cooldownPanel.AddComponent<Image>();
            cooldownPanelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            PositionElement(cooldownPanel.GetComponent<RectTransform>(), 0, -20, 600, 80);
            cooldownPanel.SetActive(false);

            GameObject cooldownText = CreateTextElement("CooldownText", cooldownPanel.transform, "Next Race Available In:\n00:00:00", 20);
            SetFullScreen(cooldownText.GetComponent<RectTransform>());

            // Create Rewards Container
            GameObject rewardsContainer = CreateUIElement("RewardsContainer", panel.transform);
            rewardsContainer.AddComponent<HorizontalLayoutGroup>();
            PositionElement(rewardsContainer.GetComponent<RectTransform>(), 0, -90, 600, 80);

            // Create Enter Button
            GameObject enterButton = CreateButtonElement("EnterButton", panel.transform, "ENTER RACE");
            PositionElement(enterButton.GetComponent<RectTransform>(), 0, -180, 300, 60);

            // Create Close Button
            GameObject closeButton = CreateButtonElement("CloseButton", panel.transform, "X");
            PositionElement(closeButton.GetComponent<RectTransform>(), 350, 250, 60, 60);

            // Assign references via SerializedObject
            SerializedObject so = new SerializedObject(raceUI);
            so.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            so.FindProperty("_enterButton").objectReferenceValue = enterButton.GetComponent<Button>();
            so.FindProperty("_tierText").objectReferenceValue = tierText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_objectivesRequiredText").objectReferenceValue = objectivesText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_dailyLimitText").objectReferenceValue = dailyLimitText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_cooldownText").objectReferenceValue = cooldownText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_tierProgressText").objectReferenceValue = tierProgressText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_cooldownPanel").objectReferenceValue = cooldownPanel;
            so.FindProperty("_rewardsParent").objectReferenceValue = rewardsContainer.transform;
            so.ApplyModifiedProperties();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(prefab, $"{path}/RaceEntryUI.prefab");
            DestroyImmediate(prefab);
            
            Debug.Log($"Created: {path}/RaceEntryUI.prefab");
        }

        private static void CreateRaceActiveUIPrefab(string path)
        {
            // Create root Canvas
            GameObject prefab = new GameObject("RaceActiveUI");
            Canvas canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefab.AddComponent<CanvasScaler>();
            prefab.AddComponent<GraphicRaycaster>();

            // Add RaceActiveUI script
            var raceUI = prefab.AddComponent<RaceActiveUI>();

            // Create Background
            GameObject background = CreateUIElement("Background", prefab.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.5f);
            SetFullScreen(background.GetComponent<RectTransform>());

            // Create Panel
            GameObject panel = CreateUIElement("Panel", prefab.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(900, 700);

            // Create Timer Text
            GameObject timerText = CreateTextElement("TimerText", panel.transform, "10:00", 48);
            PositionElement(timerText.GetComponent<RectTransform>(), 0, 300, 200, 60);

            // Create Tier Text
            GameObject tierText = CreateTextElement("TierText", panel.transform, "Bronze Tier Race", 24);
            PositionElement(tierText.GetComponent<RectTransform>(), 0, 250, 400, 40);

            // Player Section
            GameObject playerSection = CreateUIElement("PlayerSection", panel.transform);
            PositionElement(playerSection.GetComponent<RectTransform>(), 0, 150, 800, 100);

            GameObject playerName = CreateTextElement("PlayerNameText", playerSection.transform, "YOU", 22);
            PositionElement(playerName.GetComponent<RectTransform>(), -300, 0, 100, 30);

            GameObject playerProgressBar = CreateSliderElement("PlayerProgressBar", playerSection.transform);
            PositionElement(playerProgressBar.GetComponent<RectTransform>(), 50, 0, 500, 40);

            GameObject playerProgressText = CreateTextElement("PlayerProgressText", playerSection.transform, "0/3", 20);
            PositionElement(playerProgressText.GetComponent<RectTransform>(), 350, 0, 80, 30);

            // Competitors Container
            GameObject competitorsContainer = CreateUIElement("CompetitorsContainer", panel.transform);
            VerticalLayoutGroup vlg = competitorsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childForceExpandHeight = false;
            PositionElement(competitorsContainer.GetComponent<RectTransform>(), 0, -50, 800, 400);

            // Close Button
            GameObject closeButton = CreateButtonElement("CloseButton", panel.transform, "X");
            PositionElement(closeButton.GetComponent<RectTransform>(), 400, 300, 60, 60);

            // Assign references
            SerializedObject so = new SerializedObject(raceUI);
            so.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            so.FindProperty("_timerText").objectReferenceValue = timerText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_tierText").objectReferenceValue = tierText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_playerNameText").objectReferenceValue = playerName.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_playerProgressBar").objectReferenceValue = playerProgressBar.GetComponent<Slider>();
            so.FindProperty("_playerProgressText").objectReferenceValue = playerProgressText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_competitorsParent").objectReferenceValue = competitorsContainer.transform;
            so.ApplyModifiedProperties();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(prefab, $"{path}/RaceActiveUI.prefab");
            DestroyImmediate(prefab);
            
            Debug.Log($"Created: {path}/RaceActiveUI.prefab");
        }

        private static void CreateRaceResultUIPrefab(string path)
        {
            // Create root Canvas
            GameObject prefab = new GameObject("RaceResultUI");
            Canvas canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefab.AddComponent<CanvasScaler>();
            prefab.AddComponent<GraphicRaycaster>();

            // Add RaceResultUI script
            var raceUI = prefab.AddComponent<RaceResultUI>();

            // Create Background
            GameObject background = CreateUIElement("Background", prefab.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.9f);
            SetFullScreen(background.GetComponent<RectTransform>());

            // Victory Panel
            GameObject victoryPanel = CreateUIElement("VictoryPanel", prefab.transform);
            RectTransform victoryRect = victoryPanel.GetComponent<RectTransform>();
            victoryRect.sizeDelta = new Vector2(800, 600);

            GameObject victoryTitle = CreateTextElement("VictoryTitleText", victoryPanel.transform, "VICTORY!", 56);
            PositionElement(victoryTitle.GetComponent<RectTransform>(), 0, 200, 700, 80);
            victoryTitle.GetComponent<TextMeshProUGUI>().color = Color.yellow;

            GameObject victoryMessage = CreateTextElement("VictoryMessageText", victoryPanel.transform, "You won the race!", 28);
            PositionElement(victoryMessage.GetComponent<RectTransform>(), 0, 120, 600, 50);

            // Defeat Panel
            GameObject defeatPanel = CreateUIElement("DefeatPanel", prefab.transform);
            RectTransform defeatRect = defeatPanel.GetComponent<RectTransform>();
            defeatRect.sizeDelta = new Vector2(800, 600);
            defeatPanel.SetActive(false);

            GameObject defeatTitle = CreateTextElement("DefeatTitleText", defeatPanel.transform, "DEFEAT", 56);
            PositionElement(defeatTitle.GetComponent<RectTransform>(), 0, 200, 700, 80);
            defeatTitle.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.2f, 0.2f);

            GameObject defeatMessage = CreateTextElement("DefeatMessageText", defeatPanel.transform, "Better luck next time!", 28);
            PositionElement(defeatMessage.GetComponent<RectTransform>(), 0, 120, 600, 50);

            // Common elements (create on main canvas)
            GameObject rewardsContainer = CreateUIElement("RewardsContainer", prefab.transform);
            rewardsContainer.AddComponent<HorizontalLayoutGroup>();
            PositionElement(rewardsContainer.GetComponent<RectTransform>(), 0, 0, 700, 100);

            GameObject tierChangeText = CreateTextElement("TierChangeText", prefab.transform, "", 24);
            PositionElement(tierChangeText.GetComponent<RectTransform>(), 0, -80, 600, 40);

            GameObject tierProgressText = CreateTextElement("TierProgressText", prefab.transform, "1/3 wins to promote", 20);
            PositionElement(tierProgressText.GetComponent<RectTransform>(), 0, -120, 600, 30);

            GameObject nextRaceText = CreateTextElement("NextRaceText", prefab.transform, "Next race in: 01:00:00", 20);
            PositionElement(nextRaceText.GetComponent<RectTransform>(), 0, -160, 600, 30);

            GameObject adMultiplierText = CreateTextElement("AdMultiplierText", prefab.transform, "2x", 32);
            PositionElement(adMultiplierText.GetComponent<RectTransform>(), 150, -220, 80, 40);

            GameObject claimButton = CreateButtonElement("ClaimButton", prefab.transform, "CLAIM");
            PositionElement(claimButton.GetComponent<RectTransform>(), -150, -220, 200, 60);

            GameObject claimWithAdButton = CreateButtonElement("ClaimWithAdButton", prefab.transform, "WATCH AD");
            PositionElement(claimWithAdButton.GetComponent<RectTransform>(), 150, -220, 200, 60);

            GameObject closeButton = CreateButtonElement("CloseButton", prefab.transform, "CLOSE");
            PositionElement(closeButton.GetComponent<RectTransform>(), 0, -290, 200, 60);

            // Assign references
            SerializedObject so = new SerializedObject(raceUI);
            so.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            so.FindProperty("_claimButton").objectReferenceValue = claimButton.GetComponent<Button>();
            so.FindProperty("_claimWithAdButton").objectReferenceValue = claimWithAdButton.GetComponent<Button>();
            so.FindProperty("_victoryPanel").objectReferenceValue = victoryPanel;
            so.FindProperty("_defeatPanel").objectReferenceValue = defeatPanel;
            so.FindProperty("_victoryTitleText").objectReferenceValue = victoryTitle.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_victoryMessageText").objectReferenceValue = victoryMessage.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_defeatTitleText").objectReferenceValue = defeatTitle.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_defeatMessageText").objectReferenceValue = defeatMessage.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_rewardsParent").objectReferenceValue = rewardsContainer.transform;
            so.FindProperty("_tierChangeText").objectReferenceValue = tierChangeText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_tierProgressText").objectReferenceValue = tierProgressText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_nextRaceText").objectReferenceValue = nextRaceText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_adMultiplierText").objectReferenceValue = adMultiplierText.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(prefab, $"{path}/RaceResultUI.prefab");
            DestroyImmediate(prefab);
            
            Debug.Log($"Created: {path}/RaceResultUI.prefab");
        }

        // Helper methods
        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;
            return go;
        }

        private static GameObject CreateTextElement(string name, Transform parent, string text, int fontSize)
        {
            GameObject go = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        private static GameObject CreateButtonElement(string name, Transform parent, string buttonText)
        {
            GameObject go = CreateUIElement(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f);
            Button btn = go.AddComponent<Button>();

            GameObject textGo = CreateTextElement("Text", go.transform, buttonText, 24);
            SetFullScreen(textGo.GetComponent<RectTransform>());

            return go;
        }

        private static GameObject CreateSliderElement(string name, Transform parent)
        {
            GameObject go = CreateUIElement(name, parent);
            Slider slider = go.AddComponent<Slider>();

            GameObject background = CreateUIElement("Background", go.transform);
            Image bgImg = background.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            SetFullScreen(background.GetComponent<RectTransform>());

            GameObject fillArea = CreateUIElement("Fill Area", go.transform);
            SetFullScreen(fillArea.GetComponent<RectTransform>());

            GameObject fill = CreateUIElement("Fill", fillArea.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 1f, 0.2f);
            SetFullScreen(fill.GetComponent<RectTransform>());

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImg;

            return go;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void PositionElement(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}

