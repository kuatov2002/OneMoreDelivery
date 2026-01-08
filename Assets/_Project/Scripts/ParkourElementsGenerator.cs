using System;
using System.Collections.Generic;
using System.Linq;
using BlockGeneration;
using GraphModel;
using Services;
using UnityEngine;

namespace ParkourGeneration
{
    /// <summary>
    /// ВЕРСИЯ 3.0 - Генератор паркур-элементов для игры Courier Rush
    /// 
    /// ОСНОВНЫЕ УЛУЧШЕНИЯ:
    /// 1. Wallrun поверхности на зданиях
    /// 2. Умная система паркур-маршрутов
    /// 3. Зоны сложности (легкие/средние/сложные маршруты)
    /// 4. Кластеризация элементов для создания "воздушных магистралей"
    /// 5. Точки доставки (delivery points) для игрового процесса
    /// </summary>
    public class ParkourElementsGeneratorV3
    {
        // Основные данные
        private readonly List<Block> lots;
        private readonly Graph roadGraph;
        private readonly System.Random rand;
        private readonly float mapScale;
        private readonly int mapSize;
        
        // Префабы
        private readonly GameObject bridgePrefab;
        private readonly GameObject ziplinePrefab;
        private readonly GameObject wallRunSurfacePrefab;
        
        // Структуры данных для отслеживания
        private Dictionary<Block, BuildingParkourData> buildingData;
        private List<LinearParkourElement> placedLinearElements;
        private List<ParkourRoute> parkourRoutes;
        private List<DeliveryPoint> deliveryPoints;
        
        // === НАСТРОЙКИ ГЕНЕРАЦИИ ===
        
        // Wallrun настройки
        private readonly float wallRunMinHeight = 2f;      // Минимальная высота для wallrun
        private readonly float wallRunMaxHeight = 8f;      // Максимальная высота для wallrun
        private readonly float wallRunWidth = 3f;          // Ширина wallrun поверхности
        private readonly float wallRunThickness = 0.1f;    // Толщина wallrun поверхности
        private readonly int maxWallRunsPerBuilding = 3;   // Максимум wallrun на здание
        
        // Ограничения на элементы
        private readonly int maxBridgesPerBuilding = 2;
        private readonly int maxZiplinesPerBuilding = 2;
        
        // Дистанции
        private readonly float minBuildingDistance = 3f;
        private readonly float maxBridgeDistance = 15f;
        private readonly float maxZiplineDistance = 30f;
        private readonly float minZiplineAngle = 10f;
        private readonly float raycastCheckRadius = 0.5f;
        
        // Шансы появления (динамические, зависят от зоны)
        private float bridgeSpawnChance = 1;
        private float ziplineSpawnChance = 1f;
        private float wallRunSpawnChance = 1f;
        
        // Зоны сложности
        private Dictionary<DifficultyZone, List<Block>> difficultyZones;
        
        public ParkourElementsGeneratorV3(
            List<Block> buildings,
            Graph roads,
            System.Random random,
            float scale,
            int mapSizeValue,
            GameObject bridge = null,
            GameObject zipline = null,
            GameObject wallRun = null)
        {
            lots = buildings;
            roadGraph = roads;
            rand = random;
            mapScale = scale;
            mapSize = mapSizeValue;
            
            bridgePrefab = bridge;
            ziplinePrefab = zipline;
            wallRunSurfacePrefab = wallRun;
            
            // Инициализация структур данных
            buildingData = new Dictionary<Block, BuildingParkourData>();
            placedLinearElements = new List<LinearParkourElement>();
            parkourRoutes = new List<ParkourRoute>();
            deliveryPoints = new List<DeliveryPoint>();
            difficultyZones = new Dictionary<DifficultyZone, List<Block>>();
            
            // Создаем записи для каждого здания
            foreach (var building in lots)
            {
                buildingData[building] = new BuildingParkourData();
            }
        }
        
