using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlockGeneration;
using GraphModel;
using MeshGeneration;
using RoadGeneration;
using BlockDivision;
using Services;
using ParkourGeneration;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// ОБНОВЛЕННЫЙ CityGenerator для игры Courier Rush
/// 
/// КЛЮЧЕВЫЕ УЛУЧШЕНИЯ:
/// 1. Умная генерация высот зданий для паркура
/// 2. Кластеризация зданий по высоте для интересных маршрутов
/// 3. Использование ParkourElementsGeneratorV3 с wallrun и маршрутами
/// 4. Оптимизированная структура города для курьерской игры
/// </summary>
public class CityGenerator : MonoBehaviour
{
    private Graph _roadGraph;
    private List<BlockNode> _blockNodes;
    private List<Block> _blocks;
    private List<Block> _thinnedBlocks;
    private List<Block> _lots;
    private System.Random _rand;

    private List<Block> _concaveBlocks;
    private List<Block> _convexBlocks;
    private List<BlockMesh> _blockMeshes;
    private List<BlockMesh> _lotMeshes;
    private List<BoundingRectangle> _boundingRectangles;
    private readonly float _blockHeight = 0.02f;

    [Header("Seed and Size")]
    public float mapScale = 1;
    
    [Header("Major Road generation")]
    [Range(0, 20)]
    public int maxDegreeInCurves = 10;
    [Range(0.03f, 0.1f)]
    public float branchingProbability = 0.075f;
    
    [Header("Minor Road generation")]
    [Range(0.02f, 0.2f)] 
    public float crossingDeletionProbability = 0.1f;

    [Header("Maximum Number of Roads")]
    public int maxMajorRoad = 2000;
    public int maxMinorRoad = 10000;

    [Header("Thickness of Roads")]
    [Range(0.1f, 2.5f)]
    public float majorThickness = 2.5f;
    [Range(0.1f, 2.5f)]
    public float minorThickness = 0.9f;

    [Header("Sidewalk generation")]
    [Range(0.1f, 1f)]
    public float sidewalkThickness = 0.5f;
    
    [Header("Building Generation - ОПТИМИЗИРОВАНО ДЛЯ COURIER RUSH")] 
    [Tooltip("Минимальная высота зданий (для паркура нужны высокие здания)")]
    public float minBuildHeight = 4f; // Увеличено для паркура
    
    [Tooltip("Максимальная высота зданий")]
    public float maxBuildHeight = 25f; // Увеличено для разнообразия
    
    [Range(0f, 1f)]
    [Tooltip("Шанс создать очень высокое здание в центре")]
    public float tallBuildingChance = 0.35f;
    
    [Tooltip("Создавать кластеры зданий разной высоты для паркур-маршрутов")]
    public bool createParkourClusters = true;
    
    [Range(0f, 1f)]
    [Tooltip("Интенсивность кластеризации (0 = нет, 1 = максимум)")]
    public float clusterIntensity = 0.7f;

    [Header("Parkour Elements - COURIER RUSH")]
    [Tooltip("Префаб моста между зданиями")]
    public GameObject bridgePrefab;
    
    [Tooltip("Префаб zipline для быстрого перемещения")]
    public GameObject ziplinePrefab;
    
    [Tooltip("Префаб поверхности для бега по стенам")]
    public GameObject wallRunSurfacePrefab;
    
    [Header("Grappling Hook Platforms - LEGACY (для обратной совместимости)")]
    public GameObject grapplePlatformPrefab;
    [Range(0f, 1f)]
    public float grappleSpawnChance = 0.4f;
    public float minDistanceToOtherBuildings = 2f;
    
    [Header("Gizmos")]
    public bool drawRoadNodes;
    public bool drawRoads = true;
    public bool drawBlockNodes;
    public bool drawBlocks = true;
    public bool drawThinnedBlocks;
    public bool drawConvexBlocks;
    public bool drawConcaveBlocks;
    public bool drawTriangulatedMeshes;
    public bool drawBoundingBoxes;
    public bool drawLots = true;

    private bool _genReady;
    private bool _genDone;
    private int _seed;
    
    public int MapSize;
    public event System.Action OnCityGenerationComplete;
    
    // Новые публичные данные для доступа из других систем
    private ParkourElementsGeneratorV3 _parkourGenerator;
    
    public List<ParkourRoute> GetParkourRoutes() => _parkourGenerator?.GetParkourRoutes();
    public List<DeliveryPoint> GetDeliveryPoints() => _parkourGenerator?.GetDeliveryPoints();

