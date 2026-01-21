using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneEvents : MonoBehaviour
{
    [Header("Scene Events")]
    [SerializeField] private UnityEvent onSceneLoaded;
    [SerializeField] private UnityEvent onSceneUnloaded;


    private void Start()
    {
        onSceneLoaded?.Invoke();
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloadedHandler;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloadedHandler;
    }

    private void OnSceneUnloadedHandler(Scene scene)
    {
        // Проверяем, что выгружается именно наша сцена
        if (scene == gameObject.scene)
        {
            onSceneUnloaded?.Invoke();
        }
    }
}