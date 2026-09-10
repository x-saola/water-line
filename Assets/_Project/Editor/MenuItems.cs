using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEditor.SceneManagement;

namespace Delta.ProjectName
{
    public static class MenuItems
    {
        private const int PRIORITY_SCENE = 0;
        private const int PRIORITY_DATA = 100;
        private const int PRIORITY_OPEN = 200;
        private const int PRIORITY_EDIT_SCRIPT = 10000;

        # region Data Utilities
        [MenuItem("_Project/Data/Clear Player Pref", false, PRIORITY_DATA)]
        public static void ClearPlayerPref()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("Cleared player preferences!");
        }

        [MenuItem("_Project/Data/Clear Player Pref User Data", false, PRIORITY_DATA + 1)]
        public static void ClearPlayerPrefUserData()
        {
            PlayerPrefs.SetInt("KEY_LEVEL", 11);
            PlayerPrefs.DeleteKey("USER_DATA");
        }

        [MenuItem("_Project/Data/Clear Persistent Data", false, PRIORITY_DATA + 2)]
        public static void ClearPersistentData()
        {
            Debug.Log("Persistent data located at :" + Application.persistentDataPath);
            DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
            dir.Delete(true);
            Debug.Log("Deleted player persistent data!");
        }

        [MenuItem("_Project/Data/Clear Temporary Data", false, PRIORITY_DATA + 3)]
        public static void ClearTempData()
        {
            Debug.Log("Temporary data located at :" + Application.temporaryCachePath);
            DirectoryInfo dir = new DirectoryInfo(Application.temporaryCachePath);
            dir.Delete(true);
            Debug.Log("Deleted player temporary data!");
        }

        [MenuItem("_Project/Data/Clear All", false, PRIORITY_DATA + 4)]
        public static void ClearAll()
        {
            ClearPlayerPref();
            ClearPersistentData();
            ClearTempData();
        }
        #endregion

        [MenuItem("_Project/Memory/UnloadAllUnusedAssets", false)]
        public static void UnloadAllUnusedAsset()
        {
            Debug.Log("Unload all unused assets ");

            Resources.UnloadUnusedAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
        
        #region Open Folder
        [MenuItem("_Project/Open Folder", false, PRIORITY_OPEN)]
        public static void OpenProjectFolderInExplorer()
        {
            string projectFolderPath = Application.dataPath;
            projectFolderPath = System.IO.Path.GetDirectoryName(projectFolderPath);
            if (projectFolderPath != null)
            {
#if UNITY_EDITOR_WIN
                Process.Start("explorer.exe", projectFolderPath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
                Process.Start("open", projectFolderPath);
#elif UNITY_EDITOR_LINUX
                Process.Start("xdg-open", projectFolderPath);
#endif
                Debug.Log("Opened project folder: " + projectFolderPath);
            }
        }

        [MenuItem("_Project/Open Persistent Folder", false, PRIORITY_OPEN + 1)]
        public static void OpenApplicationPersistentDataFolderInExplorer()
        {
            string projectFolderPath = Application.persistentDataPath;

            Debug.LogError(Directory.Exists(projectFolderPath));
            
            if (projectFolderPath != null)
            {
#if UNITY_EDITOR_WIN
                Process.Start("explorer.exe", projectFolderPath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
                Process.Start("open", projectFolderPath);
#elif UNITY_EDITOR_LINUX
                Process.Start("xdg-open", projectFolderPath);
#endif
                Debug.Log("Opened project folder: " + projectFolderPath);
            }
        }
        #endregion

        #region Scene Management
        [MenuItem("_Project/Play #p", false, PRIORITY_SCENE)]
        public static void Play()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            OpenSceneBySceneName("Bootstrap");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("_Project/Scene/Open Bootstrap Scene _1", false, PRIORITY_SCENE + 1)]
        public static void OpenBootstrapScene()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }
            
            OpenSceneBySceneName("Bootstrap");
        }

        [MenuItem("_Project/Scene/Open MainMenu Scene _2", false, PRIORITY_SCENE + 2)]
        public static void OpenMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            OpenSceneBySceneName("MainMenu");
        }

        [MenuItem("_Project/Scene/Open Gameplay Scene _3", false, PRIORITY_SCENE + 3)]
        public static void OpenGameplayScene()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            OpenSceneBySceneName("Gameplay");
        }

        private static void OpenSceneBySceneName(string sceneName)
        {
            string scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            EditorSceneManager.OpenScene(scenePath);
        }

        private static void OpenSceneByIndex(int index)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length <= index)
            {
                Debug.LogError("Scene index out of range: " + index);
                return;
            }

            string scenePath = scenes[index].path;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log("Opened scene: " + scenePath);
            }
        }

        [MenuItem("_Project/Scene/Setup Bootstrap Scene", false, PRIORITY_SCENE + 4)]
        public static void SetupBootstrapScene()
        {
            Debug.Log("Setting up bootstrap scene...");
        }
        #endregion

        [MenuItem("_Project/Edit Script...", false, PRIORITY_EDIT_SCRIPT)]
        public static void OpenScript()
        {
            var currentFilePath = GetCurrentFilePath();
            var currentLineNumber = GetCurrentLineNumber();
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(currentFilePath, currentLineNumber + 1);
        }

        public static string GetCurrentFilePath([CallerFilePath] string filePath = "")
        {
            return filePath;
        }

        public static int GetCurrentLineNumber([CallerLineNumber] int lineNumber = 0)
        {
            return lineNumber;
        }
    }
}
