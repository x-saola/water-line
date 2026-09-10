using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public class SpriteTrimFeature : IDeltaSpriteInspectorFeature
    {
        public bool IsApplicable(TextureImporter importer) => importer.textureType == TextureImporterType.Sprite;

        public void OnGUI(TextureImporter importer)
        {
            if (GUILayout.Button("Trim Transparent Padding"))
                Trim(importer);
        }

        static void Trim(TextureImporter importer)
        {
            var dataProvider = DeltaSpriteInspectorHelpers.GetDataProvider(importer);
            if (dataProvider == null)
            {
                Debug.LogError("[SpriteTrimFeature] No sprite data provider for this asset.");
                return;
            }

            var readableTexture = DeltaSpriteInspectorHelpers.LoadReadableTexture(importer.assetPath);
            if (readableTexture == null)
            {
                Debug.LogError("[SpriteTrimFeature] Could not read source texture.");
                return;
            }

            var outputs = SpriteTrimUtility.TrimTransparentPadding(dataProvider, readableTexture);
            Object.DestroyImmediate(readableTexture);

            if (outputs.Count > 0)
            {
                AssetDatabase.Refresh();
                SpriteTrimUtility.ApplySpriteSettings(outputs);
            }

            DeltaSpriteInspectorHelpers.ShowNotification(outputs.Count > 0
                ? $"Created {outputs.Count} trimmed image(s)"
                : "No transparent padding found; nothing was trimmed.");
        }
    }
}