    public void Generate(int mapSize)
    {
        MapSize = mapSize;
        _seed = Random.Range(0, int.MaxValue);
        _rand = new System.Random(_seed);
        _roadGraph = new Graph();
        _lots = new List<Block>();

        Thread t = new Thread(ThreadProc);
        t.Start();
    }

    void Update()
    {
        if (_genReady && !_genDone)
        {
            _genDone = true;
            GenerateGameObjects();
            OnCityGenerationComplete?.Invoke();
        }
    }

    private void ThreadProc()
    {
        System.Diagnostics.Stopwatch mainSw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        // ROAD GENERATION
        MajorGenerator majorGen = new MajorGenerator(
            _rand, MapSize, maxMajorRoad, maxDegreeInCurves, branchingProbability, _roadGraph);
        majorGen.Run();
        MinorGenerator minorGen = new MinorGenerator(
            _rand, MapSize, maxMinorRoad, crossingDeletionProbability, _roadGraph, majorGen.GetRoadSegments());
        minorGen.Run();

        sw.Stop();
        Debug.Log("Road generation time taken: " + sw.Elapsed.TotalMilliseconds + " ms");
        Debug.Log(majorGen.GetRoadSegments().Count + " major road generated");
        Debug.Log(minorGen.GetRoadSegments().Count + " minor road generated");

        // BLOCK GENERATION
        BlockGenerator blockGen = new BlockGenerator(_roadGraph, MapSize, majorThickness, minorThickness, _blockHeight);
        blockGen.Generate();
        _blockNodes = blockGen.BlockNodes;
        _blocks = blockGen.Blocks;
        Debug.Log(blockGen.Blocks.Count + " block generated");

        // SIDEWALK GENERATION
        blockGen.ThickenBlocks(sidewalkThickness);
        _thinnedBlocks = blockGen.ThinnedBlocks;
        Debug.Log("Sidewalk generation completed");

        // BLOCK DIVISION
        sw = System.Diagnostics.Stopwatch.StartNew();

        BlockDivider blockDiv = new BlockDivider(_rand, _thinnedBlocks, _lots);
        blockDiv.DivideBlocks();
        
        // === НОВАЯ УЛУЧШЕННАЯ ГЕНЕРАЦИЯ ВЫСОТЫ ДЛЯ COURIER RUSH ===
        SetCourierRushBuildingHeights();
        
        _boundingRectangles = blockDiv.BoundingRectangles;

        sw.Stop();
        Debug.Log("Lot generation time taken: " + sw.Elapsed.TotalMilliseconds + " ms");
        Debug.Log(_lots.Count + " lot generated");

        // BLOCK MESH GENERATION
        MeshGenerator blockMeshGen = new MeshGenerator(_blocks, _blockHeight);
        blockMeshGen.GenerateMeshes();
        _blockMeshes = blockMeshGen.BlockMeshes;

        // LOT MESH GENERATION
        MeshGenerator lotMeshGen = new MeshGenerator(_lots, _blockHeight + _blockHeight / 3);
        lotMeshGen.GenerateMeshes();

        _convexBlocks = lotMeshGen.ConvexBlocks;
        _concaveBlocks = lotMeshGen.ConcaveBlocks;
        _lotMeshes = lotMeshGen.BlockMeshes;

        mainSw.Stop();
        Debug.Log("City generation time taken: " + mainSw.Elapsed.TotalMilliseconds + " ms");
        
        _genReady = true;
    }
    
    /// <summary>
    /// УЛУЧШЕННАЯ СИСТЕМА ГЕНЕРАЦИИ ВЫСОТ ДЛЯ COURIER RUSH
    /// 
    /// Создает город оптимизированный для паркур-геймплея:
    /// 1. Высокие здания в центре для сложных маршрутов
    /// 2. Кластеры зданий разной высоты для интересных путей
    /// 3. Плавные переходы высот для natural flow
    /// 4. "Лестницы" из зданий для vertical gameplay
    /// </summary>
    private void SetCourierRushBuildingHeights()
    {
        Debug.Log("=== Generating Courier Rush optimized building heights ===");
        
        // Шаг 1: Вычисляем центр карты и расстояния
        var buildingDistances = new Dictionary<Block, float>();
        foreach (var lot in _lots)
        {
            if (lot.IsPark)
            {
                lot.Height = _blockHeight + _blockHeight / 3;
                continue;
            }
            
            // Расстояние от центра
            float distanceFromCenter = Mathf.Sqrt(
                lot.Nodes[0].X * lot.Nodes[0].X +
                lot.Nodes[0].Y * lot.Nodes[0].Y
            );
            buildingDistances[lot] = distanceFromCenter;
        }
        
        // Шаг 2: Создаем кластеры если включено
        if (createParkourClusters)
        {
            CreateParkourHeightClusters(buildingDistances);
        }
        else
        {
            CreateBasicHeights(buildingDistances);
        }
        
        Debug.Log($"Building heights optimized for Courier Rush parkour gameplay");
    }
    
