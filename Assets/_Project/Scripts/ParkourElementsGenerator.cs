using System.Collections.Generic;
using System.Linq;
using BlockGeneration;
using GraphModel;
using Services;
using UnityEngine;

namespace ParkourGeneration
{
    /// <summary>
    /// УЛУЧШЕННЫЙ генератор паркур-элементов с проверкой коллизий и умным размещением
    /// Версия 2.0 - решает проблемы пересечения элементов и избыточного размещения
    /// </summary>
    public class ParkourElementsGenerator
    {
        // Основные данные
        private readonly List<Block> lots;
        private readonly Graph roadGraph;
        private readonly System.Random rand;
        private readonly float mapScale;
        
        private readonly GameObject bridgePrefab;
        private readonly GameObject ziplinePrefab;
        private readonly GameObject wallRunSurfacePrefab;
        
        // === НОВОЕ: Структуры данных для отслеживания размещенных элементов ===
        
        /// <summary>
        /// Отслеживает, какие элементы уже размещены на каждом здании
        /// Это предотвращает переполнение одного здания элементами
        /// </summary>
        private Dictionary<Block, BuildingParkourElements> buildingElements;
        
        /// <summary>
        /// Хранит все размещенные линейные элементы (мосты, zipline'ы) для проверки пересечений
        /// Это позволяет убедиться, что новый элемент не пересекает существующий
        /// </summary>
        private List<LinearParkourElement> placedLinearElements;
        
        // === НАСТРОЙКИ ГЕНЕРАЦИИ ===
        
        // Ограничения на количество элементов на одно здание
        private readonly int maxBridgesPerBuilding = 2;     // Максимум 3 моста от здания
        private readonly int maxZiplinesPerBuilding = 2;    // Максимум 2 zipline'а от здания
        
        // Дистанции и проверки
        private readonly float minBuildingDistance = 3f;
        private readonly float maxBridgeDistance = 15f;
        private readonly float maxZiplineDistance = 30f;
        private readonly float minZiplineAngle = 10f;       // Минимальный угол наклона zipline в градусах
        private readonly float raycastCheckRadius = 1.5f;   // Радиус для проверки пересечений
        
        // Шансы появления (можно настраивать)
        private readonly float bridgeSpawnChance = 0.5f;
        private readonly float ziplineSpawnChance = 0.3f;
        
        public ParkourElementsGenerator(
            List<Block> buildings,
            Graph roads,
            System.Random random,
            float scale,
            GameObject bridge = null,
            GameObject zipline = null,
            GameObject wallRun = null)
        {
            lots = buildings;
            roadGraph = roads;
            rand = random;
            mapScale = scale;
            
            bridgePrefab = bridge;
            ziplinePrefab = zipline;
            wallRunSurfacePrefab = wallRun;
            
            // Инициализируем систему отслеживания элементов
            buildingElements = new Dictionary<Block, BuildingParkourElements>();
            placedLinearElements = new List<LinearParkourElement>();
            
            // Создаем записи для каждого здания
            foreach (var building in lots)
            {
                buildingElements[building] = new BuildingParkourElements();
            }
        }
        
