
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;

namespace OneSDK.Editor
{
    public class OneSDKNotification : EditorWindow
    {
        static string _title = "Version Update";
        static string _targetPackage;
        GUIStyle textStyle;
        static EmbedRequest Request;
        static ListRequest LRequest;

        private void Awake()
        {
            textStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 20,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
        public static void ShowUI()
        {
            OneSDKNotification.GetPackageName();

        }


        static void GetPackageName()
        {
            LRequest = Client.List();
            EditorApplication.update += LProgress;
        }

        static void LProgress()
        {
            try
            {
                if (LRequest.IsCompleted)
                {
                    if (LRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in LRequest.Result)
                        {

                            if (package.name.Contains("com.athena.onesdktool"))
                            {
                                Debug.Log("Current Version: " + package.version);
                                Debug.Log("Current Version: " + package.versions.latest);
                                if (package.version != package.versions.latest)
                                {
                                    OneSDKNotification.GetWindow<OneSDKNotification>(true, OneSDKNotification._title, true);
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log(LRequest.Error.message);
                    }
                    EditorApplication.update -= LProgress;

                }
            }
            catch (System.Exception)
            {
                EditorApplication.update -= LProgress;
                throw new Exception();
            }

        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("One SDK new Version", textStyle);

            if (GUILayout.Button("Update"))
            {
            }
        }
    }
}
