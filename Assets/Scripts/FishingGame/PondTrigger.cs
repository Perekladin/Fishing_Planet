using UnityEngine;

public class PondTrigger : MonoBehaviour
{
    public Fishing fishing;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Float"))
        {
            fishing.OnFloatEnteredPond(other.gameObject);
        }
    }
}
