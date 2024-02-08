using Assets.Scripts;
using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BoatMovement : Agent
{
    private Rigidbody rb;
    public float currSpeedForward;
    public Transform[] turboRightForwards;
    public Transform[] turboLeftForwards;
    public float steerPower;
    public float stabilizationSmoothing;
    public Camera[] cameras;
    public Camera currentCamera;
    public Transform goalTransform;
    public List<Transform> startPointList;
    public DistanceRewarder distanceRewarder;
    public List<RayPerceptionSensorComponent3D> rayPerceptionSensorComponents;
    public Transform centerOfMass;

    private List<double> distanceFromWallsForEachEpisode = new List<double>();
    public long collisions = 0;
    private List<double> collisionsForEachEpisode = new List<double>();
    private List<Vector3> collisionsLocationForEachEpisode = new List<Vector3>();

    private float distanceToGoal;
    private float oldDistanceToGoal;
    private float angleToGoal_x;
    private float angleToGoal_y;
    private double meanDistanceFromWalls = 0.0f;
    private int measurementsMeanDistanceFromWalls = 0;

    private EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        float target_x = m_ResetParams.GetWithDefault("target_x", goalTransform.position.x);
        float target_y = m_ResetParams.GetWithDefault("target_y", goalTransform.position.y);
        float target_z = m_ResetParams.GetWithDefault("target_z", goalTransform.position.z);
        float waterEnabled = m_ResetParams.GetWithDefault("waterEnabled", 1f);
        float fastRestart = m_ResetParams.GetWithDefault("fastRestart", 1f);
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 5);
        float safeTraining = m_ResetParams.GetWithDefault("safeTraining", 1f);
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.Water != null && !GameManager.Instance.WaterActive)
            {
                GameManager.Instance.WaterActive = true;
                GameManager.Instance.Water.SetActive(waterEnabled == 1f);
            }
            GameManager.Instance.FastRestart = fastRestart == 1f;
            GameManager.Instance.SafeTraining = safeTraining == 1f;
        }
        goalTransform.position = new Vector3(target_x, target_y, target_z);
        base.Initialize();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
        distanceRewarder = Instantiate(distanceRewarder);
        distanceRewarder.SetOnlyNCrossedPlanes((int)distancePlanesN);
        distanceRewarder.SetCommonTarget(this.gameObject);
    }

    private void FixedUpdate()
    {
        if (StepCount > 12000)
        {
            AddDistanceFromWalls();
            collisionsForEachEpisode.Add(collisions);
            CSVManager.Instance.CreateCollisionsCSV(this.name, collisionsForEachEpisode);

            GameManager.Instance.AddCollisions(collisions);
            GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
            GameManager.Instance.AddSuccess(0);
            GameManager.Instance.PrintResults();
            EndEpisode();
        }
        CalculateInfoToGoal();
        //SavePosition();
        float differenceScaled = (oldDistanceToGoal - distanceToGoal) * Constants.RWD_MULTIPLIER_DISTANCE;
        //Debug.Log(differenceScaled);
        AddReward(differenceScaled + Constants.RWD_TIMESTEP_PENALITY);
        AddSensorRewards();
        Stabilize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeCamera();
        }
    }

    #region MLAgents
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteVectorAction = actionBuffers.DiscreteActions;
        GoStraight(discreteVectorAction[1] - 1);
        Torque(discreteVectorAction[1] - 1, discreteVectorAction[0] - 1);
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        FixObservations();
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
        float fastRestart = m_ResetParams.GetWithDefault("fastRestart", 1f);
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 5);
        float safeTraining = m_ResetParams.GetWithDefault("safeTraining", 1f);
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.Water != null && !GameManager.Instance.WaterActive)
            {
                GameManager.Instance.WaterActive = true;
                GameManager.Instance.Water.SetActive(waterEnabled == 1f);
            }
            GameManager.Instance.FastRestart = fastRestart == 1f;
            GameManager.Instance.SafeTraining = safeTraining == 1f;
        }

        collisions = 0;
        meanDistanceFromWalls = 0;
        measurementsMeanDistanceFromWalls = 0;
        goalTransform.position = new Vector3(target_x, target_y, target_z);

        rb.velocity = new Vector3(0, 0, 0);
        rb.angularVelocity = new Vector3(0, 0, 0);
        int choice = UnityEngine.Random.Range(0, startPointList.Count);
        rb.position = startPointList[choice].position;
        rb.rotation = Quaternion.Euler(0, 180, 0);

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

        if (Input.GetKey(KeyCode.A))
            vectorAction[0] = 0;
        else if (Input.GetKey(KeyCode.D))
            vectorAction[0] = 2;

        if (Input.GetKey(KeyCode.S))
            vectorAction[1] = 0;
        else if (Input.GetKey(KeyCode.W))
            vectorAction[1] = 2;

    }

    #endregion

    #region Actions
    private void GoStraight(float straightPower)
    {
        if (straightPower < 0)
        {
            straightPower /= 2;
        }
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
        rb.AddTorque(transform.up * steerPower * steeringPower);
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

    private void AddSensorRewards()
    {
        if (!GameManager.Instance.SafeTraining)
            return;

        // Read the observations from the RayPerceptionSensor3D
        float sum = 0.0f;
        foreach (RayPerceptionSensorComponent3D rayPerceptionSensor in rayPerceptionSensorComponents)
        {
            var r1 = rayPerceptionSensor.GetRayPerceptionInput();
            var r3 = RayPerceptionSensor.Perceive(r1);
            {
                foreach (RayPerceptionOutput.RayOutput rayOutput in r3.RayOutputs)
                {
                    AddReward(-(1 - rayOutput.HitFraction) * Constants.RWD_MULTIPLIER_SENSORS);
                    sum += r1.RayLength * (rayOutput.HitFraction);
                }
            }
        }
        sum /= 28;
        measurementsMeanDistanceFromWalls += 1;
        meanDistanceFromWalls += sum;
    }
    private void CalculateInfoToGoal()
    {
        //Distance to goal
        oldDistanceToGoal = distanceToGoal;
        distanceToGoal = distanceRewarder.GetCumulativeDistanceToGoal();

        //Angle direction
        Vector3 normalizedDirection = goalTransform.position - transform.position;
        float whichWay = Vector3.Cross(transform.forward, normalizedDirection).y;
        whichWay /= Math.Abs(whichWay);

        //Angle degree X
        Vector3 targetDir_x = new Vector3(goalTransform.position.x, 0, goalTransform.position.z) - new Vector3(transform.position.x, 0, transform.position.z);
        targetDir_x = targetDir_x.normalized;
        float dot = Vector3.Dot(targetDir_x, transform.forward);
        angleToGoal_x = Mathf.Acos(dot) * Mathf.Rad2Deg * whichWay;

        whichWay = 1;

        //Angle degree Y
        Vector3 targetDir_y = new Vector3(goalTransform.position.x, goalTransform.position.y, goalTransform.position.z) - new Vector3(transform.position.x, transform.position.y, transform.position.z);
        targetDir_y = targetDir_y.normalized;
        dot = Vector3.Dot(targetDir_y, transform.up);
        angleToGoal_y = Mathf.Asin(dot) * Mathf.Rad2Deg * whichWay;


        //Debug.Log(distanceToGoal);
    }

    void Stabilize()
    {
        // Smoothly and slowly rotate the submarine to be upright
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(new Vector3(0, rb.rotation.eulerAngles.y, 0)), stabilizationSmoothing));
    }

    private void FixObservations()
    {
        if (distanceToGoal is float.NaN)
        {
            distanceToGoal = 0.0f;
        }
        if (angleToGoal_x is float.NaN)
        {
            angleToGoal_x = 0.0f;
        }
        if (angleToGoal_y is float.NaN)
        {
            angleToGoal_y = 0.0f;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (GameManager.Instance.FastRestart)
            return;

        if (collision.collider.CompareTag("StartWall"))
        {
            AddReward(Constants.RWD_WALL_RESTART_FALSE);
        }
        if (collision.collider.CompareTag("Cave"))
        {
            AddReward(Constants.RWD_WALL_RESTART_FALSE);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisions += 1;
        collisionsLocationForEachEpisode.Add(this.transform.position);
        if (collision.collider.CompareTag("StartWall"))
        {
            if (!GameManager.Instance.FastRestart)
            {
                AddReward(Constants.RWD_WALL_RESTART_FALSE);
            }
            else
            {
                AddReward(Constants.RWD_WALL_RESTART_TRUE);
                AddDistanceFromWalls();
                collisionsForEachEpisode.Add(collisions);

                CSVManager.Instance.CreateMeanDistanceFromWallsCSV(this.name, distanceFromWallsForEachEpisode);
                CSVManager.Instance.CreateCollisionsCSV(this.name, collisionsForEachEpisode);
                CSVManager.Instance.CreateCollisionLocationCSV(this.name, collisionsLocationForEachEpisode);

                GameManager.Instance.AddCollisions(collisions);
                GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
                GameManager.Instance.AddSuccess(0);
                GameManager.Instance.PrintResults();
                EndEpisode();
            }
        }
        if (collision.collider.CompareTag("Cave"))
        {
            if (!GameManager.Instance.FastRestart)
            {
                AddReward(Constants.RWD_WALL_RESTART_FALSE);
            }
            else
            {
                AddReward(Constants.RWD_WALL_RESTART_TRUE);
                AddDistanceFromWalls();
                collisionsForEachEpisode.Add(collisions);

                CSVManager.Instance.CreateMeanDistanceFromWallsCSV(this.name, distanceFromWallsForEachEpisode);
                CSVManager.Instance.CreateCollisionsCSV(this.name, collisionsForEachEpisode);
                CSVManager.Instance.CreateCollisionLocationCSV(this.name, collisionsLocationForEachEpisode);

                GameManager.Instance.AddCollisions(collisions);
                GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
                GameManager.Instance.AddSuccess(0);
                GameManager.Instance.PrintResults();
                EndEpisode();
            }
        }
    }

    private void AddDistanceFromWalls()
    {
        string meanString = (meanDistanceFromWalls / measurementsMeanDistanceFromWalls).ToString().Replace(",", ".");
        Debug.Log("Mean Distance From Walls: " + meanString);
        distanceFromWallsForEachEpisode.Add(meanDistanceFromWalls / measurementsMeanDistanceFromWalls);
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
            AddReward(Constants.RWD_GOAL);
            AddDistanceFromWalls();
            collisionsForEachEpisode.Add(collisions);

            CSVManager.Instance.CreateMeanDistanceFromWallsCSV(this.name, distanceFromWallsForEachEpisode);
            CSVManager.Instance.CreateCollisionsCSV(this.name, collisionsForEachEpisode);
            CSVManager.Instance.CreateCollisionLocationCSV(this.name, collisionsLocationForEachEpisode);

            GameManager.Instance.AddCollisions(collisions);
            GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
            GameManager.Instance.AddSuccess(1);
            GameManager.Instance.PrintResults();
            EndEpisode();
        }

    }

    #endregion


}
