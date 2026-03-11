using UnityEngine;

public class Door : MonoBehaviour
{
    public void OpenDoor()
    {
        gameObject.SetActive(false);
    }
    
    public void CloseDoor()
    {
        gameObject.SetActive(true);
    }
}
