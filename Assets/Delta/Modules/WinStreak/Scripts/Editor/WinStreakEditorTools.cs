using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Delta.Modules.WinStreak
{   
    public class WinStreakEditorTools : EditorWindow
    {
        [MenuItem("Tools/WinStreak/Delete Save Data")]
        public static void DeleteSaveData()
        {
            if (EditorUtility.DisplayDialog("Delete Save Data", 
                "Are you sure you want to delete the Win Streak System save data? This cannot be undone.", 
                "Delete", "Cancel"))
            {
                PlayerPrefs.DeleteKey("WinStreakSystem_Data");
                PlayerPrefs.Save();
                Debug.Log("[WinStreakSystem] Save data deleted.");
            }
        }
    }
}
