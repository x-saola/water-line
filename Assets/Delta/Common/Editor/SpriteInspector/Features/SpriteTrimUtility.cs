using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public readonly struct TrimOutput
    {
        public readonly string AssetPath;
        public readonly Vector4 Border;
        public readonly Vector2 Pivot;

        public TrimOutput(string assetPath, Vector4 border, Vector2 pivot)
        {
            AssetPath = assetPath;
            Border = border;
            Pivot = pivot;
        }
    }

    public static class SpriteTrimUtility
    {
        const float AlphaThreshold = 1f / 255f;

        public static List<TrimOutput> TrimTransparentPadding(ISpriteEditorDataProvider dataProvider, Texture2D sourceTexture)
        {
            var outputs = new List<TrimOutput>();

            var assetPath = (dataProvider.targetObject as AssetImporter)?.assetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SpriteTrimUtility] Could not resolve source asset path.");
                return outputs;
            }

            var directory = Path.GetDirectoryName(assetPath);

            foreach (var spriteRect in dataProvider.GetSpriteRects())
            {
                if (TryTrim(sourceTexture, spriteRect, directory, out var output))
                {
                    Debug.Log($"[SpriteTrimUtility] Saved '{spriteRect.name}' trimmed sprite to {output.AssetPath}");
                    outputs.Add(output);
                }
            }

            return outputs;
        }

        public static void ApplySpriteSettings(IEnumerable<TrimOutput> outputs)
        {
            foreach (var output in outputs)
            {
                var importer = AssetImporter.GetAtPath(output.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[SpriteTrimUtility] No TextureImporter found for {output.AssetPath}; settings not applied.");
                    continue;
                }

                importer.spriteBorder = output.Border;
                importer.spritePivot = output.Pivot;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        static bool TryTrim(Texture2D sourceTexture, SpriteRect spriteRect, string directory, out TrimOutput output)
        {
            output = default;

            var rect = spriteRect.rect;
            var rectX = Mathf.RoundToInt(rect.x);
            var rectY = Mathf.RoundToInt(rect.y);
            var rectW = Mathf.RoundToInt(rect.width);
            var rectH = Mathf.RoundToInt(rect.height);

            var pixels = sourceTexture.GetPixels(rectX, rectY, rectW, rectH);

            var minX = rectW;
            var minY = rectH;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < rectH; y++)
            {
                for (var x = 0; x < rectW; x++)
                {
                    if (pixels[y * rectW + x].a < AlphaThreshold)
                        continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < 0)
            {
                Debug.LogWarning($"[SpriteTrimUtility] '{spriteRect.name}' is fully transparent; skipping.");
                return false;
            }

            var trimLeft = minX;
            var trimBottom = minY;
            var trimRight = rectW - 1 - maxX;
            var trimTop = rectH - 1 - maxY;

            if (trimLeft == 0 && trimRight == 0 && trimBottom == 0 && trimTop == 0)
            {
                Debug.Log($"[SpriteTrimUtility] '{spriteRect.name}' has no transparent padding; skipping.");
                return false;
            }

            var newWidth = maxX - minX + 1;
            var newHeight = maxY - minY + 1;

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            result.SetPixels(sourceTexture.GetPixels(rectX + minX, rectY + minY, newWidth, newHeight));
            result.Apply();

            var outputPath = Path.Combine(directory, $"{spriteRect.name}_trimmed.png").Replace('\\', '/');
            File.WriteAllBytes(outputPath, result.EncodeToPNG());
            Object.DestroyImmediate(result);

            var border = spriteRect.border; // x = left, y = bottom, z = right, w = top
            var newBorder = new Vector4(
                Mathf.Max(0f, border.x - trimLeft),
                Mathf.Max(0f, border.y - trimBottom),
                Mathf.Max(0f, border.z - trimRight),
                Mathf.Max(0f, border.w - trimTop));

            // Pivot is normalized within the sprite rect (bottom-left origin); re-anchor it to the trimmed bounds.
            var oldPivotAbsolute = spriteRect.pivot * new Vector2(rectW, rectH);
            var newPivotAbsolute = oldPivotAbsolute - new Vector2(minX, minY);
            var newPivot = new Vector2(newPivotAbsolute.x / newWidth, newPivotAbsolute.y / newHeight);

            output = new TrimOutput(outputPath, newBorder, newPivot);
            return true;
        }
    }
}