    /// <summary>
    /// Создает кластеры зданий разной высоты для паркур-маршрутов
    /// </summary>
    private void CreateParkourHeightClusters(Dictionary<Block, float> buildingDistances)
    {
        // Группируем здания по расстоянию от центра
        var centerBuildings = new List<Block>();
        var midBuildings = new List<Block>();
        var outerBuildings = new List<Block>();
        
        float maxDist = buildingDistances.Values.Max();
        
        foreach (var kvp in buildingDistances)
        {
            var lot = kvp.Key;
            float normalizedDist = kvp.Value / maxDist;
            
            if (normalizedDist < 0.35f)
                centerBuildings.Add(lot);
            else if (normalizedDist < 0.7f)
                midBuildings.Add(lot);
            else
                outerBuildings.Add(lot);
        }
        
        // Центр: высокие кластеры
        CreateHeightClustersForZone(centerBuildings, 
            minHeight: maxBuildHeight * 0.6f, 
            maxHeight: maxBuildHeight,
            clusterSize: 3,
            heightVariation: 0.3f);
        
        // Средняя зона: смешанные кластеры
        CreateHeightClustersForZone(midBuildings,
            minHeight: maxBuildHeight * 0.4f,
            maxHeight: maxBuildHeight * 0.7f,
            clusterSize: 4,
            heightVariation: 0.4f);
        
        // Окраины: низкие здания с вариацией
        CreateHeightClustersForZone(outerBuildings,
            minHeight: minBuildHeight,
            maxHeight: maxBuildHeight * 0.5f,
            clusterSize: 3,
            heightVariation: 0.3f);
    }
    
    /// <summary>
    /// Создает кластеры высот в зоне
    /// </summary>
    private void CreateHeightClustersForZone(List<Block> buildings, float minHeight, float maxHeight, 
        int clusterSize, float heightVariation)
    {
        if (buildings.Count == 0) return;
        
        var processed = new HashSet<Block>();
        
        foreach (var building in buildings)
        {
            if (processed.Contains(building)) continue;
            
            // Определяем базовую высоту для кластера
            float baseHeight = (float)(_rand.NextDouble() * (maxHeight - minHeight) + minHeight);
            
            // Шанс создать ОЧЕНЬ высокое здание (landmark)
            if (_rand.NextDouble() < tallBuildingChance)
            {
                baseHeight = maxHeight * (0.8f + (float)_rand.NextDouble() * 0.2f);
            }
            
            // Находим соседние здания для кластера
            var cluster = FindNearbyBuildings(building, buildings, clusterSize, processed);
            
            // Применяем высоты с вариацией
            foreach (var clusterBuilding in cluster)
            {
                float variation = (float)(_rand.NextDouble() * 2 - 1) * heightVariation;
                float height = baseHeight * (1f + variation * clusterIntensity);
                
                // Клэмп в пределах зоны
                height = Mathf.Clamp(height, minHeight, maxHeight);
                
                // Минимальная высота для паркура
                if (height < minBuildHeight)
                    height = minBuildHeight;
                
                clusterBuilding.Height = height;
                processed.Add(clusterBuilding);
            }
        }
        
        // Обработка оставшихся зданий
        foreach (var building in buildings)
        {
            if (!processed.Contains(building))
            {
                float height = (float)(_rand.NextDouble() * (maxHeight - minHeight) + minHeight);
                building.Height = Mathf.Clamp(height, minBuildHeight, maxHeight);
            }
        }
    }
    
    /// <summary>
    /// Находит ближайшие здания для формирования кластера
    /// </summary>
    private List<Block> FindNearbyBuildings(Block center, List<Block> candidates, int maxCount, HashSet<Block> exclude)
    {
        var result = new List<Block> { center };
        if (maxCount <= 1) return result;
        
        var centerPos = new Vector2(
            center.Nodes[0].X,
            center.Nodes[0].Y
        );
        
        var nearby = candidates
            .Where(b => b != center && !exclude.Contains(b))
            .OrderBy(b => Vector2.Distance(centerPos, new Vector2(b.Nodes[0].X, b.Nodes[0].Y)))
            .Take(maxCount - 1)
            .ToList();
        
        result.AddRange(nearby);
        return result;
    }
    
