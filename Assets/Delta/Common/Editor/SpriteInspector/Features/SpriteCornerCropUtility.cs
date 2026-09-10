using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public readonly struct CropOutput
    {
        public readonly string AssetPath;
        public readonly Vector4 Border;

        public CropOutput(string assetPath, Vector4 border)
        {
            AssetPath = assetPath;
            Border = border;
        }
    }

    public static class SpriteCornerCropUtility
    {
        public static List<CropOutput> CropCorners(ISpriteEditorDataProvider dataProvider, Texture2D sourceTexture)
        {
            var outputs = new List<CropOutput>();

            var assetPath = (dataProvider.targetObject as AssetImporter)?.assetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SpriteCornerCropUtility] Could not resolve source asset path.");
                return outputs;
            }

            var spriteRects = dataProvider.GetSpriteRects();

            // A single-sprite source can be safely replaced in place (keeps the same file, .meta, and GUID).
            // A multi-sprite atlas can't be replaced by one sub-sprite's corners without destroying the rest, so it still gets its own suffixed file.
            var overwriteSource = spriteRects.Length == 1;
            var directory = Path.GetDirectoryName(assetPath);

            foreach (var spriteRect in spriteRects)
            {
                var outputPath = overwriteSource ? assetPath : Path.Combine(directory, $"{spriteRect.name}_cropped.png").Replace('\\', '/');
                if (TryCropCorners(sourceTexture, spriteRect, outputPath))
                {
                    Debug.Log($"[SpriteCornerCropUtility] Saved '{spriteRect.name}' corners to {outputPath}");
                    outputs.Add(new CropOutput(outputPath, spriteRect.border));
                }
            }

            return outputs;
        }

        public static void ApplySourceBorder(IEnumerable<CropOutput> outputs)
        {
            foreach (var output in outputs)
            {
                var importer = AssetImporter.GetAtPath(output.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[SpriteCornerCropUtility] No TextureImporter found for {output.AssetPath}; border not applied.");
                    continue;
                }

                importer.spriteBorder = output.Border;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        static bool TryCropCorners(Texture2D sourceTexture, SpriteRect spriteRect, string outputPath)
        {
            var border = spriteRect.border; // x = left, y = bottom, z = right, w = top
            var left = Mathf.RoundToInt(border.x);
            var bottom = Mathf.RoundToInt(border.y);
            var right = Mathf.RoundToInt(border.z);
            var top = Mathf.RoundToInt(border.w);

            // An axis with both sides at 0 has no border on that axis, so keep its full span instead of collapsing to 0.
            var hasHorizontalBorder = left != 0 || right != 0;
            var hasVerticalBorder = top != 0 || bottom != 0;
            if (!hasHorizontalBorder && !hasVerticalBorder)
                return false;

            var rect = spriteRect.rect;
            var rectX = Mathf.RoundToInt(rect.x);
            var rectY = Mathf.RoundToInt(rect.y);
            var rectW = Mathf.RoundToInt(rect.width);
            var rectH = Mathf.RoundToInt(rect.height);

            if (left + right > rectW || top + bottom > rectH)
            {
                Debug.LogWarning($"[SpriteCornerCropUtility] '{spriteRect.name}' border is larger than the sprite rect; skipping.");
                return false;
            }

            var bandLeft = hasHorizontalBorder ? left : 0;
            var bandRight = hasHorizontalBorder ? right : rectW;
            var bandBottom = hasVerticalBorder ? bottom : 0;
            var bandTop = hasVerticalBorder ? top : rectH;
            var newWidth = hasHorizontalBorder ? left + right : rectW;
            var newHeight = hasVerticalBorder ? top + bottom : rectH;

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            CopyBlock(sourceTexture, result, rectX, rectY, bandLeft, bandBottom, 0, 0);                                                                  // bottom-left
            CopyBlock(sourceTexture, result, rectX + rectW - bandRight, rectY, bandRight, bandBottom, newWidth - bandRight, 0);                          // bottom-right
            CopyBlock(sourceTexture, result, rectX, rectY + rectH - bandTop, bandLeft, bandTop, 0, newHeight - bandTop);                                 // top-left
            CopyBlock(sourceTexture, result, rectX + rectW - bandRight, rectY + rectH - bandTop, bandRight, bandTop, newWidth - bandRight, newHeight - bandTop); // top-right
            result.Apply();

            File.WriteAllBytes(outputPath, result.EncodeToPNG());
            Object.DestroyImmediate(result);
            return true;
        }

        static void CopyBlock(Texture2D source, Texture2D dest, int srcX, int srcY, int width, int height, int destX, int destY)
        {
            if (width <= 0 || height <= 0)
                return;
            dest.SetPixels(destX, destY, width, height, source.GetPixels(srcX, srcY, width, height));
        }
    }
}
