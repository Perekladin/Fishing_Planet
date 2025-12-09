using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AimBehaviour : MonoBehaviour
{
    [SerializeField] public GameObject target;
    public ARPlane currentPlane;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    private ARPlane _lockedPlane;
    private Camera _mainCamera;
    private void Start()
    {
        _mainCamera = Camera.main;
    }
    private void Update()
    {
        var screenCenter = _mainCamera.ViewportToScreenPoint(new Vector3(0.5f,
     0.5f));
        var hits = new List<ARRaycastHit>();
        raycastManager.Raycast(screenCenter, hits,
     TrackableType.PlaneWithinBounds);
        currentPlane = null;
        ARRaycastHit? hit = null;
        if (hits.Count > 0)
        {
            var lockedPlane = _lockedPlane;
            hit = lockedPlane == null
                ? hits[0]
               : hits.SingleOrDefault(x => x.trackableId ==
     lockedPlane.trackableId);
        }
        if (hit.HasValue)
        {
            currentPlane = planeManager.GetPlane(hit.Value.trackableId);
            transform.position = hit.Value.pose.position;
        }
        target.SetActive(currentPlane != null);
    }
    public void SetLockedPlane(ARPlane keepPlane)
    {
        _lockedPlane = keepPlane;
    }

}