        /// <summary>
        /// ГЛАВНЫЙ МЕТОД: Генерирует все паркур-элементы
        /// Порядок генерации важен для создания осмысленных маршрутов
        /// </summary>
        public void GenerateAllParkourElements(GameObject parentContainer)
        {
            Debug.Log("=== Starting V3 Parkour Generation for Courier Rush ===");
            
            // ШАГ 1: Анализ карты и создание зон сложности
            AnalyzeMapAndCreateZones();
            
            // ШАГ 2: Создание паркур-маршрутов (умная система)
            CreateParkourRoutes();
            
            // ШАГ 3: Размещение точек доставки
            GenerateDeliveryPoints(parentContainer);
            
            // ШАГ 4: Генерация элементов вдоль маршрутов
            var bridgesContainer = new GameObject("Bridges Container (Route-Based)");
            bridgesContainer.transform.SetParent(parentContainer.transform);
            int bridgesCount = GenerateBridgesAlongRoutes(bridgesContainer);
            
            var ziplinesContainer = new GameObject("Ziplines Container (Route-Based)");
            ziplinesContainer.transform.SetParent(parentContainer.transform);
            int ziplinesCount = GenerateZiplinesAlongRoutes(ziplinesContainer);
            
            var wallRunContainer = new GameObject("WallRun Surfaces Container");
            wallRunContainer.transform.SetParent(parentContainer.transform);
            int wallRunCount = GenerateWallRunSurfaces(wallRunContainer);
            
            // ШАГ 5: Дополнительные элементы для заполнения
            GenerateAdditionalElements(bridgesContainer, ziplinesContainer);
            
            Debug.Log($"=== Parkour Generation Complete ===");
            Debug.Log($"Bridges: {bridgesCount} | Ziplines: {ziplinesCount} | WallRuns: {wallRunCount}");
            Debug.Log($"Routes: {parkourRoutes.Count} | Delivery Points: {deliveryPoints.Count}");
            Debug.Log($"Easy zones: {difficultyZones[DifficultyZone.Easy].Count} buildings");
            Debug.Log($"Medium zones: {difficultyZones[DifficultyZone.Medium].Count} buildings");
            Debug.Log($"Hard zones: {difficultyZones[DifficultyZone.Hard].Count} buildings");
        }
        
        // ===================================================================
        // ШАГ 1: АНАЛИЗ КАРТЫ И ЗОНЫ СЛОЖНОСТИ
        // ===================================================================
        
        /// <summary>
        /// Анализирует карту и создает зоны сложности
        /// Центр = сложная зона (высокие здания, сложные маршруты)
        /// Середина = средняя зона (смешанная высота)
        /// Окраины = легкая зона (низкие здания, простые маршруты)
        /// </summary>
        private void AnalyzeMapAndCreateZones()
        {
            difficultyZones[DifficultyZone.Easy] = new List<Block>();
            difficultyZones[DifficultyZone.Medium] = new List<Block>();
            difficultyZones[DifficultyZone.Hard] = new List<Block>();
            
            foreach (var building in lots)
            {
                if (building.IsPark) continue;
                
                // Вычисляем расстояние от центра карты
                float distanceFromCenter = Mathf.Sqrt(
                    building.Nodes[0].X * building.Nodes[0].X +
                    building.Nodes[0].Y * building.Nodes[0].Y
                );
                
                float normalizedDistance = distanceFromCenter / mapSize;
                
                // Определяем зону сложности
                DifficultyZone zone;
                if (normalizedDistance < 0.3f)
                {
                    zone = DifficultyZone.Hard;
                    buildingData[building].DifficultyZone = zone;
                }
                else if (normalizedDistance < 0.65f)
                {
                    zone = DifficultyZone.Medium;
                    buildingData[building].DifficultyZone = zone;
                }
                else
                {
                    zone = DifficultyZone.Easy;
                    buildingData[building].DifficultyZone = zone;
                }
                
                difficultyZones[zone].Add(building);
                
                // Настраиваем параметры в зависимости от зоны
                buildingData[building].IsHighPriority = zone == DifficultyZone.Hard && building.Height > 10f;
            }
            
            Debug.Log($"Map analyzed: {difficultyZones[DifficultyZone.Easy].Count} easy, " +
                      $"{difficultyZones[DifficultyZone.Medium].Count} medium, " +
                      $"{difficultyZones[DifficultyZone.Hard].Count} hard buildings");
        }
        
        // ===================================================================
        // ШАГ 2: СОЗДАНИЕ ПАРКУР-МАРШРУТОВ
        // ===================================================================
        
