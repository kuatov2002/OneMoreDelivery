using UnityEngine;

/// <summary>
/// Defines the contract for all UI modules with explicit lifecycle phases.
/// </summary>
public interface IUIModule
{
    string ModuleName { get; }
    bool IsVisible { get; }
    bool IsInitialized { get; }
    
    void Initialize();
    void Show();
    void Hide();
    void Cleanup();
}

/// <summary>
/// Base implementation of IUIModule with common functionality.
/// Subclasses override lifecycle hooks for custom behavior.
/// </summary>
public abstract class UIModule : MonoBehaviour, IUIModule
{
    [Header("Module Configuration")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected bool hideOnStart = true;
    
    [Header("Animation Settings")]
    [SerializeField] protected float fadeDuration = 0.3f;
    [SerializeField] protected bool useAnimation = true;
    
    public string ModuleName => GetType().Name;
    public bool IsVisible { get; protected set; }
    public bool IsInitialized { get; protected set; }
    
    protected virtual void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        if (hideOnStart)
        {
            SetVisibilityImmediate(false);
        }
    }
    
    public virtual void Initialize()
    {
        if (IsInitialized) return;
        
        OnInitialize();
        IsInitialized = true;
    }
    
    public virtual void Show()
    {
        if (!IsInitialized)
        {
            Initialize();
        }
        
        if (IsVisible) return;
        
        gameObject.SetActive(true);
        IsVisible = true;
        
        if (useAnimation)
        {
            AnimateShow();
        }
        else
        {
            SetVisibilityImmediate(true);
        }
        
        OnShow();
    }
    
    public virtual void Hide()
    {
        if (!IsVisible) return;
        
        IsVisible = false;
        
        if (useAnimation)
        {
            AnimateHide();
        }
        else
        {
            SetVisibilityImmediate(false);
            gameObject.SetActive(false);
        }
        
        OnHide();
    }
    
    public virtual void Cleanup()
    {
        OnCleanup();
        IsInitialized = false;
    }
    
    protected virtual void OnInitialize() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnCleanup() { }
    
    protected virtual void AnimateShow()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, true));
    }
    
    protected virtual void AnimateHide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, false));
    }
    
    private System.Collections.IEnumerator FadeRoutine(float from, float to, bool interactable)
    {
        float elapsed = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        
        canvasGroup.alpha = to;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
        
        if (!interactable)
        {
            gameObject.SetActive(false);
        }
    }
    
    private void SetVisibilityImmediate(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}