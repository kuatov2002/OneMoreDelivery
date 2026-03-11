using UnityEngine;
using UnityEngine.SceneManagement;

public class UtilityInstructions : MonoBehaviour
{
    #region Cursor Methods
    public void UnlockCursor()
    { 
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void LockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    #endregion
    
    #region GameObject Methods
    public void DestroyGameObject(GameObject target) => Destroy(target);
    public void DestroyThisGameObject() => Destroy(gameObject);
    public void SetActive(GameObject target) => target.SetActive(true);
    public void SetInactive(GameObject target) => target.SetActive(false);
    public void ToggleActive(GameObject target) => target.SetActive(!target.activeSelf);
    public void InstantiatePrefab(GameObject prefab) => Instantiate(prefab);
    public void InstantiatePrefabAtPosition(GameObject prefab, Transform position) => 
        Instantiate(prefab, position.position, position.rotation);
    #endregion
    
    #region Scene Management Methods
    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
    public void LoadSceneByIndex(int sceneIndex) => SceneManager.LoadScene(sceneIndex);
    public void LoadSceneAsync(string sceneName) => SceneManager.LoadSceneAsync(sceneName);
    public void ReloadCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void LoadNextScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public void LoadPreviousScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    public void QuitApplication() => Application.Quit();
    #endregion
    
    #region Debug Methods
    public void DebugLog(string message) => Debug.Log(message);
    public void DebugLogWarning(string message) => Debug.LogWarning(message);
    public void DebugLogError(string message) => Debug.LogError(message);
    #endregion
}