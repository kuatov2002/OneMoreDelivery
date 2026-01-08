using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Улучшенный генератор карты в стиле Slay the Spire.
/// ОБНОВЛЕНО: Использует RunData.Seed для воспроизводимости
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Map Structure Settings")]
    [Tooltip("Количество слоев на карте (15 стандартно для Slay the Spire)")]
    [Range(2, 20)]
    public int numberOfLayers = 15;
    
    [Tooltip("Минимальное количество узлов в слое")]
    [Range(1, 7)]
    public int minNodesPerLayer = 5;
    
    [Tooltip("Максимальное количество узлов в слое")]
    [Range(1, 10)]
    public int maxNodesPerLayer = 7;
    
    [Header("Spacing Settings")]
    [Tooltip("Расстояние между слоями по вертикали")]
    public float layerSpacing = 4.5f;
    
    [Tooltip("Расстояние между узлами по горизонтали")]
    public float nodeSpacing = 3.5f;
    
    [Tooltip("Максимальное случайное смещение по X (уменьшено для более предсказуемых путей)")]
    [Range(0f, 1f)]
    public float randomOffsetXAmount = 0.1f;
    
    [Tooltip("Максимальное случайное смещение по Y")]
    [Range(0f, 1f)]
    public float randomOffsetYAmount = 0.08f;
    
    [Header("Path Settings - Улучшенные")]
    [Tooltip("Вероятность ветвления пути (один узел -> два узла)")]
    [Range(0f, 1f)]
    public float branchProbability = 0.35f;
    
    [Tooltip("Вероятность схождения путей")]
    [Range(0f, 1f)]
    public float convergeProbability = 0.3f;
    
    [Tooltip("Максимальная разница в 'колоннах' для создания связи")]
    [Range(0, 2)]
    public int maxColumnDistance = 1;
    
    [Tooltip("Предотвращать пересечения путей")]
    public bool preventPathCrossing = true;
    
    [Tooltip("Балансировать количество входящих связей")]
    public bool balanceIncomingConnections = true;
    
    [Header("Node Distribution")]
    [Tooltip("Вероятности появления разных типов узлов")]
    public NodeDistribution nodeDistribution;
    
    [Header("Prefabs")]
    [Tooltip("Префаб узла")]
    public GameObject nodePrefab;
    
    [Tooltip("Родительский объект для всех узлов")]
    public Transform nodesParent;
    
    private class NodeColumn
    {
        public int columnIndex;
        public List<MapNode> nodesInColumn = new List<MapNode>();
    }
    
    private readonly List<List<MapNode>> _layers = new List<List<MapNode>>();
    private readonly List<List<NodeColumn>> _layerColumns = new List<List<NodeColumn>>();
    private System.Random _random;
    
    public void GenerateMap()
    {
        InitializeRandom();
        ClearMap();
        CreateLayers();
        AssignNodesColumns();
        PositionNodesInColumns();
        CreateImprovedConnections();
        AssignNodeTypes();
        InitializeMapState();
        
        Debug.Log($"Map generated with seed {RunData.Seed}: {numberOfLayers} layers, {GetTotalNodeCount()} nodes");
    }
    
    private void InitializeRandom()
    {
        _random = new System.Random(RunData.Seed);
        Random.InitState(RunData.Seed);
    }
    
    private void ClearMap()
    {
        _layers.Clear();
        _layerColumns.Clear();
        
        if (nodesParent != null)
        {
            foreach (Transform child in nodesParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void CreateLayers()
    {
        for (int layerIndex = 0; layerIndex < numberOfLayers; layerIndex++)
        {
            List<MapNode> currentLayer = new List<MapNode>();
            int nodeCount = GetSmartNodeCountForLayer(layerIndex);
            
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                GameObject nodeObj = Instantiate(nodePrefab, nodesParent);
                nodeObj.name = $"Node_L{layerIndex}_N{nodeIndex}";
                
                MapNode node = nodeObj.GetComponent<MapNode>();
                if (node != null)
                {
                    node.layer = layerIndex;
                    node.positionInLayer = nodeIndex;
                    currentLayer.Add(node);
                }
            }
            
            _layers.Add(currentLayer);
        }
    }
    
    private int GetSmartNodeCountForLayer(int layerIndex)
    {
        if (layerIndex == 0) return 1;
        if (layerIndex == numberOfLayers - 1) return 1;
        if (layerIndex == numberOfLayers - 2) return _random.Next(2, 4);
        
        float progress = (float)layerIndex / (numberOfLayers - 1);
        
        if (progress < 0.2f)
        {
            return _random.Next(minNodesPerLayer, minNodesPerLayer + 2);
        }
        else if (progress < 0.65f)
        {
            return _random.Next(minNodesPerLayer + 1, maxNodesPerLayer + 1);
        }
        else
        {
            float narrowingFactor = (progress - 0.65f) / 0.35f;
            int maxNodes = maxNodesPerLayer - Mathf.RoundToInt(narrowingFactor * 3f);
            return _random.Next(minNodesPerLayer, Mathf.Max(minNodesPerLayer + 1, maxNodes));
        }
    }
    
    private void AssignNodesColumns()
    {
        for (int layerIndex = 0; layerIndex < _layers.Count; layerIndex++)
        {
            List<MapNode> layer = _layers[layerIndex];
            List<NodeColumn> columns = new List<NodeColumn>();
            
            int maxNodesNearby = layer.Count;
            if (layerIndex > 0)
                maxNodesNearby = Mathf.Max(maxNodesNearby, _layers[layerIndex - 1].Count);
            if (layerIndex < _layers.Count - 1)
                maxNodesNearby = Mathf.Max(maxNodesNearby, _layers[layerIndex + 1].Count);
            
            int numColumns = Mathf.Max(layer.Count, maxNodesNearby);
            
            for (int i = 0; i < numColumns; i++)
            {
                columns.Add(new NodeColumn { columnIndex = i });
            }
            
            if (layer.Count == 1)
            {
                int centerColumn = numColumns / 2;
                columns[centerColumn].nodesInColumn.Add(layer[0]);
            }
            else
            {
                float step = (float)(numColumns - 1) / (layer.Count - 1);
                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    int columnIndex = Mathf.RoundToInt(nodeIndex * step);
                    columns[columnIndex].nodesInColumn.Add(layer[nodeIndex]);
                }
            }
            
            _layerColumns.Add(columns);
        }
    }
    
    private void PositionNodesInColumns()
    {
        for (int layerIndex = 0; layerIndex < _layers.Count; layerIndex++)
        {
            List<NodeColumn> columns = _layerColumns[layerIndex];
            int totalColumns = columns.Count;
            
            float totalWidth = (totalColumns - 1) * nodeSpacing;
            float startX = -totalWidth / 2f;
            float yPosition = layerIndex * layerSpacing;
            
            foreach (var column in columns)
            {
                float columnX = startX + (column.columnIndex * nodeSpacing);
                
                int nodesInColumn = column.nodesInColumn.Count;
                for (int i = 0; i < nodesInColumn; i++)
                {
                    MapNode node = column.nodesInColumn[i];
                    
                    float verticalOffset = 0f;
                    if (nodesInColumn > 1)
                    {
                        verticalOffset = (i - (nodesInColumn - 1) / 2f) * 0.3f;
                    }
                    
                    float randomOffsetX = (float)(_random.NextDouble() * 2 - 1) * randomOffsetXAmount;
                    float randomOffsetY = (float)(_random.NextDouble() * 2 - 1) * randomOffsetYAmount;
                    
                    Vector3 position = new Vector3(
                        columnX + randomOffsetX,
                        yPosition + verticalOffset + randomOffsetY,
                        0f
                    );
                    
                    node.transform.position = position;
                }
            }
        }
    }
    
    private void CreateImprovedConnections()
    {
        HashSet<string> existingConnections = new HashSet<string>();
        
        for (int layerIndex = 0; layerIndex < _layers.Count - 1; layerIndex++)
        {
            List<MapNode> currentLayer = _layers[layerIndex];
            List<MapNode> nextLayer = _layers[layerIndex + 1];
            List<NodeColumn> currentColumns = _layerColumns[layerIndex];
            List<NodeColumn> nextColumns = _layerColumns[layerIndex + 1];
            
            foreach (MapNode currentNode in currentLayer)
            {
                int currentColumn = GetNodeColumn(currentNode, currentColumns);
                List<MapNode> viableTargets = GetViableTargetNodes(
                    currentNode, currentColumn, nextColumns, existingConnections
                );
                
                if (viableTargets.Count > 0)
                {
                    MapNode target = viableTargets[0];
                    CreateConnection(currentNode, target, existingConnections);
                    
                    if (viableTargets.Count >= 2 && _random.NextDouble() < branchProbability)
                    {
                        CreateConnection(currentNode, viableTargets[1], existingConnections);
                    }
                }
            }
            
            if (balanceIncomingConnections)
            {
                AddSmartConvergence(currentLayer, nextLayer, currentColumns, nextColumns, existingConnections);
            }
            
            EnsureAllNodesConnected(currentLayer, nextLayer, currentColumns, nextColumns, existingConnections);
        }
    }
    
    private int GetNodeColumn(MapNode node, List<NodeColumn> columns)
    {
        foreach (var column in columns)
        {
            if (column.nodesInColumn.Contains(node))
                return column.columnIndex;
        }

        return 0;
    }
    
    private List<MapNode> GetViableTargetNodes(
        MapNode sourceNode, 
        int sourceColumn,
        List<NodeColumn> targetColumns,
        HashSet<string> existingConnections)
    {
        List<MapNode> viableNodes = new List<MapNode>();
        
        for (int colOffset = 0; colOffset <= maxColumnDistance; colOffset++)
        {
            int rightColumn = sourceColumn + colOffset;
            if (rightColumn >= 0 && rightColumn < targetColumns.Count)
            {
                foreach (var node in targetColumns[rightColumn].nodesInColumn)
                {
                    if (IsConnectionViable(sourceNode, node, existingConnections))
                    {
                        viableNodes.Add(node);
                    }
                }
            }
            
            if (colOffset > 0)
            {
                int leftColumn = sourceColumn - colOffset;
                if (leftColumn >= 0 && leftColumn < targetColumns.Count)
                {
                    foreach (var node in targetColumns[leftColumn].nodesInColumn)
                    {
                        if (IsConnectionViable(sourceNode, node, existingConnections))
                        {
                            viableNodes.Add(node);
                        }
                    }
                }
            }
        }
        
        viableNodes = viableNodes
            .OrderBy(node => Vector3.Distance(sourceNode.GetPosition(), node.GetPosition()))
            .ToList();
        
        return viableNodes;
    }
    
    private bool IsConnectionViable(MapNode source, MapNode target, HashSet<string> existingConnections)
    {
        string connectionKey = GetConnectionKey(source, target);
        if (existingConnections.Contains(connectionKey))
            return false;
        
        if (balanceIncomingConnections)
        {
            int incomingCount = CountIncomingConnections(target, source.layer);
            if (incomingCount >= 3)
                return false;
        }
        
        if (preventPathCrossing && existingConnections.Count > 0)
        {
            if (WouldCrossExistingPaths(source, target))
                return false;
        }
        
        return true;
    }
    
    private bool WouldCrossExistingPaths(MapNode newSource, MapNode newTarget)
    {
        Vector2 newStart = newSource.GetPosition();
        Vector2 newEnd = newTarget.GetPosition();
        
        int targetLayer = newTarget.layer;
        List<MapNode> previousLayer = _layers[targetLayer - 1];
        
        foreach (MapNode existingSource in previousLayer)
        {
            foreach (MapNode existingTarget in existingSource.connectedNodes)
            {
                if (existingSource == newSource && existingTarget == newTarget)
                    continue;
                
                Vector2 existingStart = existingSource.GetPosition();
                Vector2 existingEnd = existingTarget.GetPosition();
                
                if (LineSegmentsIntersect(newStart, newEnd, existingStart, existingEnd))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
        
        if (Mathf.Abs(denominator) < 0.0001f)
            return false;
        
        float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
        float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;
        
        return (ua > 0.01f && ua < 0.99f && ub > 0.01f && ub < 0.99f);
    }
    
    private void AddSmartConvergence(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer,
        List<NodeColumn> currentColumns,
        List<NodeColumn> nextColumns,
        HashSet<string> existingConnections)
    {
        foreach (MapNode targetNode in nextLayer)
        {
            int incomingCount = CountIncomingConnections(targetNode, currentLayer.First().layer);
            
            if (incomingCount == 1 && _random.NextDouble() < convergeProbability)
            {
                int targetColumn = GetNodeColumn(targetNode, nextColumns);
                
                List<MapNode> potentialSources = new List<MapNode>();
                
                for (int colOffset = 0; colOffset <= maxColumnDistance; colOffset++)
                {
                    AddNodesFromColumn(potentialSources, targetColumn + colOffset, currentColumns);
                    if (colOffset > 0)
                        AddNodesFromColumn(potentialSources, targetColumn - colOffset, currentColumns);
                }
                
                potentialSources = potentialSources
                    .Where(node => !node.connectedNodes.Contains(targetNode))
                    .Where(node => IsConnectionViable(node, targetNode, existingConnections))
                    .OrderBy(node => Vector3.Distance(node.GetPosition(), targetNode.GetPosition()))
                    .ToList();
                
                if (potentialSources.Count > 0)
                {
                    CreateConnection(potentialSources[0], targetNode, existingConnections);
                }
            }
        }
    }
    
    private void AddNodesFromColumn(List<MapNode> list, int columnIndex, List<NodeColumn> columns)
    {
        if (columnIndex >= 0 && columnIndex < columns.Count)
        {
            list.AddRange(columns[columnIndex].nodesInColumn);
        }
    }
    
    private void EnsureAllNodesConnected(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer,
        List<NodeColumn> currentColumns,
        List<NodeColumn> nextColumns,
        HashSet<string> existingConnections)
    {
        foreach (MapNode targetNode in nextLayer)
        {
            int incomingCount = CountIncomingConnections(targetNode, currentLayer.First().layer);
            
            if (incomingCount == 0)
            {
                int targetColumn = GetNodeColumn(targetNode, nextColumns);
                
                MapNode closestSource = currentLayer
                    .OrderBy(node => Mathf.Abs(GetNodeColumn(node, currentColumns) - targetColumn))
                    .ThenBy(node => Vector3.Distance(node.GetPosition(), targetNode.GetPosition()))
                    .First();
                
                CreateConnection(closestSource, targetNode, existingConnections);
                Debug.LogWarning($"Forced connection for unreachable node at layer {targetNode.layer}");
            }
        }
    }
    
    private void CreateConnection(MapNode source, MapNode target, HashSet<string> connections)
    {
        source.AddConnection(target);
        connections.Add(GetConnectionKey(source, target));
    }
    
    private string GetConnectionKey(MapNode source, MapNode target)
    {
        return $"{source.layer}_{source.positionInLayer}_to_{target.layer}_{target.positionInLayer}";
    }
    
    private int CountIncomingConnections(MapNode targetNode, int sourceLayerIndex)
    {
        if (sourceLayerIndex < 0 || sourceLayerIndex >= _layers.Count)
            return 0;
        
        int count = 0;
        foreach (var sourceNode in _layers[sourceLayerIndex])
        {
            if (sourceNode.connectedNodes.Contains(targetNode))
                count++;
        }
        return count;
    }
    
    private void AssignNodeTypes()
    {
        for (int layerIndex = 0; layerIndex < _layers.Count; layerIndex++)
        {
            bool isPreBossLayer = (layerIndex == numberOfLayers - 2);
            
            foreach (MapNode node in _layers[layerIndex])
            {
                if (layerIndex == 0)
                {
                    node.nodeType = NodeType.Start;
                }
                else if (layerIndex == _layers.Count - 1)
                {
                    node.nodeType = NodeType.Boss;
                }
                else if (isPreBossLayer)
                {
                    node.nodeType = _random.NextDouble() < 0.5 ? NodeType.Treasure : NodeType.RestSite;
                }
                else
                {
                    node.nodeType = nodeDistribution.GetRandomNodeType(layerIndex, numberOfLayers, _random);
                }
                
                node.UpdateIcon();
            }
        }
    }
    
    private void InitializeMapState()
    {
        foreach (var layer in _layers)
        {
            foreach (var node in layer)
            {
                node.SetState(NodeState.Locked);
            }
        }
        
        if (_layers.Count > 0 && _layers[0].Count > 0)
        {
            MapNode startNode = _layers[0][0];
            startNode.SetState(NodeState.Current);
            
            foreach (MapNode connectedNode in startNode.connectedNodes)
            {
                connectedNode.SetState(NodeState.Available);
            }
        }
    }
    
    public int GetTotalNodeCount()
    {
        return _layers.Sum(layer => layer.Count);
    }
    
    public List<List<MapNode>> GetLayers()
    {
        return _layers;
    }
    
    public List<MapNode> GetAllNodes()
    {
        List<MapNode> allNodes = new List<MapNode>();
        foreach (var layer in _layers)
        {
            allNodes.AddRange(layer);
        }
        return allNodes;
    }
}

[System.Serializable]
public class NodeDistribution
{
    [Header("Node Type Probabilities (0-100)")]
    [Range(0, 100)] public float combatProbability = 50f;
    [Range(0, 100)] public float eliteCombatProbability = 10f;
    [Range(0, 100)] public float treasureProbability = 10f;
    [Range(0, 100)] public float shopProbability = 5f;
    [Range(0, 100)] public float restSiteProbability = 0f;
    [Range(0, 100)] public float randomEventProbability = 20f;
    [Range(0, 100)] public float mysteryProbability = 5f;
    
    public NodeType GetRandomNodeType(int currentLayer, int totalLayers, System.Random random)
    {
        float progress = (float)currentLayer / (totalLayers - 1);
        
        float adjustedEliteProbability = eliteCombatProbability * (1 + progress * 2f);
        float adjustedCombatProbability = combatProbability * (1.2f - progress * 0.4f);
        float adjustedEventProbability = randomEventProbability * (1.3f - progress * 0.5f);
        
        float totalProbability = adjustedCombatProbability + adjustedEliteProbability + 
                                 treasureProbability + shopProbability + 
                                 adjustedEventProbability + mysteryProbability;
        
        float randomValue = (float)random.NextDouble() * totalProbability;
        float cumulative = 0f;
        
        cumulative += adjustedCombatProbability;
        if (randomValue < cumulative) return NodeType.Combat;
        
        cumulative += adjustedEliteProbability;
        if (randomValue < cumulative) return NodeType.EliteCombat;
        
        cumulative += treasureProbability;
        if (randomValue < cumulative) return NodeType.Treasure;
        
        cumulative += shopProbability;
        if (randomValue < cumulative) return NodeType.Shop;
        
        cumulative += adjustedEventProbability;
        if (randomValue < cumulative) return NodeType.RandomEvent;
        
        return NodeType.Mystery;
    }
}