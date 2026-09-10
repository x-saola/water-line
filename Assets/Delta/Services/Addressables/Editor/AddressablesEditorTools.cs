using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;
using UnityEditor.AddressableAssets;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

namespace Delta.Services
{
    public static class AddressablesEditorTools
    {
        [MenuItem("Tools/Addressables/Build Local Addressables", false, 1)]
        public static void BuildLocalAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var activeProfile = settings.profileSettings.GetProfileName(settings.activeProfileId);

            var textAsset = Resources.Load<TextAsset>("PlayerAssetsVersion");
            var playerAssetsVerion = JsonUtility.FromJson<PlayerAssetsVersion>(textAsset.text);
            var profileVersion = playerAssetsVerion.GetProfile(activeProfile);
            if (profileVersion == null)
            {
                profileVersion = new PlayerAssetsVersion.ProfileAssetsVersion()
                {
                    Name = activeProfile
                };
                playerAssetsVerion.Profiles.Add(profileVersion);
            }
            playerAssetsVerion.ActiveProfile = activeProfile;

            var now = System.DateTime.Now;
            profileVersion.AssetsVersion = $"{now.Year}{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}";

            using (var versionFile = File.CreateText("Assets/Dynamic/Resources/PlayerAssetsVersion.bytes"))
            {
                versionFile.Write(JsonUtility.ToJson(playerAssetsVerion, true));
            }
            AssetDatabase.Refresh();

            var pathState = "Assets/AddressableAssetsData";
            if (activeProfile != "Dev")
                pathState = Path.Combine(pathState, activeProfile);
            settings.ContentStateBuildPath = pathState;

            var build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
            var builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builderScript);
            settings.ActivePlayerDataBuilderIndex = index;

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            // Update prebuilt addressables
            if (success)
            {
                var targetDir = "PrebuiltLocalAddressables/" + activeProfile + "/" + EditorUserBuildSettings.activeBuildTarget.ToString();
                if (Directory.Exists(targetDir))
                    Directory.Delete(targetDir, true);

                var sourceDir = "Library/com.unity.addressables/aa/" + EditorUserBuildSettings.activeBuildTarget.ToString();
                Common.AtomUtils.CopyDirectory(sourceDir, targetDir, true);

                // Deploy
                // string profileId = settings.profileSettings.GetProfileId(activeProfile);
                // sourceDir = settings.profileSettings.EvaluateString(profileId, settings.profileSettings.GetValueByName(profileId, "Remote.BuildPath"));

                // targetDir = GetDeployDir(activeProfile);
                // Utils.AthenaUtils.CopyDirectory(sourceDir, targetDir, true);
            }

            settings.ContentStateBuildPath = string.Empty;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Addressables/Build Content Updates", false, 2)]
        public static void BuildContentUpdates()
        {
            // AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Refreshing assets...");
            AssetDatabase.Refresh();
            Debug.Log("Refreshing assets done!");

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var activeProfile = settings.profileSettings.GetProfileName(settings.activeProfileId);
            Debug.Log("Addressable profile: " + activeProfile);

            var pathState = string.Empty;
            if (activeProfile != "Dev")
            {
                pathState = System.IO.Path.Combine("Assets/AddressableAssetsData/" + activeProfile, EditorUserBuildSettings.activeBuildTarget.ToString());
                pathState = System.IO.Path.Combine(pathState, "addressables_content_state.bin");
            }
            settings.ContentStateBuildPath = pathState;
            Debug.Log("ContentStateBuildPath: " + settings.ContentStateBuildPath);

            string profileId = settings.profileSettings.GetProfileId(activeProfile);
            settings.activeProfileId = profileId;

            var build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
            var builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builderScript);
            settings.ActivePlayerDataBuilderIndex = index;

            var cacheState = pathState;
            if (string.IsNullOrEmpty(cacheState))
            {
                cacheState = System.IO.Path.Combine("Assets/AddressableAssetsData", EditorUserBuildSettings.activeBuildTarget.ToString());
                cacheState = System.IO.Path.Combine(cacheState, "addressables_content_state.bin");
            }

            var entries = ContentUpdateScript.GatherModifiedEntries(settings, cacheState);
            Debug.Log("Updated entries count: " + entries.Count);
            if (entries.Count == 0)
                return;

            var groupName = "ContentUpdate-" + System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ContentUpdateScript.CreateContentUpdateGroup(settings, entries, groupName);

            var updateGroup = settings.groups.Find(delegate (AddressableAssetGroup g)
            {
                return g.Name == groupName;
            });
            foreach (var entry in updateGroup.entries)
            {
                FixAddressableEntry(entry, updateGroup.Name.ToLower());
            }

            var bundleSchema = updateGroup.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
            bundleSchema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
            bundleSchema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
            EditorUtility.SetDirty(bundleSchema);

