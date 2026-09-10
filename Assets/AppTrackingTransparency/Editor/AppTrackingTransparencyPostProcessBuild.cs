#if UNITY_IPHONE || UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using System.IO;

namespace ATT 
{
    public class AppTrackingTransparencyPostProcessBuild
    {
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget,
                                            string pathToBuiltProject)
        {
            if (ATTSettings.UseXcode12 ) {

                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                PlistElementDict rootDict = plist.root;

                if (ATTSettings.Description == "") {
                    throw new System.OperationCanceledException(
                        "Build was canceled by the user.");
                }
                rootDict.SetString("NSUserTrackingUsageDescription", 
                    ATTSettings.Description);
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }

        [PostProcessBuild]
        public static void UpdateProject(BuildTarget buildTarget,
                                            string pathToBuiltProject)
        {
            if (ATTSettings.UseXcode12) {
                string projectPath = pathToBuiltProject + 
                    "/Unity-iPhone.xcodeproj/project.pbxproj";
                PBXProject project = new PBXProject();
                project.ReadFromString(File.ReadAllText(projectPath));

            #if UNITY_2019_1_OR_NEWER
                string targetId = project.GetUnityMainTargetGuid();
                string unityFrameworkTargetID = project.GetUnityFrameworkTargetGuid();
            #else
                string targetId = project.TargetGuidByName(PBXProject.GetUnityTargetName());
                string unityFrameworkTargetID = targetId;
            #endif

                project.AddFrameworkToProject(targetId, 
                    "AppTrackingTransparency.framework", true);

                project.AddFrameworkToProject(unityFrameworkTargetID, 
                    "AppTrackingTransparency.framework", true);

                File.WriteAllText(projectPath, project.WriteToString());
            }
        }
    }
}
#endif