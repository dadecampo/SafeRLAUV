using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.DistanceRewarder;

public class DistanceObject: MonoBehaviour, IDistanceTarget
{
    [SerializeField]
    private MeshFilter m_targetMesh;
    private GameObject m_targetObject;
    private bool m_targetIsGoThrough = false;

    public bool TargetIsGoThrough { get => m_targetIsGoThrough; set => m_targetIsGoThrough = value; }
    public MeshFilter TargetMesh { get => m_targetMesh; set => m_targetMesh = value; }
    public GameObject TargetObject { get => m_targetObject; set => m_targetObject = value; }

    private void Awake()
    {
        m_targetMesh = GetComponent<MeshFilter>();
    }

    public float GetDistanceFromAnotherPoint(Vector3 point)
    {
        return Vector3.Distance(m_targetMesh.transform.position, point);
    }

    public void SetTargetObject(GameObject target)
    {
        m_targetObject = target;
    }

    public float GetDistanceFromTarget()
    {
        return Vector3.Distance(TargetMesh.transform.position, TargetObject.transform.position);
    }

}
