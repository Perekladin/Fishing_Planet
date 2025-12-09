using UnityEngine;

public class WatcherSpawner : MonoBehaviour
{
    [SerializeField] private AimBehaviour aimBehaviour;
    [SerializeField] private GameObject watcherPrefab;
    [SerializeField] private float speed = 1.2f;
    private WatcherBehavior _watcherBehavior;
    private void Update()
    {
        if (_watcherBehavior == null && aimBehaviour.currentPlane != null)
        {
            var instantiate = Instantiate(watcherPrefab,
 aimBehaviour.transform.position, Quaternion.identity);
            _watcherBehavior = instantiate.GetComponent<WatcherBehavior>();
            _watcherBehavior.SetUp(speed, aimBehaviour);
            aimBehaviour.SetLockedPlane(aimBehaviour.currentPlane);
        }
    }
}
