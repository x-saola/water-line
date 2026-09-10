using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Delta.Editor
{
    public static class DeltaSpriteInspectorHelpers
    {
        public static ISpriteEditorDataProvider GetDataProvider(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider?.InitSpriteEditorDataProvider();
            return dataProvider;
        }

        public static Texture2D LoadReadableTexture(string assetPath)
        {
            if (!File.Exists(assetPath))
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(File.ReadAllBytes(assetPath)))
                return texture;

            Object.DestroyImmediate(texture);
            return null;
        }

        public static void ShowNotification(string message)
        {
            var window = EditorWindow.focusedWindow;
            if (window != null)
                window.ShowNotification(new GUIContent(message));
        }
    }
}
