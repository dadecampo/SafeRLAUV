using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class RoverMovement : Agent
{

    private Rigidbody rb;
    public float currSpeedForward;
    public BouyancyObject bouyancy;
    public Transform[] turboRightForwards;
    public Transform[] turboLeftForwards;
    public float stepUp;
    public float steerPower;
    public float stabilizationSmoothing;
    public Camera[] cameras;
    public Camera currentCamera;
    public Transform goalTransform;
    public List<Transform> startPointList;
    public DistanceRewarder distanceRewarder;
    public long collisions = 0;

    private float distanceToGoal = 1000.0f;
    private float oldDistanceToGoal;
    private float angleToGoal_x;
    private float angleToGoal_y;
    private bool inCollision;
    private bool isPassed;
    private float _timeSpend = 0;
    private bool alreadyGetCheckpoint = false;

    private EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        float target_x = m_ResetParams.GetWithDefault("target_x", goalTransform.position.x);
        float target_y = m_ResetParams.GetWithDefault("target_y", goalTransform.position.y);
        float target_z = m_ResetParams.GetWithDefault("target_z", goalTransform.position.z);
        float waterEnabled = m_ResetParams.GetWithDefault("waterEnabled", 1f);
        float fastRestart = m_ResetParams.GetWithDefault("fastRestart", 0);
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 6);
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.Water != null)
            {
                GameManager.Instance.Water.SetActive(waterEnabled == 1f);
            }
            GameManager.Instance.FastRestart = fastRestart == 1f;
        }
        goalTransform.position = new Vector3(target_x, target_y, target_z);
        inCollision = false;
        base.Initialize();
        rb = GetComponent<Rigidbody>();
        alreadyGetCheckpoint = false;
        distanceRewarder = Instantiate(distanceRewarder);
        distanceRewarder.SetOnlyNCrossedPlanes((int)distancePlanesN);
    }

    private void FixedUpdate()
    {
        if (StepCount > 12000)
        {
            //Debug.Log(GetCumulativeReward());
            GameManager.Instance.AddCollisions(collisions);
            GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
            GameManager.Instance.AddSuccess(0);
            GameManager.Instance.PrintResults();
            EndEpisode();
        }
        float differenceScaled = (oldDistanceToGoal - distanceToGoal) * 100;
        AddReward(differenceScaled);
        Stabilize();
    }

    private void Update()
    {
        CalculateInfoToGoal();
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeCamera();
        }
    }

    #region MLAgents
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteVectorAction = actionBuffers.DiscreteActions;
        GoStraight(discreteVectorAction[2] - 1);
        Torque(discreteVectorAction[2] - 1, discreteVectorAction[1] - 1);
        //Weight system: 0->stay, 1->lose weight, 2->add weight
        if (discreteVectorAction[0] == 0)
        {
            LoseWeight();
        }
        else if (discreteVectorAction[0] == 2)
        {
            AddWeight();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(distanceToGoal);
        sensor.AddObservation(angleToGoal_x);
        sensor.AddObservation(angleToGoal_y);
        sensor.AddObservation(rb.velocity.normalized);
        sensor.AddObservation(rb.angularVelocity.normalized);
    }

    public override void OnEpisodeBegin()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        float target_x = m_ResetParams.GetWithDefault("target_x", goalTransform.position.x);
        float target_y = m_ResetParams.GetWithDefault("target_y", goalTransform.position.y);
        float target_z = m_ResetParams.GetWithDefault("target_z", goalTransform.position.z);
        float waterEnabled = m_ResetParams.GetWithDefault("waterEnabled", 1f);
        float fastRestart = m_ResetParams.GetWithDefault("fastRestart", 0);
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 6);

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.Water != null)
            {
                GameManager.Instance.Water.SetActive(waterEnabled == 1f);
            }
            GameManager.Instance.FastRestart = fastRestart == 1f;
        }

        collisions = 0;
        goalTransform.position = new Vector3(target_x, target_y, target_z);

        rb.velocity = new Vector3(0, 0, 0);
        rb.angularVelocity = new Vector3(0, 0, 0);
        int choice = UnityEngine.Random.Range(0, startPointList.Count);
        rb.position = startPointList[choice].position + new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
        bouyancy.FloatingPower = 250;
        rb.rotation = Quaternion.Euler(0, 180, 0);
        _timeSpend = 0;
        inCollision = false;
        if (distanceRewarder == null)
        {
            distanceRewarder = Instantiate(distanceRewarder);
        }
        distanceRewarder.SetOnlyNCrossedPlanes((int)distancePlanesN);
        distanceRewarder.SetCommonTarget(this.gameObject);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

        var vectorAction = actionsOut.DiscreteActions;
        vectorAction[0] = 1;
        vectorAction[1] = 1;
        vectorAction[2] = 1;

        if (Input.GetKey(KeyCode.Space))
            vectorAction[0] = 0;
        else if (Input.GetKey(KeyCode.LeftShift))
            vectorAction[0] = 2;

        if (Input.GetKey(KeyCode.A))
            vectorAction[1] = 0;
        else if (Input.GetKey(KeyCode.D))
            vectorAction[1] = 2;

        if (Input.GetKey(KeyCode.S))
            vectorAction[2] = 0;
        else if (Input.GetKey(KeyCode.W))
            vectorAction[2] = 2;

    }

    #endregion

    #region Actions
    private void GoStraight(float straightPower)
    {
        foreach (Transform t in turboRightForwards)
        {
            rb.AddForce((t.up * currSpeedForward / 2 * straightPower), ForceMode.Impulse);
        }
        foreach (Transform t in turboLeftForwards)
        {
            rb.AddForce((t.up * currSpeedForward / 2 * straightPower), ForceMode.Impulse);
        }
    }

    private void Torque(float straightPower, float steeringPower)
    {
        rb.AddTorque(transform.up * steerPower * steeringPower * straightPower);
    }

    private void AddWeight()
    {
        if (bouyancy.FloatingPower - stepUp < 100)
        {
            bouyancy.FloatingPower = 100;
            return;
        }
        bouyancy.FloatingPower -= stepUp;
    }

    private void LoseWeight()
    {
        if (bouyancy.FloatingPower + stepUp > 700)
        {
            bouyancy.FloatingPower = 700;
            return;
        }
        bouyancy.FloatingPower += stepUp;
    }

    void ChangeCamera()
    {
        currentCamera.gameObject.SetActive(false);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == currentCamera)
            {
                currentCamera = cameras[(i + 1) % cameras.Length];
                break;
            }
        }
        currentCamera.gameObject.SetActive(true);
    }


    #endregion

    #region Utility
    private void CalculateInfoToGoal()
    {
        //Distance to goal
        oldDistanceToGoal = distanceToGoal;
        distanceToGoal = distanceRewarder.GetCumulativeDistanceToGoal();
        //distanceToGoal=Vector3.Distance(transform.position, goalTransform.position);
        //Angle direction
        Vector3 normalizedDirection = goalTransform.position - transform.position;
        float whichWay = Vector3.Cross(transform.forward, normalizedDirection).y;
        whichWay /= Math.Abs(whichWay);

        //Angle degree
        Vector3 targetDir_x = new Vector3(goalTransform.position.x, 0, goalTransform.position.z) - new Vector3(transform.position.x, 0, transform.position.z);
        targetDir_x = targetDir_x.normalized;
        float dot = Vector3.Dot(targetDir_x, transform.forward);
        angleToGoal_x = Mathf.Acos(dot) * Mathf.Rad2Deg * whichWay;

        whichWay = 1;

        //Angle degree
        Vector3 targetDir_y = new Vector3(goalTransform.position.x, goalTransform.position.y, goalTransform.position.z) - new Vector3(transform.position.x, transform.position.y, transform.position.z);
        targetDir_y = targetDir_y.normalized;
        dot = Vector3.Dot(targetDir_y, transform.up);
        angleToGoal_y = Mathf.Asin(dot) * Mathf.Rad2Deg * whichWay;

        //Debug.Log(String.Format("Distance to goal: {0}, Angle_X: {1}, Angle_Y: {2} , way: {3}", distanceToGoal, angleToGoal_x, angleToGoal_y, whichWay));
    }
    void Stabilize()
    {
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(new Vector3(0, rb.rotation.eulerAngles.y, 0)), stabilizationSmoothing)); // Smoothly and slowly rotate the submarine to be upright
    }

    private void OnCollisionStay(Collision collision)
    {
        if (GameManager.Instance.FastRestart)
            return;

        if (collision.collider.CompareTag("StartWall"))
        {
            inCollision = true;
            AddReward(-0.05f);
        }
        if (collision.collider.CompareTag("Cave"))
        {
            inCollision = true;
            AddReward(-0.05f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("StartWall"))
        {
            if (!GameManager.Instance.FastRestart)
            {
                inCollision = true;
                collisions += 1;
                AddReward(-0.05f);
            }
            else
            {
                inCollision = true;
                AddReward(-2000f);
                Debug.Log(GetCumulativeReward());
                EndEpisode();
            }
        }
        if (collision.collider.CompareTag("Cave"))
        {
            if (!GameManager.Instance.FastRestart)
            {
                inCollision = true;
                collisions += 1;
                AddReward(-0.05f);
            }
            else
            {
                inCollision = true;
                AddReward(-2000f);
                Debug.Log(GetCumulativeReward());
                EndEpisode();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("StartWall"))
        {
            inCollision = false;
        }
        if (collision.collider.CompareTag("Cave"))
        {
            inCollision = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DistancePlane"))
        {
            distanceRewarder.UpdateCrossedPlanes();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GOAL"))
        {
            AddReward(2000f);
            //Debug.Log(GetCumulativeReward());
            GameManager.Instance.AddCollisions(collisions);
            GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
            GameManager.Instance.AddSuccess(1);
            GameManager.Instance.PrintResults();
            EndEpisode();
        }
        else if (other.CompareTag("CHECKPOINT") && !alreadyGetCheckpoint)
        {
            AddReward(500f);
            alreadyGetCheckpoint = true;
        }

    }

    #endregion


}
