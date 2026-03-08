using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnterZone : MonoBehaviour
{
    public event Action EventOnPlayerEntered;

    private void OnValidate()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            EventOnPlayerEntered?.Invoke();
    }
}