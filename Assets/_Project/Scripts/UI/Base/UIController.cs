using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Центральный координатор всех UI модулей. Управляет регистрацией, навигацией
/// и взаимодействием между модулями. Реализует паттерн Singleton для глобального доступа.
/// </summary>
public class UIController : MonoBehaviour
{
    private static UIController _instance;
    public static UIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UIController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIController");
                    _instance = go.AddComponent<UIController>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Module References")]
    [SerializeField] private List<UIModule> registeredModules = new List<UIModule>();
    
    private Dictionary<string, IUIModule> _moduleRegistry;
    private Stack<string> _navigationStack;
    private IUIModule _currentModule;
    
    // События для подписки других систем на изменения UI
    public event Action<string> OnModuleShown;
    public event Action<string> OnModuleHidden;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeController();
    }
    
    private void InitializeController()
    {
        _moduleRegistry = new Dictionary<string, IUIModule>();
        _navigationStack = new Stack<string>();
        
        // Автоматическая регистрация модулей, назначенных в инспекторе
        foreach (var module in registeredModules)
        {
            RegisterModule(module);
        }
    }
    
    /// <summary>
    /// Регистрирует UI модуль в системе. Модуль должен быть зарегистрирован
    /// перед использованием через ShowModule.
    /// </summary>
    private void RegisterModule(IUIModule module)
    {
        if (module == null)
        {
            Debug.LogError("Attempted to register null module");
            return;
        }
        
        if (_moduleRegistry.ContainsKey(module.ModuleName))
        {
            Debug.LogWarning($"Module {module.ModuleName} is already registered");
            return;
        }
        
        module.Initialize();
        _moduleRegistry[module.ModuleName] = module;
        Debug.Log($"Module {module.ModuleName} registered successfully");
    }
    
    /// <summary>
    /// Отображает указанный модуль. Если hideOthers = true, скрывает все остальные модули.
    /// </summary>
    public void ShowModule(string moduleName, bool hideOthers = true)
    {
        if (!_moduleRegistry.TryGetValue(moduleName, out IUIModule module))
        {
            Debug.LogError($"Module {moduleName} not found in registry");
            return;
        }
        
        if (hideOthers)
        {
            HideAllModules(moduleName);
        }
        
        module.Show();
        _currentModule = module;
        _navigationStack.Push(moduleName);
        
        OnModuleShown?.Invoke(moduleName);
    }
    
    /// <summary>
    /// Скрывает указанный модуль.
    /// </summary>
    public void HideModule(string moduleName)
    {
        if (!_moduleRegistry.TryGetValue(moduleName, out IUIModule module))
        {
            Debug.LogError($"Module {moduleName} not found in registry");
            return;
        }
        
        module.Hide();
        OnModuleHidden?.Invoke(moduleName);
    }
    
    /// <summary>
    /// Скрывает все модули, кроме указанного в параметре exception.
    /// </summary>
    public void HideAllModules(string exception = null)
    {
        foreach (var kvp in _moduleRegistry)
        {
            if (kvp.Key != exception && kvp.Value.IsVisible)
            {
                kvp.Value.Hide();
            }
        }
    }
    
    /// <summary>
    /// Возвращается к предыдущему модулю в стеке навигации.
    /// </summary>
    public void NavigateBack()
    {
        if (_navigationStack.Count <= 1)
        {
            Debug.LogWarning("Navigation stack is empty or at root");
            return;
        }
        
        // Убираем текущий модуль из стека
        _navigationStack.Pop();
        
        // Показываем предыдущий модуль
        if (_navigationStack.Count > 0)
        {
            string previousModule = _navigationStack.Peek();
            ShowModule(previousModule, true);
        }
    }
    
    /// <summary>
    /// Получает ссылку на зарегистрированный модуль по имени.
    /// </summary>
    public T GetModule<T>() where T : class, IUIModule
    {
        string moduleName = typeof(T).Name;
        if (_moduleRegistry.TryGetValue(moduleName, out IUIModule module))
        {
            return module as T;
        }
        return null;
    }
    
    /// <summary>
    /// Проверяет, зарегистрирован ли модуль с указанным именем.
    /// </summary>
    public bool IsModuleRegistered(string moduleName)
    {
        return _moduleRegistry.ContainsKey(moduleName);
    }
    
    private void OnDestroy()
    {
        // Очистка всех модулей при уничтожении контроллера
        foreach (var module in _moduleRegistry.Values)
        {
            module.Cleanup();
        }
    }
}