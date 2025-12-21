using UnityEngine;

/// <summary>
/// Интерфейс для всех UI модулей. Определяет контракт управления видимостью
/// и жизненным циклом модуля.
/// </summary>
public interface IUIModule
{
    string ModuleName { get; }
    bool IsVisible { get; }
    void Initialize();
    void Show();
    void Hide();
    void Cleanup();
}

/// <summary>
/// Базовый абстрактный класс для всех UI модулей. Инкапсулирует общую логику
/// управления видимостью, анимациями и состоянием.
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
    
    protected bool isInitialized;
    
    protected virtual void Awake()
    {
        // Автоматическая инициализация, если CanvasGroup не назначен в инспекторе
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
        if (isInitialized) return;
        
        OnInitialize();
        isInitialized = true;
    }
    
    public virtual void Show()
    {
        if (!isInitialized)
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
        isInitialized = false;
    }
    
    // Методы для переопределения в дочерних классах
    protected virtual void OnInitialize() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnCleanup() { }
    
    // Анимация появления/исчезания через CanvasGroup
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