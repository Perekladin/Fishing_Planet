using UnityEngine;

public class WatcherBehavior : MonoBehaviour
{
    public void SetUp(float speed, AimBehaviour aimBehaviour)
    {
        _speed = speed;
        _target = aimBehaviour.target.transform;
    }
    private float _speed;
    private Transform _target;
    private void Update()
    {
        var trackingPosition = _target.position;
        var currentPosition = transform.position;
        if (Vector3.Distance(trackingPosition, currentPosition) < 0.1f)
        {
            return;
        }
        var lookRotation = Quaternion.LookRotation(trackingPosition -
 currentPosition);
        transform.rotation = Quaternion.Slerp(transform.rotation,
 lookRotation, Time.deltaTime * 10f);
        transform.position = Vector3.MoveTowards(currentPosition,
 trackingPosition, _speed * Time.deltaTime);
    }

}
