using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanManager : MonoBehaviour
{

    public float wavesHeight = 0.07f;
    public float wavesFrequency = 0.01f;
    public float wavesSpeed = 0.04f;
    public Transform ocean;
    Material oceanMat;
    Texture2D wavesDisplacement;


    // Start is called before the first frame update
    void Start()
    {
        SetVariables();
    }

    void SetVariables()
    {
        oceanMat = ocean.GetComponent<Renderer>().sharedMaterial;
        wavesDisplacement = (Texture2D)oceanMat.GetTexture("_WavesDisplacement");

    }

    public float WaterHeightAtPosition(Vector3 position)
    {
        return ocean.position.y + wavesDisplacement.GetPixelBilinear(position.x * wavesFrequency * ocean.localScale.x, (position.z * wavesFrequency + Time.time * wavesSpeed) * ocean.localScale.z).g * wavesHeight;
    }

    private void OnValidate()
    {
        if (!oceanMat)
        {
            SetVariables();
        }
        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        oceanMat.SetFloat("_WavesFrequency", wavesFrequency);
        oceanMat.SetFloat("_WavesSpeed", wavesSpeed);
        oceanMat.SetFloat("_WavesHeight", wavesHeight);
    }

}
