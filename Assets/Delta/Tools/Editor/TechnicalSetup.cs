using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Delta.Tools.Editor
{
    public class TechnicalSetup : EditorWindow
    {
        private const string CONFIG_ASSET_PATH = "Assets/Delta/GameOps/Resources/app_configs.bytes";
        private const string GOOGLE_MOBILE_ADS_SETTINGS_PATH = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string GOOGLE_SERVICES_JSON_PATH = "Assets/google-services.json";
        private const string GOOGLE_SERVICE_INFO_PLIST_PATH = "Assets/GoogleService-Info.plist";
        private const string GOOGLE_SERVICES_DESKTOP_JSON_PATH = "Assets/StreamingAssets/google-services-desktop.json";

        private string _productName = "";
        private string _androidPackageName = "";
        private string _iosBundleId = "";
        private string _iosAppId = "";

        private string _adjustAndroidToken = "";
        private string _adjustIosToken = "";

        private string _maxSdkKey = "";
        private string _maxAndroidBannerAdId = "";
        private string _maxAndroidInterstitialAdId = "";
        private string _maxAndroidRewardedAdId = "";
        private string _maxIosBannerAdId = "";
        private string _maxIosInterstitialAdId = "";
        private string _maxIosRewardedAdId = "";

        private string _admobAndroidAppId = "";
        private string _admobIosAppId = "";

        private string _googleServicesJson;
        private string _googleServiceInfoPlist;

        private static readonly string[] TabNames = { "Android", "iOS" };
        private int _selectedTab;

        [MenuItem("Tools/Delta/Technical Setup #t")]
        public static void Open()
        {
            var window = GetWindow<TechnicalSetup>("Technical Setup");
            window.minSize = new Vector2(420, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadValues();
        }

        private void LoadValues()
        {
            _productName = PlayerSettings.productName ?? "";
            _androidPackageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) ?? "";
            _iosBundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) ?? "";

            var config = new AppConfigFile(CONFIG_ASSET_PATH);
            _iosAppId = config.GetValue("General", "ios_app_id") ?? "";

            _adjustAndroidToken = config.GetValue("Adjust", "android_app_token") ?? "";
            _adjustIosToken = config.GetValue("Adjust", "ios_app_token") ?? "";

            _maxSdkKey = AppLovinSettings.Instance.SdkKey ?? "";
            _maxAndroidBannerAdId = config.GetValue("MAX", "MAX_android_banner_ad_id") ?? "";
            _maxAndroidInterstitialAdId = config.GetValue("MAX", "MAX_android_interstitial_ad_id") ?? "";
            _maxAndroidRewardedAdId = config.GetValue("MAX", "MAX_android_rewarded_ad_id") ?? "";
            _maxIosBannerAdId = config.GetValue("MAX", "MAX_ios_banner_ad_id") ?? "";
            _maxIosInterstitialAdId = config.GetValue("MAX", "MAX_ios_interstitial_ad_id") ?? "";
            _maxIosRewardedAdId = config.GetValue("MAX", "MAX_ios_rewarded_ad_id") ?? "";

            _admobAndroidAppId = AppLovinSettings.Instance.AdMobAndroidAppId ?? "";
            _admobIosAppId = AppLovinSettings.Instance.AdMobIosAppId ?? "";

            _googleServicesJson = LoadTextFile(GOOGLE_SERVICES_JSON_PATH);
            _googleServiceInfoPlist = LoadTextFile(GOOGLE_SERVICE_INFO_PLIST_PATH);
        }

        private static string LoadTextFile(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space(8);
            DrawCommonSection();

            EditorGUILayout.Space(8);
            DrawPlatformTabs();
        }

        private void DrawPlatformTabs()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            _selectedTab = GUILayout.Toolbar(_selectedTab, TabNames);
            EditorGUILayout.Space(8);

            if (_selectedTab == 0)
                DrawAndroidTab();
            else
                DrawIosTab();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
                LoadValues();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SaveValues();
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCommonSection()
        {
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _productName = EditorGUILayout.TextField("Product Name", _productName);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("MAX", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _maxSdkKey = EditorGUILayout.TextField("SDK Key", _maxSdkKey);
            }
        }

        private void DrawAndroidTab()
        {
            _androidPackageName = EditorGUILayout.TextField("Package Name", _androidPackageName);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Adjust", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _adjustAndroidToken = EditorGUILayout.TextField("App Token", _adjustAndroidToken);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("MAX", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _maxAndroidBannerAdId = EditorGUILayout.TextField("Banner Ad Id", _maxAndroidBannerAdId);
                _maxAndroidInterstitialAdId = EditorGUILayout.TextField("Interstitial Ad Id", _maxAndroidInterstitialAdId);
                _maxAndroidRewardedAdId = EditorGUILayout.TextField("Rewarded Ad Id", _maxAndroidRewardedAdId);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("AdMob", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _admobAndroidAppId = EditorGUILayout.TextField("App Id", _admobAndroidAppId);
            }

            EditorGUILayout.Space(8);
            DrawFirebaseSectionAndroid();
        }

        private void DrawFirebaseSectionAndroid()
        {
            EditorGUILayout.LabelField("Firebase", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                if (_googleServicesJson != null)
                {
                    string jsonPackageName = ExtractJsonStringValue(_googleServicesJson, "package_name");

                    DrawSelectableField("Project Id", ExtractJsonStringValue(_googleServicesJson, "project_id"));
                    DrawSelectableField("Storage Bucket", ExtractJsonStringValue(_googleServicesJson, "storage_bucket"));
                    DrawSelectableField("Package Name", jsonPackageName);

                    if (!string.IsNullOrEmpty(jsonPackageName) && jsonPackageName != _androidPackageName)
                        EditorGUILayout.HelpBox($"package_name in google-services.json ({jsonPackageName}) does not match Package Name ({_androidPackageName}).", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"{GOOGLE_SERVICES_JSON_PATH} not found.", MessageType.Warning);
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Import / Replace google-services.json...", GUILayout.Width(260)))
                    ImportGoogleServicesJson();
            }
        }

        private void DrawFirebaseSectionIos()
        {
            EditorGUILayout.LabelField("Firebase", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                if (_googleServiceInfoPlist != null)
                {
                    string plistBundleId = ExtractPlistStringValue(_googleServiceInfoPlist, "BUNDLE_ID");

                    DrawSelectableField("Project Id", ExtractPlistStringValue(_googleServiceInfoPlist, "PROJECT_ID"));
                    DrawSelectableField("Storage Bucket", ExtractPlistStringValue(_googleServiceInfoPlist, "STORAGE_BUCKET"));
                    DrawSelectableField("Bundle Id", plistBundleId);

                    if (!string.IsNullOrEmpty(plistBundleId) && plistBundleId != _iosBundleId)
                        EditorGUILayout.HelpBox($"BUNDLE_ID in GoogleService-Info.plist ({plistBundleId}) does not match Bundle Id ({_iosBundleId}).", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"{GOOGLE_SERVICE_INFO_PLIST_PATH} not found.", MessageType.Warning);
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Import / Replace GoogleService-Info.plist...", GUILayout.Width(280)))
                    ImportGoogleServiceInfoPlist();
            }
        }

        private static void DrawSelectableField(string label, string value)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            Rect fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            EditorGUI.SelectableLabel(fieldRect, value ?? string.Empty);
        }

        private void ImportGoogleServicesJson()
        {
            if (!TryImportFile("Select google-services.json", "json", GOOGLE_SERVICES_JSON_PATH))
                return;

            _googleServicesJson = LoadTextFile(GOOGLE_SERVICES_JSON_PATH);

            // Stale desktop config from the old google-services.json — delete so Firebase regenerates it.
            AssetDatabase.DeleteAsset(GOOGLE_SERVICES_DESKTOP_JSON_PATH);

            ShowNotification(new GUIContent("google-services.json imported"));
        }

        private void ImportGoogleServiceInfoPlist()
        {
            if (!TryImportFile("Select GoogleService-Info.plist", "plist", GOOGLE_SERVICE_INFO_PLIST_PATH))
                return;

            _googleServiceInfoPlist = LoadTextFile(GOOGLE_SERVICE_INFO_PLIST_PATH);
            ShowNotification(new GUIContent("GoogleService-Info.plist imported"));
        }

        private static bool TryImportFile(string dialogTitle, string extension, string destAssetPath)
        {
            string sourcePath = EditorUtility.OpenFilePanel(dialogTitle, "", extension);
            if (string.IsNullOrEmpty(sourcePath))
                return false;

            File.Copy(sourcePath, Path.GetFullPath(destAssetPath), true);
            AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        // Light manual extraction (no JSON/plist dependency) — only needs to pull one known string value.
        private static string ExtractJsonStringValue(string json, string key)
        {
            string keyToken = $"\"{key}\"";
            int keyIndex = json.IndexOf(keyToken, System.StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            int colonIndex = json.IndexOf(':', keyIndex + keyToken.Length);
            if (colonIndex < 0)
                return null;

            int valueStart = json.IndexOf('"', colonIndex + 1) + 1;
            if (valueStart <= 0)
                return null;

            int valueEnd = json.IndexOf('"', valueStart);
            return valueEnd < 0 ? null : json.Substring(valueStart, valueEnd - valueStart);
        }

        private static string ExtractPlistStringValue(string plistXml, string key)
        {
            string keyTag = $"<key>{key}</key>";
            int keyIndex = plistXml.IndexOf(keyTag, System.StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            int valueStart = plistXml.IndexOf("<string>", keyIndex + keyTag.Length, System.StringComparison.Ordinal);
            if (valueStart < 0)
                return null;
            valueStart += "<string>".Length;

            int valueEnd = plistXml.IndexOf("</string>", valueStart, System.StringComparison.Ordinal);
            return valueEnd < 0 ? null : plistXml.Substring(valueStart, valueEnd - valueStart);
        }

        private void DrawIosTab()
        {
            _iosBundleId = EditorGUILayout.TextField("Bundle Id", _iosBundleId);
            _iosAppId = EditorGUILayout.TextField("App Id", _iosAppId);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Adjust", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _adjustIosToken = EditorGUILayout.TextField("App Token", _adjustIosToken);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("MAX", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _maxIosBannerAdId = EditorGUILayout.TextField("Banner Ad Id", _maxIosBannerAdId);
                _maxIosInterstitialAdId = EditorGUILayout.TextField("Interstitial Ad Id", _maxIosInterstitialAdId);
                _maxIosRewardedAdId = EditorGUILayout.TextField("Rewarded Ad Id", _maxIosRewardedAdId);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("AdMob", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                _admobIosAppId = EditorGUILayout.TextField("App Id", _admobIosAppId);
            }

            EditorGUILayout.Space(8);
            DrawFirebaseSectionIos();
        }

        private void SaveValues()
        {
            PlayerSettings.productName = _productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _androidPackageName);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, _iosBundleId);

            AppLovinSettings.Instance.SdkKey = _maxSdkKey;
            AppLovinSettings.Instance.AdMobAndroidAppId = _admobAndroidAppId;
            AppLovinSettings.Instance.AdMobIosAppId = _admobIosAppId;
            AppLovinSettings.Instance.SaveAsync();
            AssetDatabase.SaveAssets();

            SetSerializedStringValue(GOOGLE_MOBILE_ADS_SETTINGS_PATH, "adMobAndroidAppId", _admobAndroidAppId);
            SetSerializedStringValue(GOOGLE_MOBILE_ADS_SETTINGS_PATH, "adMobIOSAppId", _admobIosAppId);

            var config = new AppConfigFile(CONFIG_ASSET_PATH);
            config.SetValue("General", "android_package_name", _androidPackageName);
            config.SetValue("General", "ios_bundle_id", _iosBundleId);
            config.SetValue("General", "ios_app_id", _iosAppId);
            config.SetValue("Adjust", "android_app_token", _adjustAndroidToken);
            config.SetValue("Adjust", "ios_app_token", _adjustIosToken);
            config.SetValue("MAX", "MAX_android_banner_ad_id", _maxAndroidBannerAdId);
            config.SetValue("MAX", "MAX_android_interstitial_ad_id", _maxAndroidInterstitialAdId);
            config.SetValue("MAX", "MAX_android_rewarded_ad_id", _maxAndroidRewardedAdId);
            config.SetValue("MAX", "MAX_ios_banner_ad_id", _maxIosBannerAdId);
            config.SetValue("MAX", "MAX_ios_interstitial_ad_id", _maxIosInterstitialAdId);
            config.SetValue("MAX", "MAX_ios_rewarded_ad_id", _maxIosRewardedAdId);
            config.Save();

            EditorApplication.ExecuteMenuItem("File/Save Project");

            ShowNotification(new GUIContent("Settings saved"));
        }

        // GoogleMobileAdsSettings is an internal class in a separate assembly, so its
        // serialized fields are written through SerializedObject instead of a typed API.
        private static void SetSerializedStringValue(string assetPath, string propertyName, string value)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
                return;

            var serializedObject = new SerializedObject(asset);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
