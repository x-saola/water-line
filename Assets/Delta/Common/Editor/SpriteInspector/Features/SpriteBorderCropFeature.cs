using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public class SpriteBorderCropFeature : IDeltaSpriteInspectorFeature
    {
        public bool IsApplicable(TextureImporter importer) => importer.textureType == TextureImporterType.Sprite;

        public void OnGUI(TextureImporter importer)
        {
            if (GUILayout.Button("Crop by Border"))
                CropByBorder(importer);
        }

        static void CropByBorder(TextureImporter importer)
        {
            var dataProvider = DeltaSpriteInspectorHelpers.GetDataProvider(importer);
            if (dataProvider == null)
            {
                Debug.LogError("[SpriteBorderCropFeature] No sprite data provider for this asset.");
                return;
            }

            var readableTexture = DeltaSpriteInspectorHelpers.LoadReadableTexture(importer.assetPath);
            if (readableTexture == null)
            {
                Debug.LogError("[SpriteBorderCropFeature] Could not read source texture.");
                return;
            }

            var outputs = SpriteBorderCropUtility.CropByBorder(dataProvider, readableTexture);
            Object.DestroyImmediate(readableTexture);

            if (outputs.Count > 0)
            {
                AssetDatabase.Refresh();
                SpriteBorderCropUtility.ApplySpriteSettings(outputs);
            }

            DeltaSpriteInspectorHelpers.ShowNotification(outputs.Count > 0
                ? $"Created {outputs.Count} border-cropped image(s)"
                : "No sprite border found; nothing was cropped.");
        }
    }
}
