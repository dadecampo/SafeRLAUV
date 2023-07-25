using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class DistanceRewarder : MonoBehaviour
{
    [SerializeField]
    private List<DistancePlane> m_divisionPlanes;
    private void Awake()
    {
    }

    public void SetCommonTarget(GameObject target)
    {
        foreach (var plane in m_divisionPlanes)
        {
            plane.SetTargetObject(target);
        }
    }

    public float GetCumulativeDistanceToGoal()
    {
        List<DistancePlane> lst = m_divisionPlanes.Where((x) => ! x.TargetIsGoThrough).ToList();
        float cumulative = 0;
        for (int i = 0; i < lst.Count - 1; i++)
        {
            cumulative += lst[i].GetDistanceFromAnotherPoint(lst[i + 1].transform.position);
        }
        if (lst.Count == 0)
            cumulative += m_divisionPlanes[m_divisionPlanes.Count - 1].GetDistanceFromTarget();
        else
            cumulative += m_divisionPlanes[m_divisionPlanes.Count-lst.Count].GetDistanceFromTarget();
        return cumulative;
    }

    public DistancePlane GetNearestDistancePlane()
    {
        float minDist = float.MaxValue;
        float computedDistance = 0;
        int index = -1;
        for (int i = 0; i < m_divisionPlanes.Count - 1; i++)
        {
            //Trovo Distance Panel più vicino così so quale ho attraversato
            computedDistance = m_divisionPlanes[i].GetDistanceFromTarget();
            if (computedDistance < minDist)
            {
                minDist = computedDistance;
                index = i;
            }
        }
        return m_divisionPlanes[index];
    }

    public void UpdateCrossedPlanes()
    {
        int index = m_divisionPlanes.IndexOf(GetNearestDistancePlane());
        for (int i = 0; i <= index; i++)
        {
            m_divisionPlanes[i].TargetIsGoThrough = true;
        }
    }

    public void ResetCrossedPlanes()
    {
        for (int i = 0; i < m_divisionPlanes.Count; i++)
        {
            m_divisionPlanes[i].TargetIsGoThrough = false;
        }
    }

    public void SetOnlyNCrossedPlanes(int n)
    {
        List<DistancePlane> planes= new List<DistancePlane>();
        for (int i = 0; i < n; i++)
        {
            planes.Add(m_divisionPlanes[i]);
        }
        m_divisionPlanes = planes;
        ResetCrossedPlanes();
    }

}
