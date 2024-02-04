using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DistanceRewarder
{
    public interface IDistanceTarget
    {
        public MeshFilter TargetMesh { get; set; }
        public GameObject TargetObject { get; set; }
        public bool TargetIsGoThrough { get; set; }

        public void SetTargetObject(GameObject target);
    }
}
