using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public class SpriteCornerCropFeature : IDeltaSpriteInspectorFeature
    {
        public bool IsApplicable(TextureImporter importer) => importer.textureType == TextureImporterType.Sprite;

        public void OnGUI(TextureImporter importer)
        {
            if (GUILayout.Button("Merge 9-Slice Corners"))
                CropCorners(importer);
        }

        static void CropCorners(TextureImporter importer)
        {
            var dataProvider = DeltaSpriteInspectorHelpers.GetDataProvider(importer);
            if (dataProvider == null)
            {
                Debug.LogError("[SpriteCornerCropFeature] No sprite data provider for this asset.");
                return;
            }

            var readableTexture = DeltaSpriteInspectorHelpers.LoadReadableTexture(importer.assetPath);
            if (readableTexture == null)
            {
                Debug.LogError("[SpriteCornerCropFeature] Could not read source texture.");
                return;
            }

            var outputs = SpriteCornerCropUtility.CropCorners(dataProvider, readableTexture);
            Object.DestroyImmediate(readableTexture);

            if (outputs.Count > 0)
            {
                AssetDatabase.Refresh();
                SpriteCornerCropUtility.ApplySourceBorder(outputs);
            }

            DeltaSpriteInspectorHelpers.ShowNotification(outputs.Count > 0
                ? $"Created {outputs.Count} cropped image(s)"
                : "No sprite border found; nothing was cropped.");
        }
    }
}
