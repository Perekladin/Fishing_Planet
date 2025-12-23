using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PondTriggerCustom
    : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Float"))
        {
            Fishing fishing = FindObjectOfType<Fishing>();
            if (fishing != null)
                fishing.OnFloatEnteredPond(other.gameObject);
        }
    }
}
