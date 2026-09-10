using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class AthenaProcessBuild : IPostprocessBuildWithReport, IPreprocessBuildWithReport
{
    const string XCODE_IMAGES_FOLDER = "Unity-iPhone/Images.xcassets";
    const string SOURCE_FOLDER_NAME = "AppLogo.imageset";
    const string SOURCE_FOLDER_ROOT = "Athena/Editor/iOS-Patch/LaunchScreen/Assets.xcassets";

    public int callbackOrder { get { return int.MaxValue; } }

    public void OnPreprocessBuild(BuildReport report)
    {
#if USE_ATHENA_LOGO
        PlayerSettings.SplashScreen.show = false;
#endif
    }

    public void OnPostprocessBuild(BuildReport report)
    {
#if UNITY_IOS
        string buildPath = report.summary.outputPath;
        string projectPath = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));

#if UNITY_2018
        string targetGUID = project.TargetGuidByName("Unity-iPhone");
        string unityFrameworkTargetID = project.TargetGuidByName("UnityFramework");
#else
        string targetGUID = project.GetUnityMainTargetGuid();
        string unityFrameworkTargetID = project.GetUnityFrameworkTargetGuid();
#endif

        // sound switch
        var muteFile = Application.dataPath + "/Athena/Editor/iOS-Patch/mute.caf";
        File.Copy(muteFile, report.summary.outputPath + "/mute.caf", true);
        var fileGuid = project.AddFile("mute.caf", "mute.caf");
        project.AddFileToBuild(targetGUID, fileGuid);

#if UNITY_2019_3_OR_NEWER
        // Unity 2019 issue - GoogleService-Info.plist is not added to main target
        string googleInfoPlistGuid = project.FindFileGuidByProjectPath("GoogleService-Info.plist");
        project.AddFileToBuild(targetGUID, googleInfoPlistGuid);
#endif
        project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC");
        project.SetBuildProperty(targetGUID, "CLANG_ENABLE_MODULES", "YES");
        project.SetBuildProperty(targetGUID, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");

        project.SetBuildProperty(targetGUID, "ENABLE_BITCODE", "NO");
        project.SetBuildProperty(unityFrameworkTargetID, "ENABLE_BITCODE", "NO");

        File.WriteAllText(projectPath, project.WriteToString());

        // Launch Screen
#if USE_ATHENA_LOGO
        string sourcePath = $"{Application.dataPath}/{SOURCE_FOLDER_ROOT}/{SOURCE_FOLDER_NAME}";
        string targetPath = $"{buildPath}/{XCODE_IMAGES_FOLDER}/{SOURCE_FOLDER_NAME}";

        FileUtil.DeleteFileOrDirectory(targetPath);
        FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
#endif

        // wkweview
        string plistPath = report.summary.outputPath + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("gad_preferred_webview", "wkwebview");
#if USE_MAX_MEDIATION
        rootDict.SetString("NSCalendarsUsageDescription", "Used to deliver better advertising experience");
#endif
        File.WriteAllText(plistPath, plist.WriteToString());
#endif
    }
}