        /// <summary>
        /// Создает умные паркур-маршруты через город
        /// Маршруты соединяют стратегически важные здания
        /// </summary>
        private void CreateParkourRoutes()
        {
            // 1. Создаем основные "магистрали" через центр города
            CreateMainRoutes();
            
            // 2. Создаем второстепенные маршруты в каждой зоне
            CreateZonalRoutes(DifficultyZone.Easy, 3);
            CreateZonalRoutes(DifficultyZone.Medium, 4);
            CreateZonalRoutes(DifficultyZone.Hard, 5);
            
            Debug.Log($"Created {parkourRoutes.Count} parkour routes");
        }
        
        /// <summary>
        /// Создает главные маршруты через центр карты
        /// </summary>
        private void CreateMainRoutes()
        {
            var hardZoneBuildings = difficultyZones[DifficultyZone.Hard]
                .OrderByDescending(b => b.Height)
                .Take(10)
                .ToList();
            
            if (hardZoneBuildings.Count < 3) return;
            
            // Создаем 2-3 главных маршрута
            for (int i = 0; i < 2; i++)
            {
                var route = new ParkourRoute
                {
                    Difficulty = DifficultyZone.Hard,
                    IsMainRoute = true
                };
                
                // Берем 4-6 высоких зданий для маршрута
                int buildingCount = rand.Next(4, 7);
                var routeBuildings = hardZoneBuildings
                    .OrderBy(x => rand.Next())
                    .Take(buildingCount)
                    .ToList();
                
                // Сортируем здания для создания логичного пути
                routeBuildings = SortBuildingsForRoute(routeBuildings);
                route.Buildings.AddRange(routeBuildings);
                
                parkourRoutes.Add(route);
            }
        }
        
        /// <summary>
        /// Создает маршруты внутри зоны сложности
        /// </summary>
        private void CreateZonalRoutes(DifficultyZone zone, int routeCount)
        {
            var zoneBuildings = difficultyZones[zone].ToList();
            if (zoneBuildings.Count < 3) return;
            
            for (int i = 0; i < routeCount && zoneBuildings.Count >= 3; i++)
            {
                var route = new ParkourRoute
                {
                    Difficulty = zone,
                    IsMainRoute = false
                };
                
                // Берем 3-5 зданий для маршрута
                int buildingCount = Mathf.Min(rand.Next(3, 6), zoneBuildings.Count);
                var routeBuildings = zoneBuildings
                    .OrderBy(x => rand.Next())
                    .Take(buildingCount)
                    .ToList();
                
                // Убираем использованные здания
                foreach (var b in routeBuildings)
                {
                    zoneBuildings.Remove(b);
                }
                
                routeBuildings = SortBuildingsForRoute(routeBuildings);
                route.Buildings.AddRange(routeBuildings);
                
                parkourRoutes.Add(route);
            }
        }
        
        /// <summary>
        /// Сортирует здания для создания логичного пути
        /// (ближайший сосед)
        /// </summary>
        private List<Block> SortBuildingsForRoute(List<Block> buildings)
        {
            if (buildings.Count <= 1) return buildings;
            
            var sorted = new List<Block>();
            var remaining = new List<Block>(buildings);
            
            // Начинаем с случайного здания
            var current = remaining[rand.Next(remaining.Count)];
            sorted.Add(current);
            remaining.Remove(current);
            
            // Добавляем ближайших соседей
            while (remaining.Count > 0)
            {
                Block nearest = null;
                float minDist = float.MaxValue;
                
                foreach (var building in remaining)
                {
                    float dist = GetBuildingDistance(current, building);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = building;
                    }
                }
                
                if (nearest != null)
                {
                    sorted.Add(nearest);
                    remaining.Remove(nearest);
                    current = nearest;
                }
                else
                {
                    break;
                }
            }
            
            return sorted;
        }
        
        // ===================================================================
        // ШАГ 3: ТОЧКИ ДОСТАВКИ
        // ===================================================================
        
        /// <summary>
        /// Генерирует точки доставки для игрового процесса
        /// Размещает их на разных высотах и в разных зонах
        /// </summary>
        private void GenerateDeliveryPoints(GameObject parentContainer)
        {
            var deliveryContainer = new GameObject("Delivery Points");
            deliveryContainer.transform.SetParent(parentContainer.transform);
            
            // Генерируем точки в каждой зоне
            GenerateDeliveryPointsInZone(DifficultyZone.Easy, 5, deliveryContainer);
            GenerateDeliveryPointsInZone(DifficultyZone.Medium, 7, deliveryContainer);
            GenerateDeliveryPointsInZone(DifficultyZone.Hard, 10, deliveryContainer);
        }
        
