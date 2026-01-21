using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class SceneTrigger : MonoBehaviour
{
    [SerializeField]
    private SceneAsset sceneAsset;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(sceneAsset.name)) return;

        SceneManager.LoadSceneAsync(sceneAsset.name);
    }
}