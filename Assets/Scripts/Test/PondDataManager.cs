using System.Collections.Generic;
using UnityEngine;
using static ARDrawingManager;

public class PondDataManager : MonoBehaviour
{
    public static PondDataManager Instance { get; private set; }

    public PondSaveData savedPondData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePondData(List<Vector3> points, Vector3 position, Quaternion rotation, float brushSize)
    {
        savedPondData = new PondSaveData
        {
            points = new List<Vector3>(points),
            position = position,
            rotation = rotation,
            brushSize = brushSize
        };

        Debug.Log($"Пруд сохранен: {points.Count} точек, позиция: {position}");
    }

    public PondSaveData LoadPondData()
    {
        return savedPondData;
    }

    public void ClearPondData()
    {
        savedPondData = null;
    }
}