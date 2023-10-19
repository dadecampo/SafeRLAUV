using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using UnityEditor;

namespace com.zibra.liquid.Editor.Solver
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
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            EditorGUILayout.PropertyField(InvertSDF);
            EditorGUILayout.PropertyField(SurfaceDistance);
            EditorGUILayout.PropertyField(ChosenSDFType);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SDF");
            }
        }
    }
}