using UnityEditor;
using UnityEditor.UI;

namespace Delta.Common.UI
{

    [CustomEditor(typeof(DeltaButton), true)]
    [CanEditMultipleObjects]
    public class DeltaButtonEditor : ButtonEditor
    {
        private SerializedProperty m_ButtonTextProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ButtonTextProp = serializedObject.FindProperty("m_ButtonText");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Delta Button Custom Settings", EditorStyles.boldLabel);

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ButtonTextProp);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
