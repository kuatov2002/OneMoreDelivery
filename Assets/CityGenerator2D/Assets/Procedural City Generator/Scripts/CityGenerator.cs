using System;
using System.Collections.Generic;
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
    
    [Header("Building generation - УЛУЧШЕНО ДЛЯ ПАРКУРА")] 
    public float minBuildHeight = 3; // Увеличено для лучшего паркура
    public float maxBuildHeight = 20; // Увеличено для большего разнообразия
    [Range(0f, 1f)]
    public float tallBuildingChance = 0.3f; // Шанс создать очень высокое здание
    public bool createClusteredHeights = true; // Группировать здания разной высоты рядом

    [Header("Parkour Elements - НОВОЕ!")]
    public GameObject bridgePrefab;
    public GameObject ziplinePrefab;
    public GameObject wallRunSurfacePrefab;
    
    [Header("Grappling Hook Platforms")]
    public GameObject grapplePlatformPrefab;
    [Range(0f, 1f)]
    public float grappleSpawnChance = 0.4f; // Увеличено
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
        
        // НОВАЯ УЛУЧШЕННАЯ ГЕНЕРАЦИЯ ВЫСОТЫ ДЛЯ ПАРКУРА
        SetParkourFriendlyBuildingHeights();
        
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
    /// НОВЫЙ МЕТОД: Устанавливает высоты зданий оптимизированные для паркура
    /// Создает кластеры зданий разной высоты для интересных маршрутов
    /// </summary>
    private void SetParkourFriendlyBuildingHeights()
    {
        foreach (var lot in _lots)
        {
            if (lot.IsPark)
            {
                lot.Height = _blockHeight + _blockHeight / 3;
                continue;
            }
            
            // Базовая высота
            float height = (float)_rand.NextDouble() * (maxBuildHeight - minBuildHeight) + minBuildHeight;
            
            // Шанс создать действительно высокое здание
            if (_rand.NextDouble() < tallBuildingChance)
            {
                height = maxBuildHeight * (0.7f + (float)_rand.NextDouble() * 0.3f);
            }
            else
            {
                // Для обычных зданий предпочитаем средние высоты
                height *= 0.6f;
            }
            
            // Здания ближе к центру карты выше
            float distanceFromCenter = Mathf.Max(
                Mathf.Abs(lot.Nodes[0].X),
                Mathf.Abs(lot.Nodes[0].Y)
            );
            float centerFactor = 1f - (distanceFromCenter / MapSize);
            centerFactor = Mathf.Clamp01(centerFactor);
            
            height *= (0.5f + centerFactor * 0.5f);
            
            // Создаем вариацию высоты для паркура
            if (createClusteredHeights)
            {
                // Группируем здания: каждое 3-е здание получает бонус или штраф
                int buildingIndex = _lots.IndexOf(lot);
                if (buildingIndex % 3 == 0)
                {
                    height *= 1.5f; // Высокое здание
                }
                else if (buildingIndex % 3 == 1)
                {
                    height *= 0.6f; // Низкое здание
                }
                // buildingIndex % 3 == 2 остается средней высоты
            }
            
            // Минимальная высота
            if (height < minBuildHeight)
            {
                height = minBuildHeight;
            }
            
            // Максимальная высота
            if (height > maxBuildHeight)
            {
                height = maxBuildHeight;
            }
            
            lot.Height = height;
        }
        
        Debug.Log($"Building heights optimized for parkour gameplay");
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
        
        // === ГЕНЕРАЦИЯ ПАРКУР ЭЛЕМЕНТОВ - НОВОЕ! ===
        GenerateParkourElements();
        
        // Existing platforms
        GenerateGrapplePlatforms();
    }
    
    /// <summary>
    /// НОВЫЙ МЕТОД: Генерирует все паркур-элементы используя улучшенный ParkourElementsGeneratorV2
    /// Версия 2.0 включает проверку коллизий и умное размещение элементов
    /// </summary>
    private void GenerateParkourElements()
    {
        var parkourContainer = new GameObject
        {
            name = "===== PARKOUR ELEMENTS V2 ====="
        };
        
        var parkourGen = new ParkourElementsGenerator(
            _lots,
            _roadGraph,
            _rand,
            mapScale,
            bridgePrefab,
            ziplinePrefab,
            wallRunSurfacePrefab
        );
        
        parkourGen.GenerateAllParkourElements(parkourContainer);
        
        Debug.Log("All parkour elements generated with collision detection!");
    }
    
    private void GenerateGrapplePlatforms()
    {
        if (grapplePlatformPrefab == null) return;
        
        var grappleContainer = new GameObject
        {
            name = "Grapple Platform Container"
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
    
        Debug.Log($"{spawnedCount} grappling platforms spawned");
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