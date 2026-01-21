using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Класс представляет один узел на карте в стиле Slay the Spire.
/// УЛУЧШЕНО: Добавлена цветовая кодировка узлов по типам, как в оригинальной игре
/// ИСПРАВЛЕНО: Использует новый Input System вместо legacy Input
/// </summary>
public class MapNode : MonoBehaviour
{
    [Header("Node Properties")]
    [Tooltip("Тип узла определяет, что произойдет при его посещении")]
    public NodeType nodeType;
    
    [Tooltip("Слой, на котором находится узел (0 = старт, увеличивается к финишу)")]
    public int layer;
    
    [Tooltip("Позиция узла внутри слоя (для расположения узлов по горизонтали)")]
    public int positionInLayer;
    
    [Header("Visual Settings")]
    [Tooltip("Цвет узла в нормальном состоянии")]
    public Color normalColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    
    [Tooltip("Цвет узла когда он доступен для выбора")]
    public Color availableColor = new Color(0.7f, 0.9f, 1f, 1f);
    
    [Tooltip("Цвет узла после его прохождения")]
    public Color completedColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    
    [Tooltip("Цвет текущего узла")]
    public Color currentColor = new Color(1f, 0.9f, 0.3f, 1f);
    
    [Header("References")]
    public SpriteRenderer iconRenderer;
    public SpriteRenderer backgroundRenderer;
    public GameObject selectionGlow;
    
    [Header("Node Icons")]
    public Sprite combatIcon;
    public Sprite eliteCombatIcon;
    public Sprite bossIcon;
    public Sprite treasureIcon;
    public Sprite shopIcon;
    public Sprite restSiteIcon;
    public Sprite randomEventIcon;
    public Sprite mysteryIcon;
    public Sprite startIcon;
    
    private NodeState currentState = NodeState.Locked;
    public List<MapNode> connectedNodes = new List<MapNode>();
    
    private Vector3 originalScale;
    private bool isHovered = false;
    private Camera mainCamera;
    
    private float pulseTimer = 0f;
    private const float pulseSpeed = 2f;
    private const float pulseAmount = 0.1f;

    void Start()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (currentState == NodeState.Available)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float scale = 1f + Mathf.Sin(pulseTimer) * pulseAmount;
            
            if (!isHovered)
            {
                transform.localScale = originalScale * scale;
            }
        }
        
        if (currentState != NodeState.Available) return;
    
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
    
        if (hit.collider != null && hit.collider.gameObject == this.gameObject)
        {
            if (!isHovered)
            {
                OnMouseEnter();
            }
        
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                MapManager.Instance?.OnNodeSelected(this);
            }
        }
        else if (isHovered)
        {
            OnMouseExit();
        }
    }
    
    private void OnMouseEnter()
    {
        isHovered = true;
        transform.localScale = originalScale * 1.2f;
        if (selectionGlow != null) selectionGlow.SetActive(true);
    }
    
    private void OnMouseExit()
    {
        isHovered = false;
        transform.localScale = originalScale;
        
        if (currentState != NodeState.Current && selectionGlow != null)
        {
            selectionGlow.SetActive(false);
        }
    }
    
    public void SetState(NodeState newState)
    {
        currentState = newState;
        UpdateVisuals();
    }
    
    public NodeState GetState()
    {
        return currentState;
    }
    
    public void UpdateIcon()
    {
        if (iconRenderer == null) return;
        
        Sprite iconToUse = null;
        
        switch (nodeType)
        {
            case NodeType.Start:
                iconToUse = startIcon;
                break;
            case NodeType.Combat:
                iconToUse = combatIcon;
                break;
            case NodeType.EliteCombat:
                iconToUse = eliteCombatIcon;
                break;
            case NodeType.Boss:
                iconToUse = bossIcon;
                break;
            case NodeType.Treasure:
                iconToUse = treasureIcon;
                break;
            case NodeType.Shop:
                iconToUse = shopIcon;
                break;
            case NodeType.RestSite:
                iconToUse = restSiteIcon;
                break;
            case NodeType.RandomEvent:
                iconToUse = randomEventIcon;
                break;
            case NodeType.Mystery:
                iconToUse = mysteryIcon;
                break;
        }
        
        if (iconToUse != null)
        {
            iconRenderer.sprite = iconToUse;
        }
    }
    
    private void UpdateVisuals()
    {
        if (backgroundRenderer == null) return;
        
        Color baseColor = GetNodeTypeColor();
        
        switch (currentState)
        {
            case NodeState.Locked:
                backgroundRenderer.color = baseColor * 0.3f;
                if (iconRenderer != null) 
                    iconRenderer.color = new Color(1f, 1f, 1f, 0.3f);
                if (selectionGlow != null) 
                    selectionGlow.SetActive(false);
                break;
                
            case NodeState.Available:
                backgroundRenderer.color = baseColor * 1.2f;
                if (iconRenderer != null) 
                    iconRenderer.color = Color.white;
                break;
                
            case NodeState.Completed:
                backgroundRenderer.color = baseColor * 0.5f;
                if (iconRenderer != null) 
                    iconRenderer.color = new Color(1f, 1f, 1f, 0.6f);
                if (selectionGlow != null) 
                    selectionGlow.SetActive(false);
                break;
                
            case NodeState.Current:
                Color currentNodeColor = Color.Lerp(baseColor, currentColor, 0.5f);
                backgroundRenderer.color = currentNodeColor * 1.3f;
                if (iconRenderer != null) 
                    iconRenderer.color = Color.white;
                if (selectionGlow != null) 
                    selectionGlow.SetActive(true);
                break;
        }
    }
    
    public void AddConnection(MapNode targetNode)
    {
        if (!connectedNodes.Contains(targetNode))
        {
            connectedNodes.Add(targetNode);
        }
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public Color GetNodeTypeColor()
    {
        switch (nodeType)
        {
            case NodeType.Start:
                return new Color(0.4f, 0.6f, 0.9f);
            case NodeType.Combat:
                return new Color(0.85f, 0.25f, 0.25f);
            case NodeType.EliteCombat:
                return new Color(0.75f, 0.2f, 0.75f);
            case NodeType.Boss:
                return new Color(0.9f, 0.1f, 0.1f);
            case NodeType.Treasure:
                return new Color(0.95f, 0.75f, 0.2f);
            case NodeType.Shop:
                return new Color(0.25f, 0.75f, 0.35f);
            case NodeType.RestSite:
                return new Color(0.3f, 0.65f, 0.85f);
            case NodeType.RandomEvent:
                return new Color(0.85f, 0.45f, 0.2f);
            case NodeType.Mystery:
                return new Color(0.55f, 0.55f, 0.75f);
            default:
                return new Color(0.5f, 0.5f, 0.5f);
        }
    }
}

public enum NodeType
{
    Start,
    Combat,
    EliteCombat,
    Boss,
    Treasure,
    Shop,
    RestSite,
    RandomEvent,
    Mystery
}

public enum NodeState
{
    Locked,
    Available,
    Current,
    Completed
}