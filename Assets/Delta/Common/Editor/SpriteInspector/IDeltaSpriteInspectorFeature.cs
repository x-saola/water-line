using UnityEditor;

namespace Delta.Editor
{
    public interface IDeltaSpriteInspectorFeature
    {
        bool IsApplicable(TextureImporter importer);
        void OnGUI(TextureImporter importer);
    }
}
