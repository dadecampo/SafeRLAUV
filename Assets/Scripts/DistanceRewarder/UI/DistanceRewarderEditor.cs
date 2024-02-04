using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DistanceRewarder))]
public class DistanceRewarderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var distanceRewarder = (DistanceRewarder)target;

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (GUILayout.Button("Add Target Rewarder"))
        {
            DistanceObject  distanceObject=distanceRewarder.AddTargetRewarder();
            MeshRenderer mR = distanceObject.GetComponent<MeshRenderer>();
            mR.materials = new Material[0];
        }
    }
}