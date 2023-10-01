using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BouyancyObject : MonoBehaviour
{

    public Transform[] Floaters;
    public float UnderWaterDrag = 3f;
    public float UnderWaterAngularDrag = 0.8f;
    public float AirDrag = 2f;
    public float AirAngularDrag = 0.05f;
    public float FloatingPower = 1_500f;
    public GameObject Water;
    private float WaterHeight = 0f;

    Rigidbody Rb;
    bool Underwater;
    int FloatersUnderWater;
    // Start is called before the first frame update
    void Start()
    {
        Rb = this.GetComponent<Rigidbody>();
        WaterHeight = Water.transform.localToWorldMatrix.GetPosition().y;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        FloatersUnderWater = 0;
        for (int i = 0; i < Floaters.Length; i++)
        {
            float diff = Floaters[i].position.y - WaterHeight;
            if (diff < 0)
            {
                Rb.AddForceAtPosition(Vector3.up * FloatingPower * Mathf.Abs(diff), Floaters[i].position, ForceMode.Force);
                FloatersUnderWater += 1;
                if (!Underwater)
                {
                    Underwater = true;
                    SwitchState(true);
                }
            }
        }
        if (Underwater && FloatersUnderWater == 0)
        {
            Underwater = false;
            SwitchState(false);
        }
    }
    void SwitchState(bool isUnderwater)
    {
        if (isUnderwater)
        {
            Rb.drag = UnderWaterDrag;
            Rb.angularDrag = UnderWaterAngularDrag;
        }
        else
        {
            Rb.drag = AirDrag;
            Rb.angularDrag = AirAngularDrag;
        }
    }
}
