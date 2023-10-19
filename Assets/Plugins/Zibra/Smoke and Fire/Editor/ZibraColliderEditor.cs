using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.SDFObjects;
using UnityEditor;
using UnityEngine;
using com.zibra.common.Analytics;

namespace com.zibra.smoke_and_fire.Editor.SDFObjects
{
    [CustomEditor(typeof(ZibraSmokeAndFireCollider))]
    [CanEditMultipleObjects]
    internal class ColliderEditor : UnityEditor.Editor
    {
        static ColliderEditor EditorInstance;

        private ZibraSmokeAndFireCollider[] Colliders;

        private SerializedProperty Friction;

        protected void OnEnable()
        {
            EditorInstance = this;

            Colliders = new ZibraSmokeAndFireCollider[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                Colliders[i] = targets[i] as ZibraSmokeAndFireCollider;
            }

            serializedObject.Update();
            Friction = serializedObject.FindProperty("Friction");
            serializedObject.ApplyModifiedProperties();
        }

        protected void OnDisable()
        {
            if (EditorInstance == this)
            {
                EditorInstance = null;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            bool missingSDF = false;
            foreach (var instance in Colliders)
            {
                SDFObject sdf = instance.GetComponent<SDFObject>();
                if (sdf == null)
                {
                    missingSDF = true;
                }
            }

            if (missingSDF)
            {
                if (Colliders.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 collider missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing collider shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(Colliders.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in Colliders)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }

                if (GUILayout.Button(Colliders.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in Colliders)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }

                if (GUILayout.Button(Colliders.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in Colliders)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<SkinnedMeshSDF>(instance.gameObject);
                        }
                    }
                }

                GUILayout.Space(5);
            }

            EditorGUILayout.PropertyField(Friction);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SmokeAndFire");
            }
        }
    }
}
