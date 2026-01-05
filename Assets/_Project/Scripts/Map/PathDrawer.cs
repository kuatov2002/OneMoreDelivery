using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отрисовщик путей между узлами в стиле Slay the Spire.
/// УЛУЧШЕНО: Более тонкие линии, цветовая кодировка по состоянию узлов,
/// улучшенная кривизна для естественного вида
/// </summary>
public class PathDrawer : MonoBehaviour
{
    [Header("Line Settings - StS Style")]
    [Tooltip("Материал для линий между узлами")]
    public Material lineMaterial;
    
    [Tooltip("Ширина линий (тоньше для StS стиля)")]
    public float lineWidth = 0.1f;
    
    [Tooltip("Цвет заблокированных линий (темно-серый)")]
    public Color lockedLineColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);
    
    [Tooltip("Цвет обычных линий")]
    public Color normalLineColor = new Color(0.25f, 0.25f, 0.35f, 0.7f);
    
    [Tooltip("Цвет активных путей (доступных для прохождения)")]
    public Color activeLineColor = new Color(0.6f, 0.9f, 1f, 1f);
    
    [Tooltip("Цвет пройденных путей")]
    public Color completedLineColor = new Color(0.3f, 0.8f, 0.4f, 1f);
    
    [Header("Curve Settings")]
    [Tooltip("Количество точек для сглаживания кривой")]
    [Range(2, 20)]
    public int curveResolution = 12;
    
    [Tooltip("Высота дуги линии (меньше для более прямых линий)")]
    [Range(0f, 1f)]
    public float curveHeight = 0.15f;
    
    [Tooltip("Используть динамическую кривизну (больше для длинных путей)")]
    public bool useDynamicCurve = true;
    
    [Tooltip("Родительский объект для всех линий")]
    public Transform linesParent;
    
    [Header("Quality Settings")]
    [Tooltip("Сглаживание углов линий")]
    [Range(0, 10)]
    public int cornerVertices = 3;
    
    [Tooltip("Сглаживание концов линий")]
    [Range(0, 10)]
    public int capVertices = 3;
    
    private Dictionary<string, LineRenderer> lineRenderers = new Dictionary<string, LineRenderer>();
    
    public void DrawAllPaths(List<MapNode> allNodes)
    {
        ClearAllLines();
        
        foreach (MapNode node in allNodes)
        {
            foreach (MapNode connectedNode in node.connectedNodes)
            {
                DrawPath(node, connectedNode);
            }
        }
    }
    
    public void DrawPath(MapNode startNode, MapNode endNode)
    {
        string lineKey = GetLineKey(startNode, endNode);
        
        if (lineRenderers.ContainsKey(lineKey))
        {
            UpdateLine(lineRenderers[lineKey], startNode, endNode);
            return;
        }
        
        GameObject lineObj = new GameObject($"Line_{lineKey}");
        lineObj.transform.SetParent(linesParent);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer);
        UpdateLine(lineRenderer, startNode, endNode);
        
        lineRenderers[lineKey] = lineRenderer;
    }
    
    /// <summary>
    /// УЛУЧШЕНО: Настройки для более тонких и элегантных линий
    /// </summary>
    private void ConfigureLineRenderer(LineRenderer lineRenderer)
    {
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = normalLineColor;
        lineRenderer.endColor = normalLineColor;
        lineRenderer.sortingOrder = -1; // За узлами
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = curveResolution;
        
        lineRenderer.numCornerVertices = cornerVertices;
        lineRenderer.numCapVertices = capVertices;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }
    
    private void UpdateLine(LineRenderer lineRenderer, MapNode startNode, MapNode endNode)
    {
        Vector3 startPos = startNode.GetPosition();
        Vector3 endPos = endNode.GetPosition();
        
        Vector3[] curvePoints = GenerateImprovedCurvePoints(startPos, endPos);
        
        lineRenderer.positionCount = curvePoints.Length;
        lineRenderer.SetPositions(curvePoints);
        
        UpdateLineColor(lineRenderer, startNode, endNode);
    }
    
    /// <summary>
    /// НОВОЕ: Улучшенная генерация кривых с динамической кривизной
    /// Создает более естественные пути, как в Slay the Spire
    /// </summary>
    private Vector3[] GenerateImprovedCurvePoints(Vector3 start, Vector3 end)
    {
        Vector3[] points = new Vector3[curveResolution];
        
        Vector3 midPoint = (start + end) / 2f;
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        
        // Динамическая кривизна: больше для длинных путей
        float effectiveCurveHeight = curveHeight;
        if (useDynamicCurve)
        {
            effectiveCurveHeight *= Mathf.Clamp(distance / 4f, 0.5f, 1.5f);
        }
        
        // Перпендикуляр для создания дуги
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0).normalized;
        
        // Направление изгиба зависит от горизонтального расположения
        float bendDirection = Mathf.Sign(end.x - start.x);
        if (Mathf.Abs(end.x - start.x) < 0.1f) bendDirection = 0f; // Почти вертикально
        
        Vector3 controlPoint = midPoint + perpendicular * effectiveCurveHeight * bendDirection;
        
        // Генерируем плавную кривую Безье
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            points[i] = CalculateQuadraticBezierPoint(t, start, controlPoint, end);
        }
        
        return points;
    }
    
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;
        
        return point;
    }
    
    /// <summary>
    /// УЛУЧШЕНО: Более четкая логика окрашивания линий в зависимости от состояния узлов
    /// Это ключевая визуальная особенность Slay the Spire
    /// </summary>
    private void UpdateLineColor(LineRenderer lineRenderer, MapNode startNode, MapNode endNode)
    {
        NodeState startState = startNode.GetState();
        NodeState endState = endNode.GetState();
        
        Color lineColor;
        
        // Определяем цвет на основе состояний обоих узлов
        if (startState == NodeState.Completed && endState == NodeState.Completed)
        {
            // Оба узла пройдены - зеленый
            lineColor = completedLineColor;
        }
        else if (startState == NodeState.Current && endState == NodeState.Available)
        {
            // От текущего к доступному - яркий цвет
            lineColor = activeLineColor;
        }
        else if (startState == NodeState.Completed && endState == NodeState.Available)
        {
            // От пройденного к доступному - яркий цвет
            lineColor = activeLineColor;
        }
        else if (endState == NodeState.Available)
        {
            // Конечный узел доступен - показываем как активный путь
            lineColor = activeLineColor;
        }
        else if (startState == NodeState.Completed || endState == NodeState.Completed)
        {
            // Хотя бы один узел пройден - показываем как пройденный путь
            lineColor = completedLineColor;
        }
        else if (startState == NodeState.Locked && endState == NodeState.Locked)
        {
            // Оба заблокированы - темный цвет
            lineColor = lockedLineColor;
        }
        else
        {
            // Обычная линия
            lineColor = normalLineColor;
        }
        
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }
    
    public void UpdateAllLineColors(List<MapNode> allNodes)
    {
        foreach (MapNode node in allNodes)
        {
            foreach (MapNode connectedNode in node.connectedNodes)
            {
                string lineKey = GetLineKey(node, connectedNode);
                
                if (lineRenderers.ContainsKey(lineKey))
                {
                    UpdateLineColor(lineRenderers[lineKey], node, connectedNode);
                }
            }
        }
    }
    
    private string GetLineKey(MapNode startNode, MapNode endNode)
    {
        return $"{startNode.layer}_{startNode.positionInLayer}_to_{endNode.layer}_{endNode.positionInLayer}";
    }
    
    private void ClearAllLines()
    {
        foreach (var kvp in lineRenderers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        
        lineRenderers.Clear();
    }
    
    public void SetLinesVisible(bool visible)
    {
        foreach (var kvp in lineRenderers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.enabled = visible;
            }
        }
    }
    
    /// <summary>
    /// УЛУЧШЕНО: Подсветка с более тонким эффектом
    /// </summary>
    public void HighlightPath(MapNode startNode, MapNode endNode, bool highlight)
    {
        string lineKey = GetLineKey(startNode, endNode);
        
        if (lineRenderers.ContainsKey(lineKey))
        {
            LineRenderer line = lineRenderers[lineKey];
            
            if (highlight)
            {
                // Делаем линию чуть толще и ярче
                line.startWidth = lineWidth * 1.3f;
                line.endWidth = lineWidth * 1.3f;
                
                Color brightColor = activeLineColor * 1.2f;
                brightColor.a = 1f;
                line.startColor = brightColor;
                line.endColor = brightColor;
            }
            else
            {
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                UpdateLineColor(line, startNode, endNode);
            }
        }
    }
    
    public void AnimateLine(MapNode startNode, MapNode endNode)
    {
        string lineKey = GetLineKey(startNode, endNode);
        
        if (lineRenderers.ContainsKey(lineKey))
        {
            StartCoroutine(PulsateLine(lineRenderers[lineKey]));
        }
    }
    
    /// <summary>
    /// Анимация пульсации при выборе пути
    /// </summary>
    private System.Collections.IEnumerator PulsateLine(LineRenderer line)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        
        Color originalColor = line.startColor;
        float originalWidth = line.startWidth;
        
        Color brightColor = Color.white;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Sin(t * Mathf.PI * 2f); // Двойная пульсация
            
            Color currentColor = Color.Lerp(originalColor, brightColor, intensity * 0.5f);
            float currentWidth = Mathf.Lerp(originalWidth, originalWidth * 1.8f, intensity * 0.5f);
            
            line.startColor = currentColor;
            line.endColor = currentColor;
            line.startWidth = currentWidth;
            line.endWidth = currentWidth;
            
            yield return null;
        }
        
        // Возвращаем исходные параметры
        line.startColor = originalColor;
        line.endColor = originalColor;
        line.startWidth = originalWidth;
        line.endWidth = originalWidth;
    }
}