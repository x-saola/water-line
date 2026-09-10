using UnityEditor;
using UnityEngine;

namespace Delta.Services
{
    [CustomPropertyDrawer(typeof(AudioConfigSO.AudioClipData))]
    public class AudioClipDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var audioIdProp = property.FindPropertyRelative("AudioId");
            string id = audioIdProp != null && !string.IsNullOrEmpty(audioIdProp.stringValue)
                ? audioIdProp.stringValue
                : label.text;

            label.text = id;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
