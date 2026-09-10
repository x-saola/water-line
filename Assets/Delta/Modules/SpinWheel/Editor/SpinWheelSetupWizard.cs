using UnityEngine;
using UnityEditor;
using Delta.Modules.SpinWheel;
using System.Collections.Generic;

namespace Delta.Modules.SpinWheel.Editor
{
    public class SpinWheelSetupWizard : EditorWindow
    {
        private SpinWheelConfig config;
        private SerializedObject serializedConfig;
        private Vector2 scrollPosition;
        
        // UI State
        private bool showValidation = true;
        private int selectedPieceIndex = -1;
        
        [MenuItem("Tools/SpinWheel/Setup Wizard")]
        public static void ShowWindow()
        {
            SpinWheelSetupWizard window = GetWindow<SpinWheelSetupWizard>("SpinWheel Setup");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        [MenuItem("Tools/SpinWheel/Clear PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            const string PrefKeyDailySpins = "SpinWheel_DailySpins";
            const string PrefKeyLastSpinDate = "SpinWheel_LastSpinDate";
            
            if (UnityEditor.EditorUtility.DisplayDialog(
                "Clear SpinWheel PlayerPrefs",
                "This will delete all SpinWheel saved data:\n- Daily spin count\n- Last spin date\n\nAre you sure?",
                "Yes, Clear Data",
                "Cancel"))
            {
                PlayerPrefs.DeleteKey(PrefKeyDailySpins);
                PlayerPrefs.DeleteKey(PrefKeyLastSpinDate);
                PlayerPrefs.Save();
                
                UnityEngine.Debug.Log("[SpinWheel] PlayerPrefs cleared successfully!");
                UnityEditor.EditorUtility.DisplayDialog(
                    "Success",
                    "SpinWheel PlayerPrefs have been cleared.",
                    "OK");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawConfigManagement();
            
            if (config != null)
            {
                EditorGUILayout.Space(10);
                DrawWheelConfiguration();
                
                EditorGUILayout.Space(10);
                DrawWheelPiecesEditor();
                
                EditorGUILayout.Space(10);
                DrawValidationAndHelpers();
            }
            
            // Auto-save on changes
            if (GUI.changed && config != null)
            {
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("SpinWheel Setup Wizard", headerStyle);
            EditorGUILayout.LabelField("Configure your wheel pieces, rewards, and probabilities", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawConfigManagement()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Config Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            SpinWheelConfig newConfig = (SpinWheelConfig)EditorGUILayout.ObjectField("Config", config, typeof(SpinWheelConfig), false);
            
            if (newConfig != config)
            {
                config = newConfig;
                if (config != null)
                {
                    serializedConfig = new SerializedObject(config);
                }
            }
            
            if (GUILayout.Button("Create New", GUILayout.Width(100)))
            {
                CreateNewConfig();
            }
            EditorGUILayout.EndHorizontal();
            
            if (config != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Path: {AssetDatabase.GetAssetPath(config)}", EditorStyles.miniLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(config);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawWheelConfiguration()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Wheel Configuration", EditorStyles.boldLabel);
            
            // Unlock Level
            config.unlockLevel = EditorGUILayout.IntSlider("Unlock Level", config.unlockLevel, 0, 100);
            
            // Number of pieces
            int currentPieces = config.wheelPieces != null ? config.wheelPieces.Length : 0;
            EditorGUILayout.LabelField($"Current Pieces: {currentPieces}");
            
            // Quick preset buttons
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("4 Pieces")) CreatePreset(4);
            if (GUILayout.Button("6 Pieces")) CreatePreset(6);
            if (GUILayout.Button("8 Pieces")) CreatePreset(8);
            if (GUILayout.Button("12 Pieces")) CreatePreset(12);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawWheelPiecesEditor()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wheel Pieces", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Piece", GUILayout.Width(100)))
            {
                AddNewPiece();
            }
            EditorGUILayout.EndHorizontal();
            
            if (config.wheelPieces == null || config.wheelPieces.Length == 0)
            {
                EditorGUILayout.HelpBox("No wheel pieces configured. Click 'Add Piece' or use a quick preset.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.Space(5);
            
            // Scrollable area for pieces
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            for (int i = 0; i < config.wheelPieces.Length; i++)
            {
                DrawWheelPiece(i);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawWheelPiece(int index)
        {
            WheelPiece piece = config.wheelPieces[index];
            
            EditorGUILayout.BeginVertical("box");
            
            // Header with piece number and delete button
            EditorGUILayout.BeginHorizontal();
            bool foldout = selectedPieceIndex == index;
            if (GUILayout.Button(foldout ? "▼" : "▶", GUILayout.Width(25)))
            {
                selectedPieceIndex = foldout ? -1 : index;
            }
            
            EditorGUILayout.LabelField($"Piece {index + 1}", EditorStyles.boldLabel);
            
            if (piece.reward != null && piece.reward.icon != null)
            {
                GUILayout.Box(piece.reward.icon.texture, GUILayout.Width(30), GUILayout.Height(30));
            }
            
            EditorGUILayout.LabelField($"{piece.Chance:F1}%", GUILayout.Width(50));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
            {
                DuplicatePiece(index);
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                DeletePiece(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // Expanded view
            if (selectedPieceIndex == index)
            {
                EditorGUI.indentLevel++;
                
                piece.id = EditorGUILayout.IntField("ID", piece.id);
                
                // Reward section
                EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
                if (piece.reward == null)
                {
                    piece.reward = new SpinWheelReward(0, 0, null, "");
                }
                
                EditorGUI.indentLevel++;
                piece.reward.icon = (Sprite)EditorGUILayout.ObjectField("Icon", piece.reward.icon, typeof(Sprite), false);
                piece.reward.label = EditorGUILayout.TextField("Label", piece.reward.label);
                piece.reward.amount = EditorGUILayout.IntField("Amount", piece.reward.amount);
                piece.reward.rewardId = EditorGUILayout.IntField("Reward ID", piece.reward.rewardId);
                EditorGUI.indentLevel--;
                
                // Chance slider
                piece.Chance = EditorGUILayout.Slider("Chance %", piece.Chance, 0f, 100f);
                
                // Colors
                EditorGUILayout.LabelField("Gradient Colors", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                piece.CenterColor = EditorGUILayout.ColorField("Center Color", piece.CenterColor);
                piece.EdgeColor = EditorGUILayout.ColorField("Edge Color", piece.EdgeColor);
                EditorGUI.indentLevel--;
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawValidationAndHelpers()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Validation & Helpers", EditorStyles.boldLabel);
            
            if (config.wheelPieces != null && config.wheelPieces.Length > 0)
            {
                // Calculate total chance
                float totalChance = 0f;
                foreach (var piece in config.wheelPieces)
                {
                    totalChance += piece.Chance;
                }
                
                // Display total
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Total Chance: {totalChance:F1}%");
                
                // Show warning or success
                if (Mathf.Abs(totalChance - 100f) > 0.1f)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("⚠ Should equal 100%", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✓ Perfect!", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();
                
                // Warning messages
                if (Mathf.Abs(totalChance - 100f) > 0.1f)
                {
                    EditorGUILayout.HelpBox("Total chance should equal 100%. Use 'Normalize Chances' to auto-adjust.", MessageType.Warning);
                }
                
                // Check if all pieces have 0% chance
                bool allZero = true;
                foreach (var piece in config.wheelPieces)
                {
                    if (piece.Chance > 0) allZero = false;
                }
                
                if (allZero)
                {
                    EditorGUILayout.HelpBox("All pieces have 0% chance! At least one piece needs a chance > 0.", MessageType.Error);
                }
                
                // Helper buttons
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Quick Actions:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Normalize Chances"))
                {
                    NormalizeChances();
                }
                
                if (GUILayout.Button("Set Equal Chances"))
                {
                    SetEqualChances();
                }
                
                if (GUILayout.Button("Randomize Colors"))
                {
                    RandomizeColors();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Visual preview
                DrawChancePreview();
            }
            else
            {
                EditorGUILayout.HelpBox("No pieces to validate.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawChancePreview()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Chance Distribution:", EditorStyles.miniLabel);
            
            Rect rect = GUILayoutUtility.GetRect(100, 20);
            float xPos = rect.x;
            
            foreach (var piece in config.wheelPieces)
            {
                float width = rect.width * (piece.Chance / 100f);
                if (width > 0)
                {
                    Rect pieceRect = new Rect(xPos, rect.y, width, rect.height);
                    EditorGUI.DrawRect(pieceRect, piece.CenterColor);
                    xPos += width;
                }
            }
        }

        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create SpinWheel Config",
                "NewSpinWheelConfig",
                "asset",
                "Choose where to save the config"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                SpinWheelConfig newConfig = CreateInstance<SpinWheelConfig>();
                newConfig.unlockLevel = 1;
                newConfig.wheelPieces = new WheelPiece[0];
                
                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                
                config = newConfig;
                serializedConfig = new SerializedObject(config);
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = config;
            }
        }

        private void CreatePreset(int pieceCount)
        {
            if (config == null) return;
            
            Undo.RecordObject(config, "Create Preset");
            
            config.wheelPieces = new WheelPiece[pieceCount];
            float chancePerPiece = 100f / pieceCount;
            
            Color[] presetColors = new Color[]
            {
                new Color(1f, 0.3f, 0.3f),
                new Color(0.3f, 0.6f, 1f),
                new Color(1f, 0.8f, 0.2f),
                new Color(0.4f, 1f, 0.4f),
                new Color(1f, 0.5f, 0.9f),
                new Color(0.9f, 0.6f, 0.2f),
                new Color(0.6f, 0.3f, 1f),
                new Color(0.3f, 1f, 1f),
            };
            
            for (int i = 0; i < pieceCount; i++)
            {
                Color centerColor = presetColors[i % presetColors.Length];
                Color edgeColor = centerColor * 0.7f;
                
                config.wheelPieces[i] = new WheelPiece
                {
                    id = i + 1,
                    Chance = chancePerPiece,
                    CenterColor = centerColor,
                    EdgeColor = edgeColor,
                    reward = new SpinWheelReward(i + 1, 100, null, $"Reward {i + 1}")
                };
            }
            
            EditorUtility.SetDirty(config);
        }

        private void AddNewPiece()
        {
            if (config == null) return;
            
            Undo.RecordObject(config, "Add Wheel Piece");
            
            WheelPiece newPiece = new WheelPiece
            {
                id = config.wheelPieces != null ? config.wheelPieces.Length + 1 : 1,
                Chance = 0f,
                CenterColor = Color.white,
                EdgeColor = Color.gray,
                reward = new SpinWheelReward(1, 100, null, "New Reward")
            };
            
            if (config.wheelPieces == null)
            {
                config.wheelPieces = new WheelPiece[] { newPiece };
            }
            else
            {
                List<WheelPiece> pieces = new List<WheelPiece>(config.wheelPieces);
                pieces.Add(newPiece);
                config.wheelPieces = pieces.ToArray();
            }
            
            selectedPieceIndex = config.wheelPieces.Length - 1;
            EditorUtility.SetDirty(config);
        }

        private void DeletePiece(int index)
        {
            if (config == null || config.wheelPieces == null) return;
            
            Undo.RecordObject(config, "Delete Wheel Piece");
            
            List<WheelPiece> pieces = new List<WheelPiece>(config.wheelPieces);
            pieces.RemoveAt(index);
            config.wheelPieces = pieces.ToArray();
            
            if (selectedPieceIndex >= config.wheelPieces.Length)
            {
                selectedPieceIndex = -1;
            }
            
            EditorUtility.SetDirty(config);
        }

        private void DuplicatePiece(int index)
        {
            if (config == null || config.wheelPieces == null) return;
            
            Undo.RecordObject(config, "Duplicate Wheel Piece");
            
            WheelPiece original = config.wheelPieces[index];
            WheelPiece duplicate = new WheelPiece
            {
                id = original.id,
                Chance = original.Chance,
                CenterColor = original.CenterColor,
                EdgeColor = original.EdgeColor,
                reward = new SpinWheelReward(
                    original.reward.rewardId,
                    original.reward.amount,
                    original.reward.icon,
                    original.reward.label
                )
            };
            
            List<WheelPiece> pieces = new List<WheelPiece>(config.wheelPieces);
            pieces.Insert(index + 1, duplicate);
            config.wheelPieces = pieces.ToArray();
            
            EditorUtility.SetDirty(config);
        }

        private void NormalizeChances()
        {
            if (config == null || config.wheelPieces == null) return;
            
            Undo.RecordObject(config, "Normalize Chances");
            
            float totalChance = 0f;
            foreach (var piece in config.wheelPieces)
            {
                totalChance += piece.Chance;
            }
            
            if (totalChance > 0)
            {
                float multiplier = 100f / totalChance;
                foreach (var piece in config.wheelPieces)
                {
                    piece.Chance *= multiplier;
                }
            }
            
            EditorUtility.SetDirty(config);
        }

        private void SetEqualChances()
        {
            if (config == null || config.wheelPieces == null || config.wheelPieces.Length == 0) return;
            
            Undo.RecordObject(config, "Set Equal Chances");
            
            float equalChance = 100f / config.wheelPieces.Length;
            foreach (var piece in config.wheelPieces)
            {
                piece.Chance = equalChance;
            }
            
            EditorUtility.SetDirty(config);
        }

        private void RandomizeColors()
        {
            if (config == null || config.wheelPieces == null) return;
            
            Undo.RecordObject(config, "Randomize Colors");
            
            foreach (var piece in config.wheelPieces)
            {
                piece.CenterColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
                piece.EdgeColor = piece.CenterColor * 0.7f;
            }
            
            EditorUtility.SetDirty(config);
        }
    }
}

