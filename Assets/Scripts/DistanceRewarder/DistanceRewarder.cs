using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DistanceRewarder : MonoBehaviour
{
    [SerializeField]
    private List<DistanceObject> m_targetRewarders = new List<DistanceObject>();
    private void Awake()
    {
    }

    public List<DistanceObject> GetTargetRewarders()
    {
        return m_targetRewarders;
    }

    public DistanceObject AddTargetRewarder()
    {
        GameObject piano = GameObject.CreatePrimitive(PrimitiveType.Plane);
        piano.transform.position = transform.position;
        piano.transform.Rotate(90, 0, 0);

        MeshFilter meshFilter = piano.AddComponent<MeshFilter>();
        DistanceObject distanceObject = piano.AddComponent<DistanceObject>();

        MeshCollider meshCollider = piano.GetComponent<MeshCollider>();

        meshCollider.convex = true;
        meshCollider.isTrigger = true;


        distanceObject.TargetMesh = meshFilter;
        distanceObject.TargetObject = null;
        distanceObject.TargetIsGoThrough = false;

        piano.transform.SetParent(transform);
        distanceObject.TargetMesh = meshFilter;
        m_targetRewarders.Add(distanceObject);
        return distanceObject;
    }

    public void SetCommonTarget(GameObject target)
    {
        foreach (var plane in m_targetRewarders)
        {
            plane.SetTargetObject(target);
        }
    }

    public float GetCumulativeDistanceToGoal()
    {
        List<DistanceObject> lst = m_targetRewarders.Where((x) => ! x.TargetIsGoThrough).ToList();
        float cumulative = 0;
        for (int i = 0; i < lst.Count - 1; i++)
        {
            cumulative += lst[i].GetDistanceFromAnotherPoint(lst[i + 1].transform.position);
        }
        if (lst.Count == 0)
            cumulative += m_targetRewarders[m_targetRewarders.Count - 1].GetDistanceFromTarget();
        else
            cumulative += m_targetRewarders[m_targetRewarders.Count-lst.Count].GetDistanceFromTarget();
        return cumulative;
    }

    public DistanceObject GetNearestDistancePlane()
    {
        float minDist = float.MaxValue;
        float computedDistance = 0;
        int index = -1;
        for (int i = 0; i < m_targetRewarders.Count - 1; i++)
        {
            //Trovo Distance Panel più vicino così so quale ho attraversato
            computedDistance = m_targetRewarders[i].GetDistanceFromTarget();
            if (computedDistance < minDist)
            {
                minDist = computedDistance;
                index = i;
            }
        }
        return m_targetRewarders[index];
    }

    public void UpdateCrossedPlanes()
    {
        int index = m_targetRewarders.IndexOf(GetNearestDistancePlane());
        for (int i = 0; i <= index; i++)
        {
            m_targetRewarders[i].TargetIsGoThrough = true;
        }
    }

    public void ResetCrossedPlanes()
    {
        for (int i = 0; i < m_targetRewarders.Count; i++)
        {
            m_targetRewarders[i].TargetIsGoThrough = false;
        }
    }

    public void SetOnlyNCrossedPlanes(int n)
    {
        List<DistanceObject> planes= new List<DistanceObject>();
        for (int i = 0; i < n; i++)
        {
            planes.Add(m_targetRewarders[i]);
        }
        m_targetRewarders = planes;
        ResetCrossedPlanes();
    }

}
