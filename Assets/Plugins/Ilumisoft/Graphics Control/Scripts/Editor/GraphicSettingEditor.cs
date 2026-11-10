using UnityEditor;

namespace Ilumisoft.GraphicsControl.Editor
{
    [CustomEditor(typeof(GraphicSetting), true)]
    public class GraphicSettingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}