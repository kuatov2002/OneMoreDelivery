using System;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryPointMarker : MonoBehaviour
{
    [Header("Статус доставки")]
    public bool isPickupPoint = true; // true для точки взятия, false для точки доставки

    [Header("События")]
    public UnityEvent onDeliveryPickedUp = new UnityEvent();
    public UnityEvent onDeliveryCompleted = new UnityEvent();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isPickupPoint)
        {
            PickupDelivery();
            Destroy(gameObject);
        }
        else if (other.CompareTag("Player") && !isPickupPoint)
        {
            CompleteDelivery();
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Вызывается когда курьер взял посылку в точке старта
    /// </summary>
    private void PickupDelivery()
    {
        onDeliveryPickedUp?.Invoke();
    }
    
    /// <summary>
    /// Вызывается когда курьер доставил посылку в конечную точку
    /// </summary>
    private void CompleteDelivery()
    {
        onDeliveryCompleted?.Invoke();
    }
}