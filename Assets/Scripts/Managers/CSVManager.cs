using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVManager : MonoBehaviour
{
    public string PATH_FOLDER_CSV_DISTANCE_FROM_WALLS = "";
    public string PATH_FOLDER_CSV_COLLISIONS = "";

    public bool CreateCSVMeanDistanceFromWalls;
    public bool CreateCSVCollisions;
    public bool CreateCSVCollisionLocation;

    public static CSVManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void CreateMeanDistanceFromWallsCSV(string objectName, List<double> distanceFromWallsForEachEpisode)
    {
        if (!CreateCSVMeanDistanceFromWalls)
        {
            return;
        }
        //Crea il nome del file CSV
        string csvFileName = String.Format(PATH_FOLDER_CSV_DISTANCE_FROM_WALLS + "\\datiMeanDistanceFromWalls_{0}.csv", objectName);

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

    public void CreateCollisionsCSV(string objectName, List<double> collisionsForEachEpisode)
    {
        if (!CreateCSVCollisions)
        {
            return;
        }
        //Crea il nome del file CSV
        string csvFileName = String.Format(PATH_FOLDER_CSV_COLLISIONS + "\\datiCollisions_{0}.csv", objectName);

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

    public void CreateCollisionLocationCSV(string objectName, List<Vector3> collisionLocationForEachEpisode)
    {
        if (!CreateCSVCollisionLocation)
        {
            return;
        }
        //Crea il nome del file CSV
        string csvFileName = String.Format(PATH_FOLDER_CSV_COLLISIONS + "\\datiCollisionLocation_{0}.csv", objectName);

        using (StreamWriter writer = new StreamWriter(csvFileName))
        {
            // Scrive l'intestazione
            writer.WriteLine("Episode, CollisionLocation_X, CollisionLocation_Y, CollisionLocation_Z");

            // Scrive i dati
            for (int i = 0; i < collisionLocationForEachEpisode.Count; i++)
            {
                writer.WriteLine($"{i + 1}, {collisionLocationForEachEpisode[i].x}, {collisionLocationForEachEpisode[i].y}, {collisionLocationForEachEpisode[i].z}");
            }
        }

        Console.WriteLine("Dati scritti nel file CSV.");
    }
}
