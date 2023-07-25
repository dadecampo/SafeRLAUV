using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the object that will be followed
    public float movSmoothing; // The smoothing applied to the movement
    public float rotSmoothing; // The smoothing applied to the rotation

    // Start is called before the first frame update
    void Start()
    {
        transform.position = target.position; // Sets the camera pivot to the target's position
        transform.rotation = target.rotation; // Sets the camera pivot to the target's rotation
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, movSmoothing); // Smoothly follows the target's position
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotSmoothing); // Smoothly follows the target's rotation
    }
}