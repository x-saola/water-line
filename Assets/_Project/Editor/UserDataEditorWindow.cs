using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Delta.Services;

namespace Delta.ProjectName
{
    public class UserDataEditorWindow : EditorWindow
    {
        private const string KEY_USER_DATA = "USER_DATA";

        private UserData _userData;
        private string _rawJson = "";
        private Vector2 _scrollPos;
        private bool _showCurrencies = true;
        private bool _showItems = true;
        private bool _showLevelData = true;
        private bool _showPlayedLevels = false;
        private bool _showRaw = false;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("_Project/User Data Editor")]
        public static void Open()
        {
            var window = GetWindow<UserDataEditorWindow>("User Data Editor");
            window.minSize = new Vector2(420, 500);
            window.Show();
        }

        private void OnEnable()
        {
            Load();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(KEY_USER_DATA))
            {
                _userData = new UserData();
                _userData.Initialize();
                _rawJson = JsonConvert.SerializeObject(_userData, Formatting.Indented);
                SetStatus("No save data found — showing empty UserData.", MessageType.Warning);
                return;
            }

            try
            {
                string base64 = PlayerPrefs.GetString(KEY_USER_DATA);
                byte[] bytes = Convert.FromBase64String(base64);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                _userData = JsonConvert.DeserializeObject<UserData>(json);
                _userData.Initialize();
                _rawJson = JsonConvert.SerializeObject(_userData, Formatting.Indented);
                SetStatus("Loaded successfully.", MessageType.Info);
            }
            catch (Exception e)
            {
                _userData = new UserData();
                _userData.Initialize();
                SetStatus($"Load error: {e.Message}", MessageType.Error);
            }
        }

        private void Save()
        {
            try
            {
                _userData.LastUpdate = DateTime.UtcNow.Ticks;
                string json = JsonConvert.SerializeObject(_userData);
                string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                PlayerPrefs.SetString(KEY_USER_DATA, base64);
                PlayerPrefs.Save();
                _rawJson = JsonConvert.SerializeObject(_userData, Formatting.Indented);
                SetStatus("Saved successfully.", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"Save error: {e.Message}", MessageType.Error);
            }
        }

        private void DeleteSave()
        {
            if (EditorUtility.DisplayDialog("Delete Save Data",
                "This will permanently delete all local UserData from PlayerPrefs. Are you sure?",
                "Delete", "Cancel"))
            {
                PlayerPrefs.DeleteKey(KEY_USER_DATA);
                PlayerPrefs.Save();
                _userData = new UserData();
                _userData.Initialize();
                _rawJson = JsonConvert.SerializeObject(_userData, Formatting.Indented);
                SetStatus("Save data deleted.", MessageType.Warning);
            }
        }

        private void ApplyRawJson()
        {
            try
            {
                _userData = JsonConvert.DeserializeObject<UserData>(_rawJson);
                _userData.Initialize();
                SetStatus("Raw JSON applied — click Save to persist.", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"JSON parse error: {e.Message}", MessageType.Error);
            }
        }

        private void SetStatus(string msg, MessageType type)
        {
            _statusMessage = msg;
            _statusType = type;
        }

        private void OnGUI()
        {
            if (_userData == null) Load();

            DrawToolbar();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, _statusType);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_showRaw)
                DrawRawJson();
            else
                DrawStructured();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Load();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Save();

            if (GUILayout.Button("Delete Save", EditorStyles.toolbarButton, GUILayout.Width(80)))
                DeleteSave();

            GUILayout.FlexibleSpace();

            _showRaw = GUILayout.Toggle(_showRaw, "Raw JSON", EditorStyles.toolbarButton, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStructured()
        {
            EditorGUILayout.Space(4);

            // General
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _userData.HasNoAds = EditorGUILayout.Toggle("Has No Ads", _userData.HasNoAds);

                GUI.enabled = false;
                EditorGUILayout.LongField("Last Update (ticks)", _userData.LastUpdate);
                if (_userData.LastUpdate > 0)
                {
                    var dt = new DateTime(_userData.LastUpdate, DateTimeKind.Utc).ToLocalTime();
                    EditorGUILayout.LabelField("Last Update (local)", dt.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);

            // Currencies
            _showCurrencies = EditorGUILayout.Foldout(_showCurrencies, $"Currencies ({_userData.Currencies?.Count ?? 0})", true, EditorStyles.foldoutHeader);
            if (_showCurrencies)
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawItemList(_userData.Currencies ??= new List<ItemEntry>(), "currency");
            }

            EditorGUILayout.Space(4);

            // Items
            _showItems = EditorGUILayout.Foldout(_showItems, $"Items ({_userData.Items?.Count ?? 0})", true, EditorStyles.foldoutHeader);
            if (_showItems)
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawItemList(_userData.Items ??= new List<ItemEntry>(), "item");
            }

            EditorGUILayout.Space(4);

            // Classic Level Data
            _showLevelData = EditorGUILayout.Foldout(_showLevelData, "Classic Level Data", true, EditorStyles.foldoutHeader);
            if (_showLevelData)
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawLevelModeData(_userData.ClassicLevelModeData ??= new LevelModeData());
            }
        }

        private void DrawItemList(List<ItemEntry> list, string label)
        {
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i].Id = EditorGUILayout.TextField(list[i].Id, GUILayout.MinWidth(120));
                list[i].Quantity = EditorGUILayout.IntField(list[i].Quantity, GUILayout.Width(80));
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    list.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            if (GUILayout.Button($"+ Add {label}", GUILayout.Width(120)))
                list.Add(new ItemEntry { Id = "new_" + label, Quantity = 0 });
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelModeData(LevelModeData data)
        {
            data.CurrentLevel = EditorGUILayout.IntField("Current Level", data.CurrentLevel);

            EditorGUILayout.LabelField($"Played Levels: {data.PlayedLevels?.Count ?? 0}", EditorStyles.miniLabel);

            _showPlayedLevels = EditorGUILayout.Foldout(_showPlayedLevels, "Played Levels", true);
            if (_showPlayedLevels && data.PlayedLevels != null)
            {
                int savedIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                // Header row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Level",    EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Random Level",    EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("IsComplete",     EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("TutorialDone", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Stars",    EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Attempts", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < data.PlayedLevels.Count; i++)
                {
                    var lvl = data.PlayedLevels[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField($"Level {lvl.Level}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Level {lvl.RandomLevel}", GUILayout.Width(80));
                    lvl.IsComplete   = EditorGUILayout.Toggle(lvl.IsComplete,   GUILayout.Width(60));
                    lvl.TutorialDone = EditorGUILayout.Toggle(lvl.TutorialDone, GUILayout.Width(60));
                    lvl.Stars        = EditorGUILayout.IntField(lvl.Stars,      GUILayout.Width(60));
                    lvl.Attempts     = EditorGUILayout.IntField(lvl.Attempts,   GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel = savedIndent;

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8);
                if (GUILayout.Button("Reset All Levels", GUILayout.Width(130)))
                {
                    if (EditorUtility.DisplayDialog("Reset Levels", "Clear all played level data?", "Reset", "Cancel"))
                    {
                        data.PlayedLevels = new List<PlayedLevelData>();
                        data.CurrentLevel = 1;
                    }
                }
                if (GUILayout.Button("Complete All", GUILayout.Width(100)))
                {
                    foreach (var lvl in data.PlayedLevels) { lvl.IsComplete = true; lvl.Stars = 3; }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRawJson()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Raw JSON", EditorStyles.boldLabel);
            _rawJson = EditorGUILayout.TextArea(_rawJson, GUILayout.ExpandHeight(true), GUILayout.MinHeight(300));
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Apply JSON"))
                ApplyRawJson();
        }
    }
}
