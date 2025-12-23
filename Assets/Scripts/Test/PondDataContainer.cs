using System.Collections.Generic;
using UnityEngine;

public class PondDataContainer : MonoBehaviour
{
    [System.Serializable]
    public class PondSaveData
    {
        public List<Vector3> points;
        public Vector3 position;
        public Quaternion rotation;
        public float brushSize;
    }

    public PondSaveData pondData;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void ClearData()
    {
        pondData = null;
    }
}