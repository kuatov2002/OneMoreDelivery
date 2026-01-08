using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Центральный менеджер карты в стиле Slay the Spire.
/// ОБНОВЛЕНО: Сохраняет и восстанавливает прогресс через RunData
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("References")]
    [Tooltip("Ссылка на генератор карты")]
    public MapGenerator mapGenerator;
    
    [Tooltip("Ссылка на отрисовщик путей")]
    public PathDrawer pathDrawer;
    
    [Header("Camera Settings")]
    [Tooltip("Камера, которая будет следить за текущим узлом")]
    public Camera mapCamera;
    
    [Tooltip("Скорость перемещения камеры к узлу")]
    public float cameraMovementSpeed = 2.5f;
    
    [Tooltip("Отступ камеры по Y для лучшего обзора")]
    public float cameraYOffset = -3f;
    
    [Tooltip("Размер камеры (Orthographic Size)")]
    [Range(3f, 15f)]
    public float cameraSize = 8f;
    
    [Header("Visual Effects")]
    [Tooltip("Показывать анимацию при переходе между узлами")]
    public bool showTransitionAnimation = true;
    
    [Header("Persistence")]
    [Tooltip("Автоматически восстанавливать прогресс при старте")]
    public bool autoRestoreProgress = true;
    
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
    
    private void SetupCamera()
    {
        if (mapCamera != null && mapCamera.orthographic)
        {
            mapCamera.orthographicSize = cameraSize;
        }
    }
    
    /// <summary>
    /// Генерирует новую карту и восстанавливает прогресс если есть
    /// </summary>
    public void GenerateNewMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator не назначен в MapManager!");
            return;
        }
        
        mapGenerator.GenerateMap();
        
        if (autoRestoreProgress && RunData.MapProgress.HasProgress)
        {
            RestoreProgressFromRunData();
        }
        else
        {
            InitializeNewRun();
        }
        
        if (pathDrawer != null)
        {
            pathDrawer.DrawAllPaths(mapGenerator.GetAllNodes());
            pathDrawer.UpdateAllLineColors(mapGenerator.GetAllNodes());
        }
    }
    
    /// <summary>
    /// Инициализирует новое прохождение с начала
    /// </summary>
    private void InitializeNewRun()
    {
        List<List<MapNode>> layers = mapGenerator.GetLayers();
        if (layers.Count > 0 && layers[0].Count > 0)
        {
            currentNode = layers[0][0];
            UpdateAvailableNodes();
            SaveProgressToRunData();
            
            if (mapCamera != null)
            {
                Vector3 startPosition = currentNode.GetPosition();
                startPosition.y += cameraYOffset;
                startPosition.z = mapCamera.transform.position.z;
                mapCamera.transform.position = startPosition;
                targetCameraPosition = startPosition;
            }
        }
    }
    
    /// <summary>
    /// Восстанавливает прогресс из RunData
    /// </summary>
    private void RestoreProgressFromRunData()
    {
        Debug.Log($"Восстановление прогресса: Layer {RunData.MapProgress.currentNodeLayer}, Position {RunData.MapProgress.currentNodePosition}");
        
        List<List<MapNode>> layers = mapGenerator.GetLayers();
        
        // Восстанавливаем пройденные узлы
        completedNodes.Clear();
        foreach (var nodeId in RunData.MapProgress.completedNodes)
        {
            MapNode node = FindNode(nodeId.layer, nodeId.position);
            if (node != null)
            {
                completedNodes.Add(node);
                node.SetState(NodeState.Completed);
            }
        }
        
        // Восстанавливаем текущий узел
        currentNode = FindNode(RunData.MapProgress.currentNodeLayer, RunData.MapProgress.currentNodePosition);
        
        if (currentNode != null)
        {
            currentNode.SetState(NodeState.Current);
            UpdateAvailableNodes();
            
            if (mapCamera != null)
            {
                Vector3 nodePosition = currentNode.GetPosition();
                nodePosition.y += cameraYOffset;
                nodePosition.z = mapCamera.transform.position.z;
                mapCamera.transform.position = nodePosition;
                targetCameraPosition = nodePosition;
            }
            
            Debug.Log($"Прогресс восстановлен: {completedNodes.Count} узлов пройдено, текущий узел: {currentNode.nodeType}");
        }
        else
        {
            Debug.LogWarning("Не удалось найти текущий узел, начинаем с начала");
            InitializeNewRun();
        }
    }
    
    /// <summary>
    /// Находит узел по координатам
    /// </summary>
    private MapNode FindNode(int layer, int position)
    {
        List<List<MapNode>> layers = mapGenerator.GetLayers();
        
        if (layer >= 0 && layer < layers.Count)
        {
            List<MapNode> layerNodes = layers[layer];
            if (position >= 0 && position < layerNodes.Count)
            {
                return layerNodes[position];
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Сохраняет прогресс в RunData
    /// </summary>
    private void SaveProgressToRunData()
    {
        if (currentNode == null) return;
        
        RunData.MapProgress.SetCurrentNode(currentNode.layer, currentNode.positionInLayer);
        
        RunData.MapProgress.completedNodes.Clear();
        foreach (var node in completedNodes)
        {
            RunData.MapProgress.AddCompletedNode(node.layer, node.positionInLayer);
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
        if (currentNode != null)
        {
            completedNodes.Add(currentNode);
            currentNode.SetState(NodeState.Completed);
        }
        
        currentNode = targetNode;
        currentNode.SetState(NodeState.Current);
        
        UpdateAvailableNodes();
        SaveProgressToRunData();
        
        if (pathDrawer != null)
        {
            pathDrawer.UpdateAllLineColors(mapGenerator.GetAllNodes());
        }
        
        MoveCameraToNode(targetNode);
        TriggerNodeEvent(targetNode);
        
        Debug.Log($"Перемещение к узлу: {targetNode.nodeType} на слое {targetNode.layer}");
    }
    
    /// <summary>
    /// Обновляет список доступных для выбора узлов
    /// </summary>
    private void UpdateAvailableNodes()
    {
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
        switch (node.nodeType)
        {
            case NodeType.Combat:
                Debug.Log("Начинается бой!");
                SceneManager.LoadScene("City");
                break;
                
            case NodeType.EliteCombat:
                Debug.Log("Начинается элитный бой!");
                break;
                
            case NodeType.Boss:
                Debug.Log("Битва с боссом!");
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
    
    public MapNode GetCurrentNode() => currentNode;
    public List<MapNode> GetAvailableNodes() => new List<MapNode>(availableNodes);
    public List<MapNode> GetCompletedNodes() => new List<MapNode>(completedNodes);
    
    public bool IsMapCompleted()
    {
        return currentNode != null && currentNode.nodeType == NodeType.Boss;
    }
    
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
        RunData.ResetMapOnly();
        GenerateNewMap();
    }
    
    /// <summary>
    /// Форсирует сохранение текущего прогресса
    /// </summary>
    public void ForceSaveProgress()
    {
        SaveProgressToRunData();
        Debug.Log("Прогресс сохранен вручную");
    }
    
    public void HighlightAvailablePaths(bool highlight)
    {
        if (pathDrawer == null || currentNode == null) return;
        
        foreach (MapNode availableNode in availableNodes)
        {
            pathDrawer.HighlightPath(currentNode, availableNode, highlight);
        }
    }
    
    public void SetCameraSize(float size)
    {
        cameraSize = Mathf.Clamp(size, 3f, 15f);
        if (mapCamera != null && mapCamera.orthographic)
        {
            mapCamera.orthographicSize = cameraSize;
        }
    }
    
    public float GetCameraSize() => cameraSize;
}