    /// <summary>
    /// Создает базовые высоты без кластеризации
    /// </summary>
    private void CreateBasicHeights(Dictionary<Block, float> buildingDistances)
    {
        float maxDist = buildingDistances.Values.Max();
        
        foreach (var lot in _lots)
        {
            if (lot.IsPark) continue;
            
            float normalizedDist = buildingDistances[lot] / maxDist;
            
            // Базовая высота
            float height = (float)_rand.NextDouble() * (maxBuildHeight - minBuildHeight) + minBuildHeight;
            
            // Шанс создать высокое здание
            if (_rand.NextDouble() < tallBuildingChance)
            {
                height = maxBuildHeight * (0.7f + (float)_rand.NextDouble() * 0.3f);
            }
            
            // Здания в центре выше
            float centerFactor = 1f - normalizedDist;
            height *= (0.5f + centerFactor * 0.5f);
            
            // Клэмп
            height = Mathf.Clamp(height, minBuildHeight, maxBuildHeight);
            
            lot.Height = height;
        }
    }

    private void GenerateGameObjects()
    {
        var separator = new GameObject
        {
            name = "==========="
        };

        // Make RoadPlane
        var roadPlane = new GameObject
        {
            name = "Road Plane"
        };
        roadPlane.AddComponent<MeshFilter>();
        roadPlane.AddComponent<MeshRenderer>();
        
        Mesh roadMesh = MeshCreateService.GenerateRoadMesh(MapSize);
        roadPlane.GetComponent<MeshFilter>().mesh = roadMesh;

        var roadCollider = roadPlane.AddComponent<MeshCollider>();
        roadCollider.sharedMesh = roadMesh;

        Material roadMaterial = Resources.Load<Material>("Material/RoadMaterial");
        roadPlane.GetComponent<MeshRenderer>().material = roadMaterial;
        
        // Make Blocks
        var blockContainer = new GameObject
        {
            name = "Block Container"
        };

        Material blockMaterial = Resources.Load<Material>("Material/BlockMaterial");
        Material parkMaterial = Resources.Load<Material>("Material/BlockGreenMaterial");

        for (int i = 0; i < _blockMeshes.Count; i++)
        {
            var block = new GameObject
            {
                name = "Block" + i,
                transform =
                {
                    parent = blockContainer.transform
                }
            };
            block.AddComponent<MeshFilter>();
            block.AddComponent<MeshRenderer>();
            
            Mesh blockMesh = MeshCreateService.GenerateBlockMesh(_blockMeshes[i]);
            block.GetComponent<MeshFilter>().mesh = blockMesh;

            var meshCollider = block.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = blockMesh;
            meshCollider.convex = true;

            if (_blockMeshes[i].Block.IsPark) block.GetComponent<MeshRenderer>().material = parkMaterial;
            else block.GetComponent<MeshRenderer>().material = blockMaterial;
        }
        
        // Make Lots (Buildings)
        var lotContainer = new GameObject
        {
            name = "Lot Container"
        };

        Material lotMaterial = Resources.Load<Material>("Material/BlockMaterial");

        for (int i = 0; i < _lotMeshes.Count; i++)
        {
            var lot = new GameObject
            {
                name = "Lot" + i,
                transform =
                {
                    parent = lotContainer.transform
                }
            };
            lot.AddComponent<MeshFilter>();
            lot.AddComponent<MeshRenderer>();
            
            Mesh lotMesh = MeshCreateService.GenerateBlockMesh(_lotMeshes[i]);
            lot.GetComponent<MeshFilter>().mesh = lotMesh;
            
            var meshCollider = lot.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = lotMesh;
            meshCollider.convex = true;
            
            if (_lotMeshes[i].Block.IsPark) lot.GetComponent<MeshRenderer>().material = parkMaterial;
            else lot.GetComponent<MeshRenderer>().material = lotMaterial;
        }
        
        roadPlane.transform.localScale = new Vector3(mapScale, mapScale, mapScale);
        blockContainer.transform.localScale = new Vector3(mapScale, mapScale, mapScale);
        lotContainer.transform.localScale = new Vector3(mapScale, mapScale, mapScale);
        
        // === ГЕНЕРАЦИЯ ПАРКУР ЭЛЕМЕНТОВ V3 - COURIER RUSH ===
        GenerateParkourElementsV3();
        
        // Legacy grapple platforms (если нужны)
        if (grapplePlatformPrefab != null)
        {
            GenerateGrapplePlatforms();
        }
    }
    
