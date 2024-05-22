using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using UnityEditor;

namespace com.zibra.common.Editor.Solver
{
    [CustomEditor(typeof(AnalyticSDF))]
    [CanEditMultipleObjects]
    internal class AnalyticSDFEditor : UnityEditor.Editor
    {
        private SerializedProperty InvertSDF;
        private SerializedProperty SurfaceDistance;
        private SerializedProperty ChosenSDFType;

        private void OnEnable()
        {
            InvertSDF = serializedObject.FindProperty("InvertSDF");
            SurfaceDistance = serializedObject.FindProperty("SurfaceDistance");
            ChosenSDFType = serializedObject.FindProperty("ChosenSDFType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(InvertSDF);
            EditorGUILayout.PropertyField(SurfaceDistance);
            EditorGUILayout.PropertyField(ChosenSDFType);

            serializedObject.ApplyModifiedProperties();
        }
    }
}