        private void GenerateDeliveryPointsInZone(DifficultyZone zone, int count, GameObject container)
        {
            var zoneBuildings = difficultyZones[zone];
            if (zoneBuildings.Count == 0) return;
            
            for (int i = 0; i < count; i++)
            {
                var building = zoneBuildings[rand.Next(zoneBuildings.Count)];
                
                // Выбираем высоту для точки доставки
                DeliveryPointType type;
                float heightFactor;
                
                if (rand.NextDouble() < 0.3) // 30% на земле
                {
                    type = DeliveryPointType.Ground;
                    heightFactor = 0f;
                }
                else if (rand.NextDouble() < 0.5) // 35% на средней высоте
                {
                    type = DeliveryPointType.MidLevel;
                    heightFactor = 0.5f;
                }
                else // 35% на крыше
                {
                    type = DeliveryPointType.Roof;
                    heightFactor = 1f;
                }
                
                var position = BuildingHelper.GetRoofCenter(building);
                position.y = building.Height * heightFactor;
                
                var deliveryPoint = new DeliveryPoint
                {
                    Building = building,
                    Position = position,
                    Type = type,
                    Difficulty = zone
                };
                
                deliveryPoints.Add(deliveryPoint);
                buildingData[building].HasDeliveryPoint = true;
                
                // Создаем маркер (простой куб)
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(container.transform);
                marker.transform.position = position * mapScale;
                marker.transform.localScale = Vector3.one * mapScale * 0.5f;
                marker.name = $"DeliveryPoint_{zone}_{type}_{i}";
                
                // Цвет в зависимости от сложности
                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = zone == DifficultyZone.Easy ? Color.green :
                                   zone == DifficultyZone.Medium ? Color.yellow : Color.red;
                    renderer.material = material;
                }
            }
        }
        
        // ===================================================================
        // ШАГ 4: ГЕНЕРАЦИЯ WALLRUN ПОВЕРХНОСТЕЙ
        // ===================================================================
        
        /// <summary>
        /// Генерирует wallrun поверхности на стенах зданий
        /// Размещает их стратегически для создания паркур-путей
        /// </summary>
        private int GenerateWallRunSurfaces(GameObject container)
        {
            if (wallRunSurfacePrefab == null) return 0;
            
            int wallRunCount = 0;
            
            foreach (var building in lots)
            {
                if (building.IsPark) continue;
                if (building.Height < wallRunMinHeight) continue;
                
                // Проверяем шанс появления (зависит от зоны)
                var zone = buildingData[building].DifficultyZone;
                float spawnChance = zone == DifficultyZone.Hard ? 0.7f :
                                  zone == DifficultyZone.Medium ? 0.5f : 0.3f;
                
                if (rand.NextDouble() > spawnChance) continue;
                
                // Определяем количество wallrun на это здание
                int wallRunsForBuilding = rand.Next(1, maxWallRunsPerBuilding + 1);
                
                // Если здание на маршруте, добавляем больше wallrun
                bool isOnRoute = parkourRoutes.Any(r => r.Buildings.Contains(building));
                if (isOnRoute)
                {
                    wallRunsForBuilding = maxWallRunsPerBuilding;
                }
                
                for (int i = 0; i < wallRunsForBuilding; i++)
                {
                    // Выбираем случайную стену
                    int edgeIndex = rand.Next(0, building.Nodes.Count);
                    
                    // Вычисляем высоту размещения
                    float minH = Mathf.Max(wallRunMinHeight, building.Height * 0.2f);
                    float maxH = Mathf.Min(wallRunMaxHeight, building.Height * 0.8f);
                    float height = (float)(rand.NextDouble() * (maxH - minH) + minH);
                    
                    // Получаем центр стены и нормаль
                    var edgeCenter = BuildingHelper.GetEdgeCenter(building, edgeIndex, height);
                    var outwardNormal = BuildingHelper.GetEdgeOutwardNormal(building, edgeIndex);
                    
                    // Проверяем, не слишком ли близко к другим зданиям
                    if (BuildingHelper.IsPointNearOtherBuildings(edgeCenter, building, lots, 1f))
                    {
                        continue;
                    }
                    
                    // Создаем wallrun поверхность
                    var wallRun = UnityEngine.Object.Instantiate(wallRunSurfacePrefab, container.transform);
                    
                    // Позиционируем немного от стены
                    var position = edgeCenter + outwardNormal * wallRunThickness;
                    wallRun.transform.position = position * mapScale;
                    wallRun.transform.rotation = Quaternion.LookRotation(-outwardNormal);
                    
                    // Размер wallrun поверхности
                    wallRun.transform.localScale = new Vector3(
                        wallRunWidth * mapScale,
                        wallRunThickness * mapScale,
                        wallRunWidth * mapScale
                    );
                    
                    wallRun.name = $"WallRun_{building.Nodes[0].X}_{building.Nodes[0].Y}_{i}";
                    
                    buildingData[building].WallRunCount++;
                    wallRunCount++;
                }
            }
            
            return wallRunCount;
        }
        
        // ===================================================================
        // ШАГ 5: ГЕНЕРАЦИЯ МОСТОВ И ZIPLINE ВДОЛЬ МАРШРУТОВ
        // ===================================================================
        
        /// <summary>
        /// Генерирует мосты вдоль созданных паркур-маршрутов
        /// Это создает логичные пути для игрока
        /// </summary>
        private int GenerateBridgesAlongRoutes(GameObject container)
        {
            if (bridgePrefab == null) return 0;
            
            int bridgeCount = 0;
            HashSet<(Block, Block)> connectedPairs = new HashSet<(Block, Block)>();
            
            // Сначала соединяем здания вдоль маршрутов
            foreach (var route in parkourRoutes)
            {
                for (int i = 0; i < route.Buildings.Count - 1; i++)
                {
                    var building1 = route.Buildings[i];
                    var building2 = route.Buildings[i + 1];
                    
                    // Проверяем лимиты
                    if (buildingData[building1].BridgesCount >= maxBridgesPerBuilding) continue;
                    if (buildingData[building2].BridgesCount >= maxBridgesPerBuilding) continue;
                    
                    var pair = (building1, building2);
                    if (connectedPairs.Contains(pair) || connectedPairs.Contains((building2, building1)))
                        continue;
                    
                    // Проверяем возможность создания моста
                    if (TryCreateBridge(building1, building2, container, ref bridgeCount))
                    {
                        connectedPairs.Add(pair);
                        buildingData[building1].BridgesCount++;
                        buildingData[building2].BridgesCount++;
                    }
                }
            }
            
            return bridgeCount;
        }
        
        /// <summary>
        /// Генерирует zipline вдоль маршрутов
        /// </summary>
        private int GenerateZiplinesAlongRoutes(GameObject container)
        {
            if (ziplinePrefab == null) return 0;
            
            int ziplineCount = 0;
            
            // Создаем zipline между далекими зданиями на маршрутах
            foreach (var route in parkourRoutes)
            {
                // Пропускаем здания и создаем zipline через 1-2 здания
                for (int i = 0; i < route.Buildings.Count - 2; i++)
                {
                    var startBuilding = route.Buildings[i];
                    
                    // Пробуем создать zipline к зданиям на расстоянии 2-3 шага
                    for (int j = i + 2; j < Mathf.Min(i + 4, route.Buildings.Count); j++)
                    {
                        var endBuilding = route.Buildings[j];
                        
                        if (buildingData[startBuilding].ZiplinesCount >= maxZiplinesPerBuilding) break;
                        if (buildingData[endBuilding].ZiplinesCount >= maxZiplinesPerBuilding) continue;
                        
                        // Проверка высоты (zipline идет вниз)
                        if (startBuilding.Height <= endBuilding.Height) continue;
                        
                        if (TryCreateZipline(startBuilding, endBuilding, container, ref ziplineCount))
                        {
                            buildingData[startBuilding].ZiplinesCount++;
                            buildingData[endBuilding].ZiplinesCount++;
                            break; // Один zipline с этого здания достаточно
                        }
                    }
                }
            }
            
            return ziplineCount;
        }
        
        /// <summary>
        /// Дополнительные элементы для заполнения пространства
        /// </summary>
        private void GenerateAdditionalElements(GameObject bridgeContainer, GameObject ziplineContainer)
        {
            // Добавляем немного случайных элементов для разнообразия
            int additionalBridges = 0;
            int additionalZiplines = 0;
            
            var shuffledBuildings = lots.Where(b => !b.IsPark).OrderBy(x => rand.Next()).ToList();
            
            foreach (var building in shuffledBuildings)
            {
                if (additionalBridges >= 10) break;
                
                if (buildingData[building].BridgesCount < maxBridgesPerBuilding)
                {
                    var nearby = FindNearbyBuildingsWithSimilarHeight(building, maxBridgeDistance, 2f);
                    foreach (var target in nearby.Take(2))
                    {
                        if (TryCreateBridge(building, target, bridgeContainer, ref additionalBridges))
                        {
                            buildingData[building].BridgesCount++;
                            buildingData[target].BridgesCount++;
                            break;
                        }
                    }
                }
            }
            
            Debug.Log($"Added {additionalBridges} additional bridges");
        }
        
        // ===================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ===================================================================
        
        private bool TryCreateBridge(Block building1, Block building2, GameObject container, ref int count)
        {
            var point1 = FindClosestRoofPoint(building1, building2);
            var point2 = FindClosestRoofPoint(building2, building1);
            
            float distance = Vector3.Distance(point1, point2);
            float heightDiff = Mathf.Abs(point1.y - point2.y);
            
            if (distance < minBuildingDistance || distance > maxBridgeDistance) return false;
            if (heightDiff > 2f) return false; // Мосты только между зданиями похожей высоты
            
            if (PathIntersectsBuildings(point1, point2, building1, building2)) return false;
            if (ConflictsWithLinearElements(point1, point2)) return false;
            
            var bridge = UnityEngine.Object.Instantiate(bridgePrefab, container.transform);
            var bridgeCenter = (point1 + point2) / 2f;
            bridge.transform.position = bridgeCenter * mapScale;
            bridge.transform.rotation = Quaternion.LookRotation((point2 - point1).normalized);
            bridge.transform.localScale = new Vector3(
                mapScale * 0.5f,
                mapScale * 0.1f,
                distance * mapScale
            );
            
            bridge.name = $"Bridge_{count}";
            
            placedLinearElements.Add(new LinearParkourElement
            {
                StartPoint = point1,
                EndPoint = point2,
                Type = ParkourElementType.Bridge,
                Building1 = building1,
                Building2 = building2
            });
            
            count++;
            return true;
        }
        
        private bool TryCreateZipline(Block startBuilding, Block endBuilding, GameObject container, ref int count)
        {
            var startPos = BuildingHelper.GetRandomRoofEdgePoint(startBuilding, rand);
            var endPos = BuildingHelper.GetRandomRoofEdgePoint(endBuilding, rand);
            
            float distance = Vector3.Distance(startPos, endPos);
            float heightDiff = startPos.y - endPos.y;
            
            if (distance < 5f || distance > maxZiplineDistance) return false;
            if (heightDiff < 3f) return false; // Zipline должен идти вниз
            
            float horizontalDist = Vector2.Distance(
                new Vector2(startPos.x, startPos.z),
                new Vector2(endPos.x, endPos.z)
            );
            float angle = Mathf.Atan2(heightDiff, horizontalDist) * Mathf.Rad2Deg;
            
            if (angle < minZiplineAngle) return false;
            
            if (PathIntersectsBuildings(startPos, endPos, startBuilding, endBuilding)) return false;
            if (ConflictsWithLinearElements(startPos, endPos)) return false;
            
            var zipline = UnityEngine.Object.Instantiate(ziplinePrefab, container.transform);
            var ziplineCenter = (startPos + endPos) / 2f;
            zipline.transform.position = ziplineCenter * mapScale;
            zipline.transform.rotation = Quaternion.LookRotation((endPos - startPos).normalized);
            zipline.transform.localScale = new Vector3(
                mapScale * 0.1f,
                mapScale * 0.1f,
                distance * mapScale
            );
            
            zipline.name = $"Zipline_{count}";
            
            placedLinearElements.Add(new LinearParkourElement
            {
                StartPoint = startPos,
                EndPoint = endPos,
                Type = ParkourElementType.Zipline,
                Building1 = startBuilding,
                Building2 = endBuilding
            });
            
            count++;
            return true;
        }
        
        private float GetBuildingDistance(Block a, Block b)
        {
            var centerA = new Vector2(a.Nodes.Average(n => n.X), a.Nodes.Average(n => n.Y));
            var centerB = new Vector2(b.Nodes.Average(n => n.X), b.Nodes.Average(n => n.Y));
            return Vector2.Distance(centerA, centerB);
        }
        
        private List<Block> FindNearbyBuildingsWithSimilarHeight(Block building, float maxDistance, float heightTolerance)
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
        
        private bool PathIntersectsBuildings(Vector3 start, Vector3 end, Block exclude1, Block exclude2)
        {
            int checkPoints = Mathf.Max(5, Mathf.CeilToInt(Vector3.Distance(start, end) / 2f));
            
            for (int i = 0; i <= checkPoints; i++)
            {
                float t = i / (float)checkPoints;
                Vector3 checkPoint = Vector3.Lerp(start, end, t);
                Vector2 checkPoint2D = new Vector2(checkPoint.x, checkPoint.z);
                
                foreach (var building in lots)
                {
                    if (building == exclude1 || building == exclude2) continue;
                    
                    if (BuildingHelper.IsPointInside(building, checkPoint2D))
                    {
                        if (checkPoint.y <= building.Height && checkPoint.y >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private bool ConflictsWithLinearElements(Vector3 start, Vector3 end)
        {
            foreach (var element in placedLinearElements)
            {
                if (LineSegmentsIntersect3D(start, end, element.StartPoint, element.EndPoint, raycastCheckRadius))
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool LineSegmentsIntersect3D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float radius)
        {
            Vector3 closestPoint = ClosestPointOnLineSegment(p1, p2, p3, p4);
            return closestPoint.magnitude < radius;
        }
        
        private Vector3 ClosestPointOnLineSegment(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector3 d1 = p2 - p1;
            Vector3 d2 = p4 - p3;
            Vector3 r = p1 - p3;
            
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            
            float s = 0f, t = 0f;
            
            if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
            {
                return p1 - p3;
            }
            
            if (a <= Mathf.Epsilon)
            {
                s = 0.0f;
                t = Mathf.Clamp01(f / e);
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
                    
                    s = denom != 0.0f ? Mathf.Clamp01((b * f - c * e) / denom) : 0.0f;
                    t = Mathf.Clamp01((b * s + f) / e);
                }
            }
            
            Vector3 c1 = p1 + d1 * s;
            Vector3 c2 = p3 + d2 * t;
            
            return c1 - c2;
        }
        
        // Публичные методы для доступа к данным
        public List<ParkourRoute> GetParkourRoutes() => parkourRoutes;
        public List<DeliveryPoint> GetDeliveryPoints() => deliveryPoints;
    }
    
    // ===================================================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ
    // ===================================================================
    
    public enum DifficultyZone
    {
        Easy,    // Окраины города - низкие здания, простые маршруты
        Medium,  // Средняя зона - смешанная сложность
        Hard     // Центр города - высокие здания, сложные маршруты
    }
    
    public enum DeliveryPointType
    {
        Ground,   // На земле
        MidLevel, // На средней высоте здания
        Roof      // На крыше
    }
    
    /// <summary>
    /// Хранит данные о паркур-элементах на здании
    /// </summary>
    public class BuildingParkourData
    {
        public int BridgesCount = 0;
        public int ZiplinesCount = 0;
        public int WallRunCount = 0;
        public bool HasDeliveryPoint = false;
        public bool IsHighPriority = false;
        public DifficultyZone DifficultyZone = DifficultyZone.Medium;
    }
    
    /// <summary>
    /// Представляет паркур-маршрут через город
    /// </summary>
    public class ParkourRoute
    {
        public List<Block> Buildings = new List<Block>();
        public DifficultyZone Difficulty;
        public bool IsMainRoute = false;
    }
    
    /// <summary>
    /// Точка доставки для игрового процесса
    /// </summary>
    public class DeliveryPoint
    {
        public Block Building;
        public Vector3 Position;
        public DeliveryPointType Type;
        public DifficultyZone Difficulty;
    }
    
    /// <summary>
    /// Линейный паркур-элемент (мост или zipline)
    /// </summary>
    public class LinearParkourElement
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;
        public ParkourElementType Type;
        public Block Building1;
        public Block Building2;
    }
    
    public enum ParkourElementType
    {
        Bridge,
        Zipline
    }
}