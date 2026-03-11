using System.Collections;
using UnityEngine;

public class TeleportToBoss : MonoBehaviour
{
    [SerializeField] private Transform bossPoint;
    [SerializeField] private float teleportDelay = 0.5f;
    [SerializeField] private GameObject teleportVFX;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(TeleportWithDelay(other.transform));
        }
    }

    private IEnumerator TeleportWithDelay(Transform player)
    {
        // Спаун VFX в позиции игрока
        Instantiate(teleportVFX, player.position + Vector3.up * 0.5f, Quaternion.identity);
        
        // Спаун VFX в точке босса
        Instantiate(teleportVFX, bossPoint.position + Vector3.up * 0.5f, Quaternion.identity);
        
        // Задержка перед телепортом
        yield return new WaitForSeconds(teleportDelay);
        
        // Телепорт игрока
        player.position = bossPoint.position;
    }
}