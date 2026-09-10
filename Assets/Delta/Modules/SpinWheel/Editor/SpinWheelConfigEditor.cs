using UnityEngine;
using UnityEditor;

namespace Delta.Modules.SpinWheel.Editor
{
    [CustomEditor(typeof(SpinWheelConfig))]
    public class SpinWheelConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SpinWheelConfig config = (SpinWheelConfig)target;
            
            EditorGUILayout.Space(10);
            
            // Open in Wizard button
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("✨ Open in Setup Wizard", GUILayout.Height(35)))
            {
                SpinWheelSetupWizard window = EditorWindow.GetWindow<SpinWheelSetupWizard>("SpinWheel Setup");
                // Use reflection to set the config field
                var configField = typeof(SpinWheelSetupWizard).GetField("config", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    configField.SetValue(window, config);
                }
                window.Show();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            // Quick stats
            if (config.wheelPieces != null && config.wheelPieces.Length > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Quick Stats", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField($"Pieces: {config.wheelPieces.Length}");
                EditorGUILayout.LabelField($"Unlock Level: {config.unlockLevel}");
                
                float totalChance = 0f;
                foreach (var piece in config.wheelPieces)
                {
                    totalChance += piece.Chance;
                }
                
                GUIStyle chanceStyle = new GUIStyle(GUI.skin.label);
                if (Mathf.Abs(totalChance - 100f) > 0.1f)
                {
                    chanceStyle.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField($"Total Chance: {totalChance:F1}% ⚠", chanceStyle);
                }
                else
                {
                    chanceStyle.normal.textColor = Color.green;
                    EditorGUILayout.LabelField($"Total Chance: {totalChance:F1}% ✓", chanceStyle);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            // Draw default inspector
            DrawDefaultInspector();
        }
    }
}

