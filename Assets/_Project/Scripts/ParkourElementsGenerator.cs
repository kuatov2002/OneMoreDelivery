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
        
        // Префабы
        private readonly GameObject stairsPrefab;
        private readonly GameObject bridgePrefab;
        private readonly GameObject ziplinePrefab;
        private readonly GameObject railPrefab;
        private readonly GameObject climbingPolePrefab;
        private readonly GameObject platformPrefab;
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
        private readonly int maxStairsPerBuilding = 2;      // Максимум 2 лестницы на здание
        private readonly int maxBridgesPerBuilding = 3;     // Максимум 3 моста от здания
        private readonly int maxZiplinesPerBuilding = 2;    // Максимум 2 zipline'а от здания
        private readonly int maxRailsPerBuilding = 3;       // Максимум 3 стороны с перилами
        private readonly int maxPolesPerBuilding = 1;       // Максимум 1 столб на здание
        
        // Дистанции и проверки
        private readonly float minBuildingDistance = 3f;
        private readonly float maxBridgeDistance = 15f;
        private readonly float maxZiplineDistance = 30f;
        private readonly float minZiplineAngle = 15f;       // Минимальный угол наклона zipline в градусах
        private readonly float raycastCheckRadius = 0.5f;   // Радиус для проверки пересечений
        
        // Шансы появления (можно настраивать)
        private readonly float stairsSpawnChance = 0.5f;
        private readonly float bridgeSpawnChance = 0.25f;
        private readonly float ziplineSpawnChance = 0.2f;
        private readonly float railSpawnChance = 0.4f;
        private readonly float climbingPoleChance = 0.3f;
        private readonly float platformSpawnChance = 0.6f;
        
        public ParkourElementsGenerator(
            List<Block> buildings,
            Graph roads,
            System.Random random,
            float scale,
            GameObject stairs = null,
            GameObject bridge = null,
            GameObject zipline = null,
            GameObject rail = null,
            GameObject pole = null,
            GameObject platform = null,
            GameObject wallRun = null)
        {
            lots = buildings;
            roadGraph = roads;
            rand = random;
            mapScale = scale;
            
            stairsPrefab = stairs;
            bridgePrefab = bridge;
            ziplinePrefab = zipline;
            railPrefab = rail;
            climbingPolePrefab = pole;
            platformPrefab = platform;
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
            
            // 1. Лестницы - самый важный элемент, нужен для доступа на крыши
            var stairsContainer = new GameObject("Stairs Container");
            stairsContainer.transform.SetParent(parentContainer.transform);
            int stairsCount = GenerateExternalStairsImproved(stairsContainer);
            Debug.Log($"Generated {stairsCount} stairs with collision checking");
            
            // 2. Столбы - альтернативный доступ на крыши
            var polesContainer = new GameObject("Climbing Poles Container");
            polesContainer.transform.SetParent(parentContainer.transform);
            int polesCount = GenerateClimbingPolesImproved(polesContainer);
            Debug.Log($"Generated {polesCount} climbing poles");
            
            // 3. Промежуточные платформы - делают высокие здания доступнее
            var platformsContainer = new GameObject("Mid-Height Platforms Container");
            platformsContainer.transform.SetParent(parentContainer.transform);
            int platformsCount = GenerateIntermediatePlatformsImproved(platformsContainer);
            Debug.Log($"Generated {platformsCount} intermediate platforms");
            
            // 4. Мосты - соединяют близкие здания
            var bridgesContainer = new GameObject("Bridges Container");
            bridgesContainer.transform.SetParent(parentContainer.transform);
            int bridgesCount = GenerateBridgesImproved(bridgesContainer);
            Debug.Log($"Generated {bridgesCount} bridges with raycast validation");
            
            // 5. Zipline'ы - быстрое перемещение на дальние расстояния
            var ziplinesContainer = new GameObject("Ziplines Container");
            ziplinesContainer.transform.SetParent(parentContainer.transform);
            int ziplinesCount = GenerateZiplinesImproved(ziplinesContainer);
            Debug.Log($"Generated {ziplinesCount} ziplines with obstacle avoidance");
            
            // 6. Перила - декоративный элемент, последний по важности
            var railsContainer = new GameObject("Rails Container");
            railsContainer.transform.SetParent(parentContainer.transform);
            int railsCount = GenerateRailsImproved(railsContainer);
            Debug.Log($"Generated {railsCount} rails");
            
            Debug.Log("=== Parkour generation complete ===");
        }
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация лестниц с проверкой на конфликты
        /// Теперь лестницы не размещаются, если мешают другие здания или элементы
        /// </summary>
        private int GenerateExternalStairsImproved(GameObject container)
        {
            if (stairsPrefab == null) return 0;
            
            int stairsCount = 0;
            
            foreach (var building in lots)
            {
                // Пропускаем парки и низкие здания
                if (building.IsPark || building.Height < 2f) continue;
                
                // Проверяем лимит лестниц на здание
                if (buildingElements[building].StairsCount >= maxStairsPerBuilding) continue;
                
                // Проверяем шанс появления
                if (rand.NextDouble() > stairsSpawnChance) continue;
                
                // Находим лучшую сторону для лестницы (ближайшую к дороге)
                int bestEdge = FindEdgeNearestToRoad(building);
                
                // Получаем позицию и направление
                var edgeCenter = BuildingHelper.GetEdgeCenter(building, bestEdge, 0f);
                var edgeNormal = BuildingHelper.GetEdgeOutwardNormal(building, bestEdge);
                
                // Лестница будет выступать наружу на это расстояние
                float stairsDepth = 1.5f;
                var stairsEndPoint = edgeCenter + edgeNormal * stairsDepth;
                
                // === НОВАЯ ПРОВЕРКА 1: Не мешает ли другое здание ===
                if (IsPointInsideAnyBuilding(stairsEndPoint, building))
                {
                    continue; // Лестница влезает в другое здание, пропускаем
                }
                
                // === НОВАЯ ПРОВЕРКА 2: Не слишком ли близко к другим зданиям ===
                if (BuildingHelper.IsPointNearOtherBuildings(
                    stairsEndPoint, 
                    building, 
                    lots, 
                    minBuildingDistance))
                {
                    continue;
                }
                
                // === НОВАЯ ПРОВЕРКА 3: Не конфликтует ли с уже размещенными мостами ===
                if (ConflictsWithLinearElements(edgeCenter, stairsEndPoint))
                {
                    continue;
                }
                
                // Все проверки пройдены, создаем лестницу
                var stairs = Object.Instantiate(stairsPrefab, container.transform);
                stairs.transform.position = edgeCenter * mapScale;
                stairs.transform.rotation = Quaternion.LookRotation(edgeNormal);
                
                float heightScale = building.Height / 2f;
                stairs.transform.localScale = new Vector3(
                    mapScale * 0.8f,
                    heightScale,
                    mapScale * stairsDepth
                );
                
                stairs.name = $"ExternalStairs_{stairsCount}";
                
                // Регистрируем размещение
                buildingElements[building].StairsCount++;
                stairsCount++;
                
                // Для очень высоких зданий добавляем промежуточную платформу
                if (building.Height > 8f && platformPrefab != null)
                {
                    var midPlatform = Object.Instantiate(platformPrefab, container.transform);
                    var platformPos = edgeCenter + Vector3.up * building.Height * 0.5f;
                    midPlatform.transform.position = platformPos * mapScale;
                    midPlatform.transform.rotation = Quaternion.LookRotation(edgeNormal);
                    midPlatform.transform.localScale = new Vector3(mapScale, mapScale * 0.2f, mapScale);
                    midPlatform.name = $"StairsPlatform_{stairsCount}";
                }
            }
            
            return stairsCount;
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
                    heightTolerance: 3f);
                
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
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация перил с ограничением количества на здание
        /// </summary>
        private int GenerateRailsImproved(GameObject container)
        {
            if (railPrefab == null) return 0;
            
            int railCount = 0;
            
            foreach (var building in lots)
            {
                if (building.IsPark || building.Height < 3f) continue;
                
                // Проверяем шанс появления
                if (rand.NextDouble() > railSpawnChance) continue;
                
                // === НОВОЕ: Ограничиваем количество сторон с перилами ===
                int maxEdgesToRail = Mathf.Min(maxRailsPerBuilding, building.Nodes.Count);
                int edgesToRail = rand.Next(1, maxEdgesToRail + 1);
                
                var edgeIndices = Enumerable.Range(0, building.Nodes.Count)
                    .OrderBy(x => rand.Next())
                    .Take(edgesToRail)
                    .ToList();
                
                foreach (int edgeIndex in edgeIndices)
                {
                    var nodeA = building.Nodes[edgeIndex];
                    var nodeB = building.Nodes[(edgeIndex + 1) % building.Nodes.Count];
                    
                    float edgeLength = Vector2.Distance(
                        new Vector2(nodeA.X, nodeA.Y),
                        new Vector2(nodeB.X, nodeB.Y));
                    
                    // Пропускаем очень короткие грани
                    if (edgeLength < 1f) continue;
                    
                    var rail = Object.Instantiate(railPrefab, container.transform);
                    var railCenter = new Vector3(
                        (nodeA.X + nodeB.X) / 2f,
                        building.Height + 0.1f,
                        (nodeA.Y + nodeB.Y) / 2f
                    );
                    
                    var direction = new Vector3(nodeB.X - nodeA.X, 0, nodeB.Y - nodeA.Y);
                    rail.transform.position = railCenter * mapScale;
                    rail.transform.rotation = Quaternion.LookRotation(direction);
                    rail.transform.localScale = new Vector3(
                        mapScale * 0.1f,
                        mapScale * 0.3f,
                        edgeLength * mapScale
                    );
                    
                    rail.name = $"Rail_{railCount}";
                    railCount++;
                }
                
                buildingElements[building].RailsCount = edgesToRail;
            }
            
            return railCount;
        }
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация столбов с проверкой конфликтов
        /// </summary>
        private int GenerateClimbingPolesImproved(GameObject container)
        {
            if (climbingPolePrefab == null) return 0;
            
            int poleCount = 0;
            
            foreach (var building in lots)
            {
                if (building.IsPark || building.Height < 4f) continue;
                
                // Проверяем лимит столбов
                if (buildingElements[building].PolesCount >= maxPolesPerBuilding) continue;
                
                // Проверяем шанс появления
                if (rand.NextDouble() > climbingPoleChance) continue;
                
                // Пробуем разные углы, пока не найдем подходящий
                var cornerIndices = Enumerable.Range(0, building.Nodes.Count)
                    .OrderBy(x => rand.Next())
                    .ToList();
                
                foreach (int cornerIndex in cornerIndices)
                {
                    var corner = building.Nodes[cornerIndex];
                    var polePosition = new Vector3(corner.X, 0, corner.Y);
                    
                    // Проверяем, не мешает ли другое здание
                    if (BuildingHelper.IsPointNearOtherBuildings(polePosition, building, lots, 1.5f))
                        continue;
                    
                    // Проверяем конфликт с линейными элементами
                    var topPoint = polePosition + Vector3.up * building.Height;
                    if (ConflictsWithLinearElements(polePosition, topPoint))
                        continue;
                    
                    // Создаем столб
                    var pole = Object.Instantiate(climbingPolePrefab, container.transform);
                    pole.transform.position = polePosition * mapScale;
                    pole.transform.localScale = new Vector3(
                        mapScale * 0.15f,
                        building.Height,
                        mapScale * 0.15f
                    );
                    
                    pole.name = $"ClimbingPole_{poleCount}";
                    
                    buildingElements[building].PolesCount++;
                    poleCount++;
                    break; // Только один столб на здание
                }
            }
            
            return poleCount;
        }
        
        /// <summary>
        /// УЛУЧШЕННАЯ генерация платформ с проверкой размещения
        /// </summary>
        private int GenerateIntermediatePlatformsImproved(GameObject container)
        {
            if (platformPrefab == null) return 0;
            
            int platformCount = 0;
            
            foreach (var building in lots)
            {
                if (building.IsPark || building.Height < 6f) continue;
                
                int platformLevels = Mathf.FloorToInt(building.Height / 4f);
                if (platformLevels < 2) continue;
                
                for (int level = 1; level < platformLevels; level++)
                {
                    if (rand.NextDouble() > platformSpawnChance) continue;
                    
                    float height = building.Height * level / (float)platformLevels;
                    int edgeIndex = rand.Next(building.Nodes.Count);
                    
                    var edgeCenter = BuildingHelper.GetEdgeCenter(building, edgeIndex, height);
                    var edgeNormal = BuildingHelper.GetEdgeOutwardNormal(building, edgeIndex);
                    
                    var platformPos = edgeCenter + edgeNormal * 0.8f;
                    
                    // Проверяем, не мешает ли другое здание
                    if (IsPointInsideAnyBuilding(platformPos, building))
                        continue;
                    
                    var platform = Object.Instantiate(platformPrefab, container.transform);
                    platform.transform.position = platformPos * mapScale;
                    platform.transform.rotation = Quaternion.LookRotation(edgeNormal);
                    platform.transform.localScale = new Vector3(
                        mapScale * 1.2f,
                        mapScale * 0.15f,
                        mapScale * 1.2f
                    );
                    
                    platform.name = $"MidPlatform_{platformCount}";
                    platformCount++;
                }
            }
            
            return platformCount;
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
        public int RailsCount = 0;
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