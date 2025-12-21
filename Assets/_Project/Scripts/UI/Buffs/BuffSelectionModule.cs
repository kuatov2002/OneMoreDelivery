using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Модуль окна выбора из 3 вариантов. Работает с любыми данными через IBuffChoice.
/// Вызывает callback с индексом выбранного варианта.
/// </summary>
public class BuffSelectionModule : UIModule
{
    [Header("Selection Configuration")]
    [SerializeField] private int numberOfChoices = 3;
    [SerializeField] private BuffCard buffCardPrefab;
    [SerializeField] private Transform cardsContainer;
    
    private List<BuffCard> _activeCards = new();
    private List<IBuffChoice> _currentChoices = new();
    
    public event Action<int> OnChoiceSelected;
    public event Action OnSelectionCompleted;
    
    protected override void OnHide()
    {
        ClearCards();
    }
    
    protected override void OnCleanup()
    {
        ClearCards();
    }
    
    /// <summary>
    /// Показывает окно выбора с указанными вариантами.
    /// </summary>
    public void ShowWithChoices(List<IBuffChoice> choices)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError("No choices provided for selection");
            return;
        }
        
        ClearCards();
        _currentChoices = choices;
        
        int displayCount = Mathf.Min(numberOfChoices, choices.Count);
        
        for (int i = 0; i < displayCount; i++)
        {
            CreateCard(choices[i], i);
        }
        
        Show();
    }
    
    private void CreateCard(IBuffChoice choice, int index)
    {
        if (buffCardPrefab == null || cardsContainer == null)
        {
            Debug.LogError("BuffCard prefab or container is not assigned");
            return;
        }
        
        var card = Instantiate(buffCardPrefab, cardsContainer);
        card.SetData(choice, index, HandleChoiceSelection);
        _activeCards.Add(card);
    }
    
    private void HandleChoiceSelection(int index)
    {
        OnChoiceSelected?.Invoke(index);
        OnSelectionCompleted?.Invoke();
        Hide();
    }
    
    private void ClearCards()
    {
        foreach (var card in _activeCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        
        _activeCards.Clear();
        _currentChoices.Clear();
    }
    
    public void SetNumberOfChoices(int count)
    {
        numberOfChoices = Mathf.Max(1, count);
    }
}