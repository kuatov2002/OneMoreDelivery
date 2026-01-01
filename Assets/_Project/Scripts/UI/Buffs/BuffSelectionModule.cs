using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic selection module that presents choices and handles selection.
/// Decoupled from specific choice implementations through IBuffChoice interface.
/// </summary>
public class BuffSelectionModule : UIModule
{
    [Header("Selection Configuration")]
    [SerializeField] private BuffCard buffCardPrefab;
    [SerializeField] private Transform cardsContainer;
    
    private readonly List<BuffCard> _activeCards = new();
    private Action<int> _onSelectionCallback;
    
    /// <summary>
    /// Shows the selection UI with provided choices and callback.
    /// </summary>
    public void ShowWithChoices(IReadOnlyList<IBuffChoice> choices, Action<int> onSelected)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError("Cannot show selection: no choices provided");
            return;
        }
        
        if (onSelected == null)
        {
            Debug.LogError("Cannot show selection: no callback provided");
            return;
        }
        
        _onSelectionCallback = onSelected;
        ClearCards();
        
        for (int i = 0; i < choices.Count; i++)
        {
            CreateCard(choices[i], i);
        }
        
        Show();
    }
    
    protected override void OnHide()
    {
        ClearCards();
        _onSelectionCallback = null;
    }
    
    protected override void OnCleanup()
    {
        ClearCards();
        _onSelectionCallback = null;
    }
    
    private void CreateCard(IBuffChoice choice, int index)
    {
        if (buffCardPrefab == null || cardsContainer == null)
        {
            Debug.LogError("BuffCard prefab or container not assigned");
            return;
        }
        
        var card = Instantiate(buffCardPrefab, cardsContainer);
        card.SetData(choice, index, HandleCardClick);
        _activeCards.Add(card);
    }
    
    private void HandleCardClick(int index)
    {
        _onSelectionCallback?.Invoke(index);
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
    }
}

/// <summary>
/// Service that handles buff selection logic and choice management.
/// Separates UI concerns from game logic.
/// </summary>
public class BuffSelectionService
{
    private readonly List<IBuffChoice> _availableChoices;
    
    public BuffSelectionService(List<IBuffChoice> availableChoices)
    {
        _availableChoices = availableChoices ?? throw new ArgumentNullException(nameof(availableChoices));
    }
    
    /// <summary>
    /// Presents a random selection of choices to the player.
    /// </summary>
    public void ShowRandomSelection(int count, Action<IBuffChoice> onChoiceSelected)
    {
        if (_availableChoices.Count == 0)
        {
            Debug.LogWarning("No choices available for selection");
            onChoiceSelected?.Invoke(null);
            return;
        }
        
        var randomChoices = GetRandomChoices(count);
        var selectionModule = HUD.Instance.GetModule<BuffSelectionModule>();
        
        if (selectionModule == null)
        {
            Debug.LogError("BuffSelectionModule not found");
            onChoiceSelected?.Invoke(null);
            return;
        }
        
        selectionModule.ShowWithChoices(randomChoices, index =>
        {
            if (index >= 0 && index < randomChoices.Count)
            {
                onChoiceSelected?.Invoke(randomChoices[index]);
            }
            else
            {
                Debug.LogError($"Invalid selection index: {index}");
                onChoiceSelected?.Invoke(null);
            }
        });
    }
    
    private List<IBuffChoice> GetRandomChoices(int count)
    {
        var shuffled = new List<IBuffChoice>(_availableChoices);
        
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }
        
        int takeCount = Mathf.Min(count, shuffled.Count);
        return shuffled.GetRange(0, takeCount);
    }
}