            var updateSchema = updateGroup.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
            updateSchema.StaticContent = true;
            EditorUtility.SetDirty(updateSchema);

            EditorUtility.SetDirty(updateGroup);
            AssetDatabase.SaveAssets();

            var result = ContentUpdateScript.BuildContentUpdate(settings, cacheState);
            bool success = string.IsNullOrEmpty(result.Error);
            Debug.Log("BuildContentUpdates success: " + success);

            // // Deploy
            // if (success)
            // {
            //     var sourceDir = settings.profileSettings.EvaluateString(profileId, settings.profileSettings.GetValueByName(profileId, "Remote.BuildPath"));
            //     Debug.Log("sourceDir: " + sourceDir);

            //     var remotePath = settings.profileSettings.EvaluateString(profileId, settings.profileSettings.GetValueByName(profileId, "Remote.LoadPath"));
            //     remotePath = UnityEngine.AddressableAssets.Initialization.AddressablesRuntimeProperties.EvaluateString(remotePath);
            //     Debug.Log("remotePath: " + remotePath);

            //     var targetDir = GetDeployDir(activeProfile);
            //     Debug.Log("targetDir: " + targetDir);

            //     Athena.Common.Utils.AthenaUtils.CopyDirectory(sourceDir, targetDir, true);

            //     Debug.Log("Deploy done at: " + targetDir);
            // }
        }

        private static string GetDeployDir(string profile)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var activeProfile = settings.profileSettings.GetProfileName(settings.activeProfileId);
            string profileId = settings.profileSettings.GetProfileId(activeProfile);

            var remotePath = settings.profileSettings.EvaluateString(profileId, settings.profileSettings.GetValueByName(profileId, "Remote.LoadPath"));
            Debug.Log("remotePath1: " + remotePath);
            remotePath = UnityEngine.AddressableAssets.Initialization.AddressablesRuntimeProperties.EvaluateString(remotePath);
            Debug.Log("remotePath: " + remotePath);

            var subIdx = remotePath.IndexOf(".com/") + ".com/".Length;
            var targetDir = (profile == "Prod" ? "cdn-prod/public/" : "cdn/public/") + remotePath.Substring(subIdx);
            return targetDir;
        }

        private static void FixAddressableEntry(AddressableAssetEntry entry, string label)
        {
            Debug.Log("address: " + entry.address + " - folder: " + entry.IsFolder + " - assetPath: " + entry.AssetPath);
            entry.SetAddress(entry.AssetPath);
            entry.SetLabel(label, true, true);
            if (entry.SubAssets == null)
                return;

            foreach (var subEntry in entry.SubAssets)
            {
                FixAddressableEntry(subEntry, label);
            }
        }

        [MenuItem("Tools/Addressables/Clear Cache", false, 20)]
        public static void ClearCache()
        {
            if (Caching.ClearCache())
            {
                Debug.LogWarning("Cleaned all cache!");
            }
            else
            {
                Debug.LogWarning("Cache was in use");
            }
        }

#region PLAYER ASSETS VERSION
        [type: UnityEngine.Scripting.Preserve, System.Serializable]
        public class PlayerAssetsVersion
        {
            [System.Serializable]
            public class ProfileAssetsVersion
            {
                public string Name;
                public string AssetsVersion;
            }

            static string _version;

            [property: UnityEngine.Scripting.Preserve]
            public static string Version
            {
                get
                {
                    if (string.IsNullOrEmpty(_version))
                    {
                        var instance = JsonUtility.FromJson<PlayerAssetsVersion>(Resources.Load<TextAsset>("PlayerAssetsVersion").text);
                        var profile = instance.GetProfile(instance.ActiveProfile);
                        _version = profile.AssetsVersion;
                    }

                    return _version;
                }
            }

            public string ActiveProfile;
            public List<ProfileAssetsVersion> iOS = new();
            public List<ProfileAssetsVersion> Android = new();

            public List<ProfileAssetsVersion> Profiles
            {
                get
                {
        #if UNITY_IOS
                    return iOS;
        #elif UNITY_ANDROID
                    return Android;
        #endif
                }
            }

            public ProfileAssetsVersion GetProfile(string profileName)
            {
                foreach (var profile in Profiles)
                {
                    if (profile.Name == profileName)
                        return profile;
                }

                return null;
            }
        }
#endregion

        [MenuItem("Tools/Addressables/Edit Script...", false, 10000)]
        public static void OpenScript()
        {
            var currentFilePath = GetCurrentFilePath();
            var currentLineNumber = GetCurrentLineNumber();
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(currentFilePath, 1);
        }

        private static string GetCurrentFilePath([CallerFilePath] string filePath = "")
        {
            return filePath;
        }

        private static int GetCurrentLineNumber([CallerLineNumber] int lineNumber = 0)
        {
            return lineNumber;
        }
    }
}
