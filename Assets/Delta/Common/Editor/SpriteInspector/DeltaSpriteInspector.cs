using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace Delta.Editor
{
    [CustomEditor(typeof(TextureImporter))]
    [CanEditMultipleObjects]
    public class DeltaSpriteInspector : UnityEditor.Editor
    {
        const string ShowToolsSessionKey = "Delta.Editor.DeltaSpriteInspector.ShowTools";

        static readonly Type DefaultInspectorType = Type.GetType("UnityEditor.TextureImporterInspector, UnityEditor.CoreModule");

        // AssetImporterEditor only enables its Apply/Revert bar (needsApplyRevert) when Unity's
        // native ActiveEditorTracker wires up a companion editor for the imported asset via this
        // internal method. CreateEditor() alone never calls it, so we replicate the wiring by hand.
        static readonly MethodInfo SetAssetImporterTargetEditorMethod =
            typeof(AssetImporterEditor).GetMethod("InternalSetAssetImporterTargetEditor", BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly IDeltaSpriteInspectorFeature[] Features =
        {
            new SpriteCornerCropFeature(),
            new SpriteBorderCropFeature(),
            new SpriteTrimFeature(),
        };

        UnityEditor.Editor _defaultEditor;
        UnityEditor.Editor _assetEditor;

        void OnEnable()
        {
            _defaultEditor = CreateEditor(targets, DefaultInspectorType);

            var mainAssets = targets
                .OfType<TextureImporter>()
                .Select(importer => AssetDatabase.LoadMainAssetAtPath(importer.assetPath))
                .Where(asset => asset != null)
                .ToArray();

            if (mainAssets.Length == targets.Length)
            {
                _assetEditor = CreateEditor(mainAssets);
                SetAssetImporterTargetEditorMethod?.Invoke(_defaultEditor, new object[] { _assetEditor });
            }
        }

        void OnDisable()
        {
            if (_defaultEditor != null)
                DestroyImmediate(_defaultEditor);

            if (_assetEditor != null)
                DestroyImmediate(_assetEditor);
        }

        public override void OnInspectorGUI()
        {
            if (target is TextureImporter importer)
            {
                var applicableFeatures = Features.Where(f => f.IsApplicable(importer)).ToArray();
                if (applicableFeatures.Length > 0)
                {
                    var showTools = SessionState.GetBool(ShowToolsSessionKey, true);
                    var newShowTools = EditorGUILayout.BeginFoldoutHeaderGroup(showTools, "Delta Tools");
                    if (newShowTools != showTools)
                        SessionState.SetBool(ShowToolsSessionKey, newShowTools);

                    if (newShowTools)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var feature in applicableFeatures)
                            feature.OnGUI(importer);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                    EditorGUILayout.Space();
                }
            }

            _defaultEditor.OnInspectorGUI();
        }
    }
}