        /// <summary>
        /// Генерирует все паркур-элементы с умной проверкой коллизий
        /// Элементы размещаются в определенном порядке по важности
        /// </summary>
        public void GenerateAllParkourElements(GameObject parentContainer)
        {
            Debug.Log("=== Starting IMPROVED parkour generation with collision detection ===");
            
            // ВАЖНО: Порядок генерации имеет значение!
            // Сначала размещаем самые важные элементы (лестницы),
            // затем менее критичные (мосты, zipline'ы)
            
            // 1. Мосты - соединяют близкие здания
            var bridgesContainer = new GameObject("Bridges Container");
            bridgesContainer.transform.SetParent(parentContainer.transform);
            int bridgesCount = GenerateBridgesImproved(bridgesContainer);
            Debug.Log($"Generated {bridgesCount} bridges with raycast validation");
            
            // 2. Zipline'ы - быстрое перемещение на дальние расстояния
            var ziplinesContainer = new GameObject("Ziplines Container");
            ziplinesContainer.transform.SetParent(parentContainer.transform);
            int ziplinesCount = GenerateZiplinesImproved(ziplinesContainer);
            Debug.Log($"Generated {ziplinesCount} ziplines with obstacle avoidance");
            
            Debug.Log("=== Parkour generation complete ===");
        }
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация мостов с raycast проверкой
        /// Мосты теперь проверяют, не проходят ли они через другие здания
        /// </summary>
        private int GenerateBridgesImproved(GameObject container)
        {
            if (bridgePrefab == null) return 0;
            
            int bridgeCount = 0;
            HashSet<(Block, Block)> connectedPairs = new HashSet<(Block, Block)>();
            
            foreach (var building1 in lots)
            {
                if (building1.IsPark) continue;
                
                // Проверяем лимит мостов от этого здания
                if (buildingElements[building1].BridgesCount >= maxBridgesPerBuilding) continue;
                
                // Находим кандидатов для моста
                var nearbyBuildings = FindNearbyBuildingsWithSimilarHeight(
                    building1, 
                    maxBridgeDistance, 
                    heightTolerance: 2f);
                
                foreach (var building2 in nearbyBuildings)
                {
                    // Проверяем лимит мостов для второго здания
                    if (buildingElements[building2].BridgesCount >= maxBridgesPerBuilding) continue;
                    
                    // Проверяем шанс появления
                    if (rand.NextDouble() > bridgeSpawnChance) continue;
                    
                    // Проверяем, не создали ли уже мост между этими зданиями
                    var pair1 = (building1, building2);
                    var pair2 = (building2, building1);
                    if (connectedPairs.Contains(pair1) || connectedPairs.Contains(pair2))
                        continue;
                    
                    // Находим ближайшие точки на крышах
                    var point1 = FindClosestRoofPoint(building1, building2);
                    var point2 = FindClosestRoofPoint(building2, building1);
                    
                    float distance = Vector3.Distance(point1, point2);
                    
                    // Проверяем дистанцию
                    if (distance < minBuildingDistance || distance > maxBridgeDistance)
                        continue;
                    
                    // === НОВАЯ КРИТИЧЕСКИ ВАЖНАЯ ПРОВЕРКА: Raycast через здания ===
                    // Это проверяет, не проходит ли мост через другое здание
                    if (PathIntersectsBuildings(point1, point2, building1, building2))
                    {
                        continue; // Мост проходит через здание, отклоняем
                    }
                    
                    // === НОВАЯ ПРОВЕРКА: Конфликт с другими линейными элементами ===
                    if (ConflictsWithLinearElements(point1, point2))
                    {
                        continue;
                    }
                    
                    // Все проверки пройдены, создаем мост
                    var bridge = Object.Instantiate(bridgePrefab, container.transform);
                    var bridgeCenter = (point1 + point2) / 2f;
                    bridge.transform.position = bridgeCenter * mapScale;
                    bridge.transform.rotation = Quaternion.LookRotation((point2 - point1).normalized);
                    bridge.transform.localScale = new Vector3(
                        mapScale * 0.5f,
                        mapScale * 0.1f,
                        distance * mapScale
                    );
                    
                    bridge.name = $"Bridge_{bridgeCount}";
                    
                    // Регистрируем размещение
                    buildingElements[building1].BridgesCount++;
                    buildingElements[building2].BridgesCount++;
                    connectedPairs.Add(pair1);
                    
                    // Добавляем в список линейных элементов
                    placedLinearElements.Add(new LinearParkourElement
                    {
                        StartPoint = point1,
                        EndPoint = point2,
                        Type = ParkourElementType.Bridge,
                        Building1 = building1,
                        Building2 = building2
                    });
                    
                    bridgeCount++;
                }
            }
            
            return bridgeCount;
        }
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация zipline'ов с проверкой угла наклона и препятствий
        /// Zipline'ы теперь избегают зданий и имеют реалистичный угол наклона
        /// </summary>
        private int GenerateZiplinesImproved(GameObject container)
        {
            if (ziplinePrefab == null) return 0;
            
            int ziplineCount = 0;
            
            // Находим высокие здания для zipline'ов
            var tallBuildings = lots.Where(b => !b.IsPark && b.Height > 8f).ToList();
            
            foreach (var startBuilding in tallBuildings)
            {
                // Проверяем лимит zipline'ов от этого здания
                if (buildingElements[startBuilding].ZiplinesCount >= maxZiplinesPerBuilding) continue;
                
                // Проверяем шанс появления
                if (rand.NextDouble() > ziplineSpawnChance) continue;
                
                // Ищем подходящие целевые здания (ниже текущего)
                var targetBuildings = FindBuildingsInRadiusLowerThan(
                    startBuilding, 
                    maxZiplineDistance,
                    minHeightDifference: 3f);
                
                if (targetBuildings.Count == 0) continue;
                
                // Пробуем несколько кандидатов, пока не найдем подходящий
                var shuffledTargets = targetBuildings.OrderBy(x => rand.Next()).ToList();
                bool ziplineCreated = false;
                
                foreach (var endBuilding in shuffledTargets)
                {
                    if (ziplineCreated) break;
                    
                    // Проверяем лимит для целевого здания
                    if (buildingElements[endBuilding].ZiplinesCount >= maxZiplinesPerBuilding) continue;
                    
                    // Позиции начала и конца
                    var startPos = BuildingHelper.GetRandomRoofEdgePoint(startBuilding, rand);
                    var endPos = BuildingHelper.GetRandomRoofEdgePoint(endBuilding, rand);
                    
                    float distance = Vector3.Distance(startPos, endPos);
                    
                    // Проверяем дистанцию
                    if (distance < 5f || distance > maxZiplineDistance) continue;
                    
                    // === НОВАЯ ПРОВЕРКА 1: Угол наклона zipline ===
                    // Zipline должен иметь разумный угол наклона для реалистичности
                    float heightDiff = startPos.y - endPos.y;
                    float horizontalDist = Vector2.Distance(
                        new Vector2(startPos.x, startPos.z),
                        new Vector2(endPos.x, endPos.z)
                    );
                    float angle = Mathf.Atan2(heightDiff, horizontalDist) * Mathf.Rad2Deg;
                    
                    if (angle < minZiplineAngle)
                    {
                        continue; // Слишком пологий наклон, не будет работать
                    }
                    
                    // === НОВАЯ ПРОВЕРКА 2: Путь свободен от зданий ===
                    if (PathIntersectsBuildings(startPos, endPos, startBuilding, endBuilding))
                    {
                        continue; // Zipline проходит через здание
                    }
                    
                    // === НОВАЯ ПРОВЕРКА 3: Конфликт с другими элементами ===
                    if (ConflictsWithLinearElements(startPos, endPos))
                    {
                        continue;
                    }
                    
                    // Все проверки пройдены, создаем zipline
                    var zipline = Object.Instantiate(ziplinePrefab, container.transform);
                    var ziplineCenter = (startPos + endPos) / 2f;
                    zipline.transform.position = ziplineCenter * mapScale;
                    zipline.transform.rotation = Quaternion.LookRotation((endPos - startPos).normalized);
                    zipline.transform.localScale = new Vector3(
                        mapScale * 0.1f,
                        mapScale * 0.1f,
                        distance * mapScale
                    );
                    
                    zipline.name = $"Zipline_{ziplineCount}";
                    
                    // Регистрируем размещение
                    buildingElements[startBuilding].ZiplinesCount++;
                    buildingElements[endBuilding].ZiplinesCount++;
                    
                    // Добавляем в список линейных элементов
                    placedLinearElements.Add(new LinearParkourElement
                    {
                        StartPoint = startPos,
                        EndPoint = endPos,
                        Type = ParkourElementType.Zipline,
                        Building1 = startBuilding,
                        Building2 = endBuilding
                    });
                    
                    ziplineCount++;
                    ziplineCreated = true;
                }
            }
            
            return ziplineCount;
        }
        
