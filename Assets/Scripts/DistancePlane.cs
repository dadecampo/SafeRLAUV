using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
public class DistancePlane : MonoBehaviour
{

    private GameObject m_targetObject;
    [SerializeField]
    private MeshFilter m_plane;

    private bool m_targetIsGoThrough=false;

    public bool TargetIsGoThrough { get => m_targetIsGoThrough; set => m_targetIsGoThrough = value; }

    private void Awake()
    {
        m_plane = GetComponent<MeshFilter>();
    }

    public void SetTargetObject(GameObject target)
    {
        m_targetObject = target;
    }

    public float GetDistanceFromTarget()
    {
        return Vector3.Distance(m_plane.transform.position, m_targetObject.transform.position);
    }

    public float GetDistanceFromAnotherPoint(Vector3 point)
    {
        return Vector3.Distance(m_plane.transform.position, point);
    }

}
