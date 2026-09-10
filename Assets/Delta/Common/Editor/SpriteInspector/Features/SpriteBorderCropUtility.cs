using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public readonly struct BorderCropOutput
    {
        public readonly string AssetPath;
        public readonly Vector2 Pivot;

        public BorderCropOutput(string assetPath, Vector2 pivot)
        {
            AssetPath = assetPath;
            Pivot = pivot;
        }
    }

    // Crops away the 9-slice border margins, keeping only the inner content area.
    // The complement of SpriteCornerCropUtility, which keeps the corners and discards this same content area.
    public static class SpriteBorderCropUtility
    {
        public static List<BorderCropOutput> CropByBorder(ISpriteEditorDataProvider dataProvider, Texture2D sourceTexture)
        {
            var outputs = new List<BorderCropOutput>();

            var assetPath = (dataProvider.targetObject as AssetImporter)?.assetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SpriteBorderCropUtility] Could not resolve source asset path.");
                return outputs;
            }

            var directory = Path.GetDirectoryName(assetPath);

            foreach (var spriteRect in dataProvider.GetSpriteRects())
            {
                if (TryCropByBorder(sourceTexture, spriteRect, directory, out var output))
                {
                    Debug.Log($"[SpriteBorderCropUtility] Saved '{spriteRect.name}' inner content to {output.AssetPath}");
                    outputs.Add(output);
                }
            }

            return outputs;
        }

        public static void ApplySpriteSettings(IEnumerable<BorderCropOutput> outputs)
        {
            foreach (var output in outputs)
            {
                var importer = AssetImporter.GetAtPath(output.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[SpriteBorderCropUtility] No TextureImporter found for {output.AssetPath}; settings not applied.");
                    continue;
                }

                importer.spriteBorder = Vector4.zero; // the border margins were cropped away; nothing left to stretch around.
                importer.spritePivot = output.Pivot;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        static bool TryCropByBorder(Texture2D sourceTexture, SpriteRect spriteRect, string directory, out BorderCropOutput output)
        {
            output = default;

            var border = spriteRect.border; // x = left, y = bottom, z = right, w = top
            var left = Mathf.RoundToInt(border.x);
            var bottom = Mathf.RoundToInt(border.y);
            var right = Mathf.RoundToInt(border.z);
            var top = Mathf.RoundToInt(border.w);

            if (left == 0 && bottom == 0 && right == 0 && top == 0)
            {
                Debug.LogWarning($"[SpriteBorderCropUtility] '{spriteRect.name}' has no border set; skipping.");
                return false;
            }

            var rect = spriteRect.rect;
            var rectX = Mathf.RoundToInt(rect.x);
            var rectY = Mathf.RoundToInt(rect.y);
            var rectW = Mathf.RoundToInt(rect.width);
            var rectH = Mathf.RoundToInt(rect.height);

            var newWidth = rectW - left - right;
            var newHeight = rectH - top - bottom;
            if (newWidth <= 0 || newHeight <= 0)
            {
                Debug.LogWarning($"[SpriteBorderCropUtility] '{spriteRect.name}' border consumes the entire sprite rect; skipping.");
                return false;
            }

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            result.SetPixels(sourceTexture.GetPixels(rectX + left, rectY + bottom, newWidth, newHeight));
            result.Apply();

            var outputPath = Path.Combine(directory, $"{spriteRect.name}_bordercrop.png").Replace('\\', '/');
            File.WriteAllBytes(outputPath, result.EncodeToPNG());
            Object.DestroyImmediate(result);

            // Pivot is normalized within the sprite rect (bottom-left origin); re-anchor it to the inner content bounds.
            var oldPivotAbsolute = spriteRect.pivot * new Vector2(rectW, rectH);
            var newPivotAbsolute = oldPivotAbsolute - new Vector2(left, bottom);
            var newPivot = new Vector2(newPivotAbsolute.x / newWidth, newPivotAbsolute.y / newHeight);

            output = new BorderCropOutput(outputPath, newPivot);
            return true;
        }
    }
}
