using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Unity.MLAgents.Sensors.RayPerceptionOutput;
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
    public List<RayPerceptionSensorComponent3D> rayPerceptionSensorComponents;
    private List<double> distanceFromWallsForEachEpisode = new List<double>();
    public long collisions = 0;
    private List<double> collisionsForEachEpisode = new List<double>();
    private List<Vector3> historyPositions = new List<Vector3>();

    private float distanceToGoal;
    private float oldDistanceToGoal;
    private float angleToGoal_x;
    private float angleToGoal_y;
    private bool inCollision;
    private bool isPassed;
    private float _timeSpend = 0;
    private bool alreadyGetCheckpoint = false;
    private double meanDistanceFromWalls = 0.0f;
    private int measurementsMeanDistanceFromWalls = 0;
    private int episode = 0;

    private EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        float target_x = m_ResetParams.GetWithDefault("target_x", goalTransform.position.x);
        float target_y = m_ResetParams.GetWithDefault("target_y", goalTransform.position.y);
        float target_z = m_ResetParams.GetWithDefault("target_z", goalTransform.position.z);
        float waterEnabled = m_ResetParams.GetWithDefault("waterEnabled", 1f);
        float fastRestart = m_ResetParams.GetWithDefault("fastRestart", 1f);
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 6);
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
            AddDistanceFromWalls();
            collisionsForEachEpisode.Add(collisions);
            CreateCollisionsCSV();

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

    private void SavePosition()
    {
        historyPositions.Add(rb.transform.position);
    }

    private void SavePositionHistory()
    {
        //Crea il nome del file CSV
        string csvFileName = String.Format("C:\\Users\\david\\Desktop\\Thesis\\CSV\\position_{0}.csv",this.name);

        using (StreamWriter writer = new StreamWriter(csvFileName))
        {
            // Scrive i dati
            for (int i = 0; i < historyPositions.Count; i++)
            {
                writer.WriteLine($"{historyPositions[i].x.ToString()}: {historyPositions[i].y.ToString()}: {historyPositions[i].z.ToString()}");
            }
        }

        Console.WriteLine("Dati scritti nel file CSV.");
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
        float distancePlanesN = m_ResetParams.GetWithDefault("distancePlanesN", 6);
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
        if (this != GameManager.Instance.FirstRover && GameManager.Instance.StartSavePosition)
        {
            rb.position = GameManager.Instance.FirstRover.transform.position;
            rb.rotation = GameManager.Instance.FirstRover.transform.rotation;
            bouyancy.FloatingPower = GameManager.Instance.FirstRover.bouyancy.FloatingPower;

        }
        else
        {
            rb.position = startPointList[choice].position;
            rb.rotation = Quaternion.Euler(0, 180, 0);
            bouyancy.FloatingPower = 250;
        }

        _timeSpend = 0;
        inCollision = false;
        if (distanceRewarder == null)
        {
            distanceRewarder = Instantiate(distanceRewarder);
        }
        distanceRewarder.SetOnlyNCrossedPlanes((int)distancePlanesN);
        distanceRewarder.SetCommonTarget(this.gameObject);
        episode += 1;
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
        if (straightPower < 0)
        {
            AddReward(Constants.RWD_BACKWARD_PENALITY);
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

    private void AddSensorRewards()
    {
        if (!GameManager.Instance.SafeTraining)
            return;

        // Read the observations from the RayPerceptionSensor3D
        float cum = 0.0f;
        foreach (RayPerceptionSensorComponent3D rayPerceptionSensor in rayPerceptionSensorComponents){
            var r1 = rayPerceptionSensor.GetRayPerceptionInput();
            var r3 = RayPerceptionSensor.Perceive(r1);
            {
                foreach (RayPerceptionOutput.RayOutput rayOutput in r3.RayOutputs)
                {
                    AddReward( - ( 1-rayOutput.HitFraction )*Constants.RWD_MULTIPLIER_SENSORS);
                    cum += r1.RayLength*(rayOutput.HitFraction);
                }
            }
        }
        cum /= 28;
        measurementsMeanDistanceFromWalls += 1;
        meanDistanceFromWalls += cum;
    }
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
        inCollision = true;
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
        inCollision = true;
        collisions += 1;

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

                CreateMeanDistanceFromWallsCSV();
                CreateCollisionsCSV();

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
                CreateMeanDistanceFromWallsCSV();
                CreateCollisionsCSV();

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
        string meanString = (meanDistanceFromWalls / measurementsMeanDistanceFromWalls).ToString().Replace(",",".");
        Debug.Log("Mean Distance From Walls: " + meanString);
        distanceFromWallsForEachEpisode.Add(meanDistanceFromWalls / measurementsMeanDistanceFromWalls);
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
            AddReward(Constants.RWD_GOAL);
            AddDistanceFromWalls();
            collisionsForEachEpisode.Add(collisions);
            CreateMeanDistanceFromWallsCSV();
            CreateCollisionsCSV();
            GameManager.Instance.AddCollisions(collisions);
            GameManager.Instance.AddCumulativeReward(GetCumulativeReward());
            GameManager.Instance.AddSuccess(1);
            GameManager.Instance.PrintResults();
            EndEpisode();
        }
        else if (other.CompareTag("CHECKPOINT") && !alreadyGetCheckpoint)
        {
            AddReward(Constants.RWD_CHECKPOINT);
            alreadyGetCheckpoint = true;
        }

    }

    #endregion

    #region CSV
    private void CreateMeanDistanceFromWallsCSV()
    {
        if (!GameManager.Instance.CreateCSVMeanDistanceFromWalls)
        {
            return;
        }
        //Crea il nome del file CSV
        string csvFileName = String.Format("C:\\Users\\david\\Desktop\\Thesis\\CSV\\datiMeanDistanceFromWalls_{0}.csv", this.name);

        using (StreamWriter writer = new StreamWriter(csvFileName))
        {
            // Scrive l'intestazione
            writer.WriteLine("Episode, MeanDistanceFromWalls");

            // Scrive i dati
            for (int i = 0; i < distanceFromWallsForEachEpisode.Count; i++)
            {
                writer.WriteLine($"{i + 1}, {distanceFromWallsForEachEpisode[i]}");
            }
        }

        Console.WriteLine("Dati scritti nel file CSV.");
    }

    private void CreateCollisionsCSV()
    {
        if (!GameManager.Instance.CreateCSVCollisions)
        {
            return;
        }
        //Crea il nome del file CSV
        string csvFileName = String.Format("C:\\Users\\david\\Desktop\\Thesis\\CSV\\datiCollisions_{0}.csv", this.name);

        using (StreamWriter writer = new StreamWriter(csvFileName))
        {
            // Scrive l'intestazione
            writer.WriteLine("Episode, Collisions");

            // Scrive i dati
            for (int i = 0; i < collisionsForEachEpisode.Count; i++)
            {
                writer.WriteLine($"{i + 1}, {collisionsForEachEpisode[i]}");
            }
        }

        Console.WriteLine("Dati scritti nel file CSV.");
    }
    #endregion

}
