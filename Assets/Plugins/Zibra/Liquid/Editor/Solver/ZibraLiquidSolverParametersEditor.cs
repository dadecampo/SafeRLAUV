using com.zibra.common.Analytics;
using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Solver;
using UnityEditor;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidSolverParameters))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidSolverParametersEditor : UnityEditor.Editor
    {
        private SerializedProperty Gravity;
        private SerializedProperty FluidStiffness;
        private SerializedProperty ParticleDensity;
        private SerializedProperty Viscosity;
        private SerializedProperty SurfaceTension;
        private SerializedProperty MaximumVelocity;
        private SerializedProperty MinimumVelocity;
        private SerializedProperty ForceInteractionStrength;
        private SerializedProperty HeightmapResolution;
        private SerializedProperty FoamBuoyancy;
        private SerializedProperty Material1;
        private SerializedProperty Material2;
        private SerializedProperty Material3;
        private SerializedProperty AdditionalParticleSpecies;

        private void OnEnable()
        {
            Gravity = serializedObject.FindProperty("Gravity");
            FluidStiffness = serializedObject.FindProperty("FluidStiffness");
            ParticleDensity = serializedObject.FindProperty("ParticleDensity");
            Viscosity = serializedObject.FindProperty("Viscosity");
            SurfaceTension = serializedObject.FindProperty("SurfaceTension");
            MaximumVelocity = serializedObject.FindProperty("MaximumVelocity");
            MinimumVelocity = serializedObject.FindProperty("MinimumVelocity");
            ForceInteractionStrength = serializedObject.FindProperty("ForceInteractionStrength");
            HeightmapResolution = serializedObject.FindProperty("HeightmapResolution");
            FoamBuoyancy = serializedObject.FindProperty("FoamBuoyancy");
            Material1 = serializedObject.FindProperty("Material1");
            Material2 = serializedObject.FindProperty("Material2");
            Material3 = serializedObject.FindProperty("Material3");
            AdditionalParticleSpecies = serializedObject.FindProperty("AdditionalParticleSpecies");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(Gravity);
            EditorGUILayout.PropertyField(FluidStiffness);
            EditorGUILayout.PropertyField(ParticleDensity);
            EditorGUILayout.PropertyField(Viscosity);
            EditorGUILayout.PropertyField(SurfaceTension);
            EditorGUILayout.PropertyField(MaximumVelocity);
            EditorGUILayout.PropertyField(MinimumVelocity);
            EditorGUILayout.PropertyField(ForceInteractionStrength);
            EditorGUILayout.PropertyField(HeightmapResolution);
            EditorGUILayout.PropertyField(FoamBuoyancy);
            EditorGUILayout.PropertyField(Material1);
            EditorGUILayout.PropertyField(Material2);
            EditorGUILayout.PropertyField(Material3);
            EditorGUILayout.PropertyField(AdditionalParticleSpecies);

            serializedObject.ApplyModifiedProperties();
        }
    }
}