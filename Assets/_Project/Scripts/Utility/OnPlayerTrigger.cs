using System;
using UnityEngine;
using UnityEngine.Events;

public class OnPlayerTrigger : MonoBehaviour
{
    [SerializeField] private UnityEvent onPlayerTrigger;
    [SerializeField] private UnityEvent onPlayerExit;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onPlayerTrigger?.Invoke();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onPlayerExit?.Invoke();
        }
    }
}