using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Центральный менеджер карты в стиле Slay the Spire.
/// Управляет текущим состоянием прохождения, обрабатывает выбор узлов игроком,
/// и координирует переходы между узлами.
/// УЛУЧШЕНО: Настроена камера для работы с увеличенными расстояниями
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("References")]
    [Tooltip("Ссылка на генератор карты")]
    public MapGenerator mapGenerator;
    
    [Tooltip("Ссылка на отрисовщик путей")]
    public PathDrawer pathDrawer;
    
    [Header("Camera Settings - Настроены для больших расстояний")]
    [Tooltip("Камера, которая будет следить за текущим узлом")]
    public Camera mapCamera;
    
    [Tooltip("Скорость перемещения камеры к узлу")]
    public float cameraMovementSpeed = 2.5f;
    
    [Tooltip("Отступ камеры по Y для лучшего обзора (увеличен)")]
    public float cameraYOffset = -3f;
    
    [Tooltip("Размер камеры (Orthographic Size). Увеличьте для большего обзора")]
    [Range(3f, 15f)]
    public float cameraSize = 8f;
    
    [Header("Visual Effects")]
    [Tooltip("Показывать анимацию при переходе между узлами")]
    public bool showTransitionAnimation = true;
    
    // Текущее состояние
    private MapNode currentNode;
    private List<MapNode> availableNodes = new List<MapNode>();
    private List<MapNode> completedNodes = new List<MapNode>();
    
    // Для плавного перемещения камеры
    private Vector3 targetCameraPosition;
    private bool isCameraMoving = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        SetupCamera();
        GenerateNewMap();
    }
    
    void Update()
    {
        // Плавное перемещение камеры
        if (isCameraMoving && mapCamera != null)
        {
            Vector3 currentPos = mapCamera.transform.position;
            Vector3 newPos = Vector3.Lerp(currentPos, targetCameraPosition, Time.deltaTime * cameraMovementSpeed);
            newPos.z = currentPos.z;
            mapCamera.transform.position = newPos;
            
            if (Vector3.Distance(newPos, targetCameraPosition) < 0.1f)
            {
                isCameraMoving = false;
            }
        }
    }
    
    /// <summary>
    /// Настраивает камеру для карты
    /// </summary>
    private void SetupCamera()
    {
        if (mapCamera != null && mapCamera.orthographic)
        {
            mapCamera.orthographicSize = cameraSize;
        }
    }
    
    /// <summary>
    /// Генерирует новую карту
    /// </summary>
    public void GenerateNewMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator не назначен в MapManager!");
            return;
        }
        
        mapGenerator.GenerateMap();
        
        List<List<MapNode>> layers = mapGenerator.GetLayers();
        if (layers.Count > 0 && layers[0].Count > 0)
        {
            currentNode = layers[0][0];
            UpdateAvailableNodes();
            
            if (mapCamera != null)
            {
                Vector3 startPosition = currentNode.GetPosition();
                startPosition.y += cameraYOffset;
                startPosition.z = mapCamera.transform.position.z;
                mapCamera.transform.position = startPosition;
                targetCameraPosition = startPosition;
            }
        }
        
        if (pathDrawer != null)
        {
            pathDrawer.DrawAllPaths(mapGenerator.GetAllNodes());
        }
    }
    
    /// <summary>
    /// Вызывается когда игрок выбирает узел
    /// </summary>
    public void OnNodeSelected(MapNode selectedNode)
    {
        if (!availableNodes.Contains(selectedNode))
        {
            Debug.Log("Этот узел недоступен!");
            return;
        }
        
        // Анимация линии при выборе
        if (showTransitionAnimation && pathDrawer != null)
        {
            pathDrawer.AnimateLine(currentNode, selectedNode);
        }
        
        MoveToNode(selectedNode);
    }
    
    /// <summary>
    /// Перемещает игрока к выбранному узлу
    /// </summary>
    private void MoveToNode(MapNode targetNode)
    {
        // Помечаем текущий узел как пройденный
        if (currentNode != null)
        {
            completedNodes.Add(currentNode);
            currentNode.SetState(NodeState.Completed);
        }
        
        // Устанавливаем новый текущий узел
        currentNode = targetNode;
        currentNode.SetState(NodeState.Current);
        
        // Обновляем доступные узлы
        UpdateAvailableNodes();
        
        // Обновляем цвета линий
        if (pathDrawer != null)
        {
            pathDrawer.UpdateAllLineColors(mapGenerator.GetAllNodes());
        }
        
        // Перемещаем камеру
        MoveCameraToNode(targetNode);
        
        // Запускаем событие узла
        TriggerNodeEvent(targetNode);
        
        Debug.Log($"Перемещение к узлу: {targetNode.nodeType} на слое {targetNode.layer}");
    }
    
    /// <summary>
    /// Обновляет список доступных для выбора узлов
    /// </summary>
    private void UpdateAvailableNodes()
    {
        // Сбрасываем состояния предыдущих доступных узлов
        foreach (MapNode node in availableNodes)
        {
            if (node.GetState() == NodeState.Available)
            {
                node.SetState(NodeState.Locked);
            }
        }
        
        availableNodes.Clear();
        
        if (currentNode != null)
        {
            foreach (MapNode connectedNode in currentNode.connectedNodes)
            {
                if (!completedNodes.Contains(connectedNode))
                {
                    availableNodes.Add(connectedNode);
                    connectedNode.SetState(NodeState.Available);
                }
            }
        }
        
        Debug.Log($"Доступно узлов: {availableNodes.Count}");
    }
    
    /// <summary>
    /// Плавно перемещает камеру к узлу
    /// </summary>
    private void MoveCameraToNode(MapNode node)
    {
        if (mapCamera != null)
        {
            targetCameraPosition = node.GetPosition();
            targetCameraPosition.y += cameraYOffset;
            targetCameraPosition.z = mapCamera.transform.position.z;
            isCameraMoving = true;
        }
    }
    
    /// <summary>
    /// Запускает событие, связанное с типом узла
    /// </summary>
    private void TriggerNodeEvent(MapNode node)
    {
        // Иначе используем простую логику
        switch (node.nodeType)
        {
            case NodeType.Combat:
                Debug.Log("Начинается бой!");
                break;
                
            case NodeType.EliteCombat:
                Debug.Log("Начинается элитный бой!");
                break;
                
            case NodeType.Boss:
                Debug.Log("Битва с боссом!");
                OnBossDefeated();
                break;
                
            case NodeType.Treasure:
                Debug.Log("Найдено сокровище!");
                break;
                
            case NodeType.Shop:
                Debug.Log("Добро пожаловать в магазин!");
                break;
                
            case NodeType.RestSite:
                Debug.Log("Место для отдыха");
                break;
                
            case NodeType.RandomEvent:
                Debug.Log("Случайное событие!");
                break;
                
            case NodeType.Mystery:
                Debug.Log("Неизвестное событие...");
                break;
        }
    }
    
    /// <summary>
    /// Вызывается после победы над боссом
    /// </summary>
    private void OnBossDefeated()
    {
        Debug.Log("Вы победили босса! Карта пройдена!");
        // Здесь можно показать экран победы, перейти к следующему акту и т.д.
    }
    
    /// <summary>
    /// Возвращает текущий узел
    /// </summary>
    public MapNode GetCurrentNode()
    {
        return currentNode;
    }
    
    /// <summary>
    /// Возвращает список доступных узлов
    /// </summary>
    public List<MapNode> GetAvailableNodes()
    {
        return new List<MapNode>(availableNodes);
    }
    
    /// <summary>
    /// Возвращает список пройденных узлов
    /// </summary>
    public List<MapNode> GetCompletedNodes()
    {
        return new List<MapNode>(completedNodes);
    }
    
    /// <summary>
    /// Проверяет, достиг ли игрок конца карты
    /// </summary>
    public bool IsMapCompleted()
    {
        return currentNode != null && currentNode.nodeType == NodeType.Boss;
    }
    
    /// <summary>
    /// Возвращает прогресс прохождения карты (0-1)
    /// </summary>
    public float GetMapProgress()
    {
        if (currentNode == null || mapGenerator == null) return 0f;
        
        return (float)currentNode.layer / (mapGenerator.numberOfLayers - 1);
    }
    
    /// <summary>
    /// Сброс карты для новой игры
    /// </summary>
    public void ResetMap()
    {
        completedNodes.Clear();
        availableNodes.Clear();
        currentNode = null;
        GenerateNewMap();
    }
    
    /// <summary>
    /// Подсвечивает доступные пути от текущего узла
    /// </summary>
    public void HighlightAvailablePaths(bool highlight)
    {
        if (pathDrawer == null || currentNode == null) return;
        
        foreach (MapNode availableNode in availableNodes)
        {
            pathDrawer.HighlightPath(currentNode, availableNode, highlight);
        }
    }
    
    /// <summary>
    /// Изменяет размер камеры (полезно для масштабирования)
    /// </summary>
    public void SetCameraSize(float size)
    {
        cameraSize = Mathf.Clamp(size, 3f, 15f);
        if (mapCamera != null && mapCamera.orthographic)
        {
            mapCamera.orthographicSize = cameraSize;
        }
    }
    
    /// <summary>
    /// Возвращает текущий размер камеры
    /// </summary>
    public float GetCameraSize()
    {
        return cameraSize;
    }
}