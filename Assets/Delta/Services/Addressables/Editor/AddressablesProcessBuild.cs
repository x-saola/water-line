#if USE_ADDRESSABLES
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Delta.Services
{
    public class AddressablesProcessBuild : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            PrepareLocalAddressables();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanUpPrebuiltLocalAddressables();
        }

        public void PrepareLocalAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var activeProfile = settings.profileSettings.GetProfileName(settings.activeProfileId);

            var pathState = Path.Combine("Assets/AddressableAssetsData", activeProfile, EditorUserBuildSettings.activeBuildTarget.ToString());
            settings.ContentStateBuildPath = pathState;

            var build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
            var builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;
            if (builderScript != null)
            {
                AddressableAssetSettings.CleanPlayerContent(builderScript);
            }
            BuildCache.PurgeCache(false);

            var libraryCachePath = Path.Combine("Library/com.unity.addressables/aa", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (Directory.Exists(libraryCachePath))
            {
                Directory.Delete(libraryCachePath, true);
            }

            var sourcePath = Path.Combine("PrebuiltLocalAddressables", activeProfile, EditorUserBuildSettings.activeBuildTarget.ToString());
            if (Directory.Exists(sourcePath))
            {
                Athena.Common.Utils.AthenaUtils.CopyDirectory(sourcePath, libraryCachePath, true);
            }
        }

        public void CleanUpPrebuiltLocalAddressables()
        {
            // 1. Clean StreamingAssets
            var streamingAssetsPath = "Assets/StreamingAssets/aa/";
            if (Directory.Exists(streamingAssetsPath))
            {
                Directory.Delete(streamingAssetsPath, true);
                
                var metaPath = streamingAssetsPath.TrimEnd('/') + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }

            // 2. Restore Library State (Optional but good for Dev experience)
            // This puts the files back into Library so you can hit "Play" in Editor immediately after build without errors.
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var activeProfile = settings.profileSettings.GetProfileName(settings.activeProfileId);
            var sourcePath = Path.Combine("PrebuiltLocalAddressables", activeProfile, EditorUserBuildSettings.activeBuildTarget.ToString());
            if (Directory.Exists(sourcePath))
            {
                var libraryCachePath = Path.Combine("Library/com.unity.addressables/aa", EditorUserBuildSettings.activeBuildTarget.ToString());
                if (Directory.Exists(libraryCachePath))
                {
                    Directory.Delete(libraryCachePath, true);
                }

                Athena.Common.Utils.AthenaUtils.CopyDirectory(sourcePath, libraryCachePath, true);
                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif
