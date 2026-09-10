using UnityEngine;
using UnityEditor;

namespace Delta.Modules.Racing.Editor
{
    public class RaceConfigCreator : EditorWindow
    {


        [MenuItem("Tools/Racing/Open Race System")]
        public static void OpenRaceSystem()
        {
            var raceSystem = RaceSystem.Instance;
            Selection.activeGameObject = raceSystem.gameObject;
            EditorGUIUtility.PingObject(raceSystem.gameObject);
        }

        [MenuItem("Tools/Racing/Delete Save Data")]
        public static void DeleteSaveData()
        {
            if (EditorUtility.DisplayDialog("Delete Save Data", 
                "Are you sure you want to delete the Race System save data? This cannot be undone.", 
                "Delete", "Cancel"))
            {
                PlayerPrefs.DeleteKey("RaceSystem_Data");
                PlayerPrefs.DeleteKey("Racing_TutorialCompleted");
                PlayerPrefs.Save();
                Debug.Log("[RaceSystem] Save data deleted.");
                
                // If game is playing, reset the system
                if (Application.isPlaying && RaceSystem.Instance != null)
                {
                    //RaceSystem.Instance.TestResetData();
                }
            }
        }

        [MenuItem("Tools/Racing/Increment Player Progress")]
        public static void IncrementPlayerProgress()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RaceSystem] This cheat only works in play mode.");
                return;
            }

            if (RaceSystem.Instance == null)
            {
                Debug.LogWarning("[RaceSystem] RaceSystem instance not found.");
                return;
            }

            RaceSystem.Instance.HandlePlayerWon();
            Debug.Log("[RaceSystem] Player progress incremented by 1.");
        }
    }
}