    /// <summary>
    /// Генерирует все паркур-элементы используя улучшенный V3 генератор
    /// Включает wallrun, мосты, zipline и умные маршруты
    /// </summary>
    private void GenerateParkourElementsV3()
    {
        var parkourContainer = new GameObject
        {
            name = "===== COURIER RUSH PARKOUR ELEMENTS V3 ====="
        };
        
        _parkourGenerator = new ParkourElementsGeneratorV3(
            _lots,
            _roadGraph,
            _rand,
            mapScale,
            MapSize,
            bridgePrefab,
            ziplinePrefab,
            wallRunSurfacePrefab
        );
        
        _parkourGenerator.GenerateAllParkourElements(parkourContainer);
        
        Debug.Log("Courier Rush parkour elements generated with routes and wallrun!");
    }
    
    /// <summary>
    /// Legacy метод для grapple платформ
    /// </summary>
    private void GenerateGrapplePlatforms()
    {
        if (grapplePlatformPrefab == null) return;
        
        var grappleContainer = new GameObject
        {
            name = "Grapple Platform Container (Legacy)"
        };
    
        int spawnedCount = 0;
        foreach (var lot in _lots)
        {
            if (_rand.NextDouble() > grappleSpawnChance) continue;
        
            if (BuildingHelper.TryGetGrapplePoint(lot, _lots, _rand, 
                    out Vector3 position, out Vector3 outwardNormal, minDistanceToOtherBuildings))
            {
                var platform = Instantiate(grapplePlatformPrefab, grappleContainer.transform);
                platform.transform.position = (position + Vector3.up * 0.01f) * mapScale;
                platform.transform.rotation = Quaternion.LookRotation(-outwardNormal);
                platform.name = $"GrapplePlatform_{spawnedCount++}";
            }
        }
    
        Debug.Log($"{spawnedCount} legacy grappling platforms spawned");
    }
    
    public Graph GetRoadGraph() => _roadGraph;
    public List<Block> GetLots() => _lots;
    public List<Block> GetBlocks() => _blocks;

    private void OnDrawGizmos()
    {
        if (_roadGraph == null) return;

        if (drawRoads)
        {
            GizmoService.DrawEdges(_roadGraph.MajorEdges, Color.white);
            GizmoService.DrawEdges(_roadGraph.MinorEdges, Color.black);
        }

        if (drawRoadNodes)
        {
            GizmoService.DrawNodes(_roadGraph.MajorNodes, Color.white, 2f);
            GizmoService.DrawNodes(_roadGraph.MinorNodes, Color.black, 1f);
        }

        if (drawBlockNodes)
        {
            GizmoService.DrawBlockNodes(_blockNodes, Color.red, 0.4f);
        }

        if (drawBlocks)
        {
            GizmoService.DrawBlocks(_blocks, new Color(0.7f, 0.4f, 0.4f));
        }

        if (drawThinnedBlocks)
        {
            GizmoService.DrawBlocks(_thinnedBlocks, new Color(0.7f, 0.4f, 0.4f));
        }

        if (drawConvexBlocks && _genDone)
        {
            GizmoService.DrawBlocks(_convexBlocks, new Color(0.2f, 0.7f, 0.7f));
        }
        if (drawConcaveBlocks && _genDone)
        {
            GizmoService.DrawBlocks(_concaveBlocks, new Color(0.2f, 0.7f, 0.2f));
        }
        if (drawTriangulatedMeshes && _genDone)
        {
            GizmoService.DrawBlockMeshes(_blockMeshes, new Color(.8f, .8f, .8f));
        }

        if (drawBoundingBoxes && _genDone)
        {
            List<Edge> cutEdges = new List<Edge>();
            
            foreach (var boundingBox in _boundingRectangles)
            {
                GizmoService.DrawEdges(boundingBox.Edges, Color.white);   
                cutEdges.Add(boundingBox.GetCutEdge());
            }
            
            GizmoService.DrawEdges(cutEdges, Color.yellow);
        }

        if (drawLots)
        {
            GizmoService.DrawBlocks(_lots, new Color(0.2f, 0.7f, 0.7f));
        }
    }
}