        // ===================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ПРОВЕРКИ КОЛЛИЗИЙ
        // ===================================================================
        
        /// <summary>
        /// КРИТИЧЕСКИ ВАЖНЫЙ МЕТОД: Проверяет, проходит ли путь через здания
        /// Использует множественные точки проверки для надежности
        /// </summary>
        private bool PathIntersectsBuildings(Vector3 start, Vector3 end, Block exclude1, Block exclude2)
        {
            // Проверяем путь в нескольких точках для надежности
            int checkPoints = Mathf.Max(5, Mathf.CeilToInt(Vector3.Distance(start, end) / 2f));
            
            for (int i = 0; i <= checkPoints; i++)
            {
                float t = i / (float)checkPoints;
                Vector3 checkPoint = Vector3.Lerp(start, end, t);
                Vector2 checkPoint2D = new Vector2(checkPoint.x, checkPoint.z);
                
                // Проверяем каждое здание
                foreach (var building in lots)
                {
                    // Пропускаем здания, которые являются частью этого элемента
                    if (building == exclude1 || building == exclude2) continue;
                    
                    // Проверяем, находится ли точка внутри здания
                    // и находится ли она на правильной высоте (не под или над зданием)
                    if (BuildingHelper.IsPointInside(building, checkPoint2D))
                    {
                        // Точка внутри 2D проекции здания, проверяем высоту
                        if (checkPoint.y <= building.Height && checkPoint.y >= 0)
                        {
                            return true; // Путь пересекает это здание
                        }
                    }
                }
            }
            
            return false; // Путь чист
        }
        
        /// <summary>
        /// Проверяет, находится ли точка внутри какого-либо здания (кроме исключаемого)
        /// </summary>
        private bool IsPointInsideAnyBuilding(Vector3 point, Block exclude)
        {
            Vector2 point2D = new Vector2(point.x, point.z);
            
            foreach (var building in lots)
            {
                if (building == exclude) continue;
                
                if (BuildingHelper.IsPointInside(building, point2D))
                {
                    if (point.y <= building.Height && point.y >= 0)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, конфликтует ли новый линейный элемент с уже размещенными
        /// Это предотвращает пересечение мостов и zipline'ов друг с другом
        /// </summary>
        private bool ConflictsWithLinearElements(Vector3 start, Vector3 end)
        {
            foreach (var element in placedLinearElements)
            {
                // Проверяем, пересекаются ли линии
                if (LineSegmentsIntersect3D(start, end, element.StartPoint, element.EndPoint, raycastCheckRadius))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, пересекаются ли два линейных сегмента в 3D пространстве
        /// Использует упрощенную проверку с радиусом допуска
        /// </summary>
        private bool LineSegmentsIntersect3D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float radius)
        {
            // Находим ближайшие точки на двух линиях
            Vector3 closestPoint = ClosestPointOnLineSegment(p1, p2, p3, p4);
            
            // Если ближайшее расстояние меньше радиуса, линии пересекаются
            return closestPoint.magnitude < radius;
        }
        
        /// <summary>
        /// Находит ближайшее расстояние между двумя линейными сегментами
        /// </summary>
        private Vector3 ClosestPointOnLineSegment(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector3 d1 = p2 - p1;
            Vector3 d2 = p4 - p3;
            Vector3 r = p1 - p3;
            
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            
            float s, t;
            
            if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
            {
                return p1 - p3;
            }
            
            if (a <= Mathf.Epsilon)
            {
                s = 0.0f;
                t = f / e;
                t = Mathf.Clamp01(t);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= Mathf.Epsilon)
                {
                    t = 0.0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    
                    if (denom != 0.0f)
                    {
                        s = Mathf.Clamp01((b * f - c * e) / denom);
                    }
                    else
                    {
                        s = 0.0f;
                    }
                    
                    t = (b * s + f) / e;
                    
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }
            
            Vector3 c1 = p1 + d1 * s;
            Vector3 c2 = p3 + d2 * t;
            
            return c1 - c2;
        }
        
        // ===================================================================
        // СУЩЕСТВУЮЩИЕ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (БЕЗ ИЗМЕНЕНИЙ)
        // ===================================================================
        
        private int FindEdgeNearestToRoad(Block building)
        {
            float minDistance = float.MaxValue;
            int bestEdge = 0;
            
            for (int i = 0; i < building.Nodes.Count; i++)
            {
                var edgeCenter = BuildingHelper.GetEdgeCenter(building, i, 0f);
                var centerPoint = new Vector2(edgeCenter.x, edgeCenter.z);
                
                float distToRoad = BuildingHelper.GetDistanceToNearestRoadNode(centerPoint, roadGraph);
                if (distToRoad < minDistance)
                {
                    minDistance = distToRoad;
                    bestEdge = i;
                }
            }
            
            return bestEdge;
        }
        
        private List<Block> FindNearbyBuildingsWithSimilarHeight(
            Block building, 
            float maxDistance, 
            float heightTolerance)
        {
            var result = new List<Block>();
            var buildingCenter = new Vector2(
                building.Nodes.Average(n => n.X),
                building.Nodes.Average(n => n.Y)
            );
            
            foreach (var other in lots)
            {
                if (other == building || other.IsPark) continue;
                
                var otherCenter = new Vector2(
                    other.Nodes.Average(n => n.X),
                    other.Nodes.Average(n => n.Y)
                );
                
                float distance = Vector2.Distance(buildingCenter, otherCenter);
                float heightDiff = Mathf.Abs(building.Height - other.Height);
                
                if (distance <= maxDistance && heightDiff <= heightTolerance)
                {
                    result.Add(other);
                }
            }
            
            return result;
        }
        
        private List<Block> FindBuildingsInRadiusLowerThan(
            Block building,
            float maxDistance,
            float minHeightDifference)
        {
            var result = new List<Block>();
            var buildingCenter = new Vector2(
                building.Nodes.Average(n => n.X),
                building.Nodes.Average(n => n.Y)
            );
            
            foreach (var other in lots)
            {
                if (other == building || other.IsPark) continue;
                
                var otherCenter = new Vector2(
                    other.Nodes.Average(n => n.X),
                    other.Nodes.Average(n => n.Y)
                );
                
                float distance = Vector2.Distance(buildingCenter, otherCenter);
                float heightDiff = building.Height - other.Height;
                
                if (distance <= maxDistance && heightDiff >= minHeightDifference)
                {
                    result.Add(other);
                }
            }
            
            return result;
        }
        
        private Vector3 FindClosestRoofPoint(Block fromBuilding, Block toBuilding)
        {
            var toCenter = new Vector2(
                toBuilding.Nodes.Average(n => n.X),
                toBuilding.Nodes.Average(n => n.Y)
            );
            
            float minDist = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            
            foreach (var node in fromBuilding.Nodes)
            {
                var nodePos = new Vector2(node.X, node.Y);
                float dist = Vector2.Distance(nodePos, toCenter);
                
                if (dist < minDist)
                {
                    minDist = dist;
                    closestPoint = new Vector3(node.X, fromBuilding.Height, node.Y);
                }
            }
            
            return closestPoint;
        }
    }
    
    // ===================================================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ ОТСЛЕЖИВАНИЯ ЭЛЕМЕНТОВ
    // ===================================================================
    
    /// <summary>
    /// Отслеживает количество паркур-элементов на каждом здании
    /// Это позволяет нам ограничить количество элементов и избежать переполнения
    /// </summary>
    public class BuildingParkourElements
    {
        public int StairsCount = 0;
        public int BridgesCount = 0;
        public int ZiplinesCount = 0;
        public int PolesCount = 0;
    }
    
    /// <summary>
    /// Представляет линейный паркур-элемент (мост или zipline)
    /// Используется для проверки пересечений между элементами
    /// </summary>
    public class LinearParkourElement
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;
        public ParkourElementType Type;
        public Block Building1;
        public Block Building2;
    }
    
    /// <summary>
    /// Типы линейных паркур-элементов
    /// </summary>
    public enum ParkourElementType
    {
        Bridge,
        Zipline
    }
}