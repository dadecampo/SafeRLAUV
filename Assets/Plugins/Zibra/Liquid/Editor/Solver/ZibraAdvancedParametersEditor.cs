using com.zibra.common.Analytics;
using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Solver;
using UnityEditor;
using UnityEngine;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidAdvancedRenderParameters))]
    [CanEditMultipleObjects]
    internal class ZibraAdvancedParametersEditor : UnityEditor.Editor
    {
        private ZibraLiquidAdvancedRenderParameters[] ParameterInstances;

        private SerializedProperty RayMarchingResolutionDownscale;
        private SerializedProperty RefractionBounces;
        private SerializedProperty DisableRaymarch;
        private SerializedProperty UnderwaterRender;
        private SerializedProperty MaxLiquidMeshSize;
        private SerializedProperty VertexOptimizationIterations;
        private SerializedProperty MeshOptimizationIterations;
        private SerializedProperty VertexOptimizationStep;
        private SerializedProperty MeshOptimizationStep;
        private SerializedProperty DualContourIsoSurfaceLevel;
        private SerializedProperty IsoSurfaceLevel;
        private SerializedProperty RayMarchIsoSurface;
        private SerializedProperty RayMarchMaxSteps;
        private SerializedProperty RayMarchStepSize;
        private SerializedProperty RayMarchStepFactor;

        protected void OnEnable()
        {
            ParameterInstances = new ZibraLiquidAdvancedRenderParameters[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ParameterInstances[i] = targets[i] as ZibraLiquidAdvancedRenderParameters;
            }

            serializedObject.Update();

            RayMarchingResolutionDownscale = serializedObject.FindProperty("RayMarchingResolutionDownscale");
            RefractionBounces = serializedObject.FindProperty("RefractionBounces");
            DisableRaymarch = serializedObject.FindProperty("DisableRaymarch");
            UnderwaterRender = serializedObject.FindProperty("UnderwaterRender");
            MaxLiquidMeshSize = serializedObject.FindProperty("MaxLiquidMeshSize");
            VertexOptimizationIterations = serializedObject.FindProperty("VertexOptimizationIterations");
            MeshOptimizationIterations = serializedObject.FindProperty("MeshOptimizationIterations");
            VertexOptimizationStep = serializedObject.FindProperty("VertexOptimizationStep");
            MeshOptimizationStep = serializedObject.FindProperty("MeshOptimizationStep");
            DualContourIsoSurfaceLevel = serializedObject.FindProperty("DualContourIsoSurfaceLevel");
            IsoSurfaceLevel = serializedObject.FindProperty("IsoSurfaceLevel");
            RayMarchIsoSurface = serializedObject.FindProperty("RayMarchIsoSurface");
            RayMarchMaxSteps = serializedObject.FindProperty("RayMarchMaxSteps");
            RayMarchStepSize = serializedObject.FindProperty("RayMarchStepSize");
            RayMarchStepFactor = serializedObject.FindProperty("RayMarchStepFactor");

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsRenderingModeUsed(ZibraLiquid.RenderingMode renderingMode)
        {
            bool isRenderingModeUsed = false;

            foreach (var instance in ParameterInstances)
            {
                ZibraLiquid liquid = instance.GetComponent<ZibraLiquid>();

                if (liquid == null)
                    continue;

                if (liquid.CurrentRenderingMode == renderingMode)
                {
                    isRenderingModeUsed = true;
                    break;
                }
            }

            return isRenderingModeUsed;
        }

        public override void OnInspectorGUI()
        {
            bool meshRenderMode = IsRenderingModeUsed(ZibraLiquid.RenderingMode.MeshRender);
#if ZIBRA_EFFECTS_OTP_VERSION
            bool unityRenderMode = false;
#else
            bool unityRenderMode = IsRenderingModeUsed(ZibraLiquid.RenderingMode.UnityRender);
#endif

            if (meshRenderMode)
            {
                EditorGUILayout.PropertyField(RayMarchingResolutionDownscale);
                EditorGUILayout.PropertyField(RefractionBounces);
                EditorGUILayout.PropertyField(DisableRaymarch);
                EditorGUILayout.PropertyField(UnderwaterRender);
            }

            if (unityRenderMode)
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                EditorGUILayout.PropertyField(MaxLiquidMeshSize);
                EditorGUI.EndDisabledGroup();
            }

            if (meshRenderMode || unityRenderMode)
            {
                EditorGUILayout.PropertyField(VertexOptimizationIterations);
                EditorGUILayout.PropertyField(MeshOptimizationIterations);
                EditorGUILayout.PropertyField(VertexOptimizationStep);
                EditorGUILayout.PropertyField(MeshOptimizationStep);
                EditorGUILayout.PropertyField(DualContourIsoSurfaceLevel);
                EditorGUILayout.PropertyField(IsoSurfaceLevel);
            }

            if (meshRenderMode)
            {
                EditorGUILayout.PropertyField(RayMarchIsoSurface);
                EditorGUILayout.PropertyField(RayMarchMaxSteps);
                EditorGUILayout.PropertyField(RayMarchStepSize);
                EditorGUILayout.PropertyField(RayMarchStepFactor);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
