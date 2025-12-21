using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Представляет визуальное отображение одного выбора.
/// Работает с любыми данными через IBuffChoice.
/// </summary>
public class BuffCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;
    
    private int _currentIndex;
    private Action<int> _onSelected;
    
    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }
    
    /// <summary>
    /// Привязывает данные к UI элементам карточки.
    /// </summary>
    public void SetData(IBuffChoice choice, int index, Action<int> onSelectCallback)
    {
        _currentIndex = index;
        _onSelected = onSelectCallback;
        
        if (nameText != null)
            nameText.text = choice.GetName();
        
        if (descriptionText != null)
            descriptionText.text = choice.GetDescription();
        
        if (iconImage != null && choice.GetIcon() != null)
            iconImage.sprite = choice.GetIcon();
        
        if (backgroundImage != null)
            backgroundImage.color = choice.GetBackgroundColor();
    }
    
    private void OnSelectClicked()
    {
        _onSelected?.Invoke(_currentIndex);
    }
    
    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectClicked);
        }
    }
}

/// <summary>
/// Интерфейс для данных выбора. Позволяет использовать любые данные
/// (например, Instructions из Game Creator 2).
/// </summary>
public interface IBuffChoice
{
    string GetName();
    string GetDescription();
    Sprite GetIcon();
    Color GetBackgroundColor();
}