using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using System.Collections.Generic;

public class PondManager : MonoBehaviour
{
    [Header("Настройки пруда")]
    [SerializeField] private GameObject pondPrefab;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private LayerMask planeLayer = 1;
    [SerializeField] private float spawnDelay = 2f; // Задержка появления
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.4f); // Центр экрана

    [Header("Состояния")]
    [SerializeField] private bool canSpawnPond = true;

    private GameObject currentPond;
    private Vector3 lastHitPosition;
    private bool isTrackingPlane = false;
    private Coroutine spawnCoroutine;

    public Vector3 PondPosition => currentPond != null ? currentPond.transform.position : Vector3.zero;
    public bool IsPondReady => currentPond != null && canSpawnPond;
    public bool HasPond => currentPond != null;

    private void Update()
    {
        TrackPlane();
    }

    private void TrackPlane()
    {
        if (!canSpawnPond) return;

        Vector2 screenPos = new Vector2(
            Screen.width * screenPosition.x,
            Screen.height * screenPosition.y
        );

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 hitPosition = hits[0].pose.position;

            //  ПРУД ТОЛЬКО НА ПЛОСКОСТИ (НЕ В ИГРОКЕ)
            if (Vector3.Distance(hitPosition, Camera.main.transform.position) > 0.5f)
            {
                if (!isTrackingPlane)
                {
                    isTrackingPlane = true;
                    lastHitPosition = hitPosition;
                    StartSpawnCoroutine();
                }
                else if (Vector3.Distance(hitPosition, lastHitPosition) > 0.1f)
                {
                    // Обновляем позицию только если смещение небольшое
                    lastHitPosition = hitPosition;
                }
            }
        }
        else
        {
            isTrackingPlane = false;
            StopSpawnCoroutine();
        }
    }

    private void StartSpawnCoroutine()
    {
        StopSpawnCoroutine();
        spawnCoroutine = StartCoroutine(SpawnPondDelayed());
    }

    private void StopSpawnCoroutine()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnPondDelayed()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (isTrackingPlane && currentPond == null)
        {
            currentPond = Instantiate(pondPrefab, lastHitPosition + Vector3.up * 0.01f, Quaternion.identity);
            Debug.Log("Пруд создан на плоскости!");
        }
    }

    public void ResetPond()
    {
        if (currentPond != null)
        {
            Destroy(currentPond);
            currentPond = null;
            Debug.Log("Пруд сброшен!");
        }
        StopSpawnCoroutine();
        isTrackingPlane = false;
    }

    public void LockPond()
    {
        canSpawnPond = false;
        StopSpawnCoroutine();
    }

    public void UnlockPond()
    {
        canSpawnPond = true;
    }

    private void OnDestroy()
    {
        StopSpawnCoroutine();
    }
}
