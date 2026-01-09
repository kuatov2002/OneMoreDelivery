using System;
using BlockGeneration;
using UnityEngine;
using UnityEngine.SceneManagement;
using Block = BlockGeneration.Block;

/// <summary>
/// Main manager that coordinates city gameplay systems.
/// Delegates specific responsibilities to specialized services.
/// </summary>
public class CityManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CityGenerator cityGenerator;
    [SerializeField] private GameObject character;
 
    [Header("City Configuration")]
    [SerializeField] private int mapSize = 100;
    
    [Header("Delivery Configuration")]
    [SerializeField] private DeliveryPointMarker startDeliveryMarker;
    [SerializeField] private DeliveryPointMarker endDeliveryMarker;
    [SerializeField] private float deliveryPointHeight = 1f;
    [SerializeField] private float targetDistanceBetweenPoints = 600f;
    [SerializeField] private float offsetFromBuilding = 3f;
    
    [Header("Character Settings")]
    [SerializeField] private float characterHeightOffset = 2f;
    
    [Header("Game Time Configuration")]
    [SerializeField] private float startHour = 9f;
    [SerializeField] private float endHour = 18f;
    [SerializeField] private float gameMinutesPerRealSecond = 1f;
    
    [Header("Win Condition")]
    [SerializeField] private int deliveriesRequiredToWin = 3;
    
    private GameTimeSystem _timeSystem;
    private DeliveryPointService _deliveryService;
    private DeliveryGameController _gameController;
    
    // Public properties for backward compatibility with existing UI and systems
    public GameObject StartDeliveryPoint => _deliveryService?.StartDeliveryPoint;
    public GameObject EndDeliveryPoint => _deliveryService?.EndDeliveryPoint;
    public int CurrentHour => _timeSystem?.CurrentHour ?? 0;
    public int CurrentMinute => _timeSystem?.CurrentMinute ?? 0;
    public int CurrentSecond
    {
        get
        {
            if (_timeSystem == null) return 0;
            float totalMinutes = _timeSystem.CurrentTimeInHours * 60f;
            float fractionalMinute = totalMinutes - Mathf.Floor(totalMinutes);
            return Mathf.FloorToInt(fractionalMinute * 60f);
        }
    }
    public float CurrentTimeInHours => _timeSystem?.CurrentTimeInHours ?? 0;
    public bool IsWorkingHours => _timeSystem?.IsWorkingHours ?? false;
    
    // Public access to systems
    public GameTimeSystem TimeSystem => _timeSystem;
    
    private void Start()
    {
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete += OnCityReady;
        }
        ValidateDependencies();
        InitializeSystems();
        cityGenerator.Generate((int)(mapSize * Math.Pow(1.1, RunData.CurrentDay)));
    }
    
    private void Update()
    {
        _timeSystem?.Update(Time.deltaTime);
    }
    
    private void OnDestroy()
    {
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete -= OnCityReady;
        }
        if (_gameController != null)
        {
            _gameController.OnDeliveryCountChanged -= HandleDeliveryCountChanged;
            _gameController.OnWinConditionMet -= HandleWinCondition;
        }
    }
    
    private void ValidateDependencies()
    {
        if (cityGenerator == null)
        {
            Debug.LogError("CityManager: CityGenerator not assigned");
        }
        
        if (character == null)
        {
            Debug.LogError("CityManager: Character not assigned");
        }
        
        if (startDeliveryMarker == null || endDeliveryMarker == null)
        {
            Debug.LogError("CityManager: Delivery markers not assigned");
        }
    }
    
    private void InitializeSystems()
    {
        _timeSystem = new GameTimeSystem(startHour, endHour, gameMinutesPerRealSecond);
        
        _deliveryService = new DeliveryPointService(
            cityGenerator,
            startDeliveryMarker,
            endDeliveryMarker,
            deliveryPointHeight,
            targetDistanceBetweenPoints,
            offsetFromBuilding
        );
        
        _gameController = new DeliveryGameController(
            _deliveryService,
            _timeSystem,
            deliveriesRequiredToWin
        );
        
        _gameController.OnDeliveryCountChanged += HandleDeliveryCountChanged;
        _gameController.OnWinConditionMet += HandleWinCondition;
    }
    
    private void HandleDeliveryCountChanged(int current)
    {
        Debug.Log($"Delivery progress: {current}/{deliveriesRequiredToWin}");
        // Update UI here if needed
    }
    
    private void HandleWinCondition(int totalDeliveries)
    {
        Time.timeScale = 1f;
        RunData.CurrentDay++;
        SceneManager.LoadSceneAsync("Hub");
    }
    
    private void OnCityReady()
    {
        PlaceCharacterInCityCenter();
        _gameController.StartNewDelivery();
        Debug.Log("City ready, game started");
    }
    
    private void PlaceCharacterInCityCenter()
    {
        if (character == null) return;
        
        Vector3 cityCenter = new Vector3(0, characterHeightOffset, 0);
        character.transform.position = cityCenter;
        character.transform.rotation = Quaternion.identity;
    }
}

/// <summary>
/// Coordinates delivery gameplay flow.
/// Uses composition instead of inheritance for better flexibility.
/// </summary>
public class DeliveryGameController
{
    private readonly DeliveryPointService _deliveryService;
    private readonly BuffSelectionService _buffSelectionService;
    private readonly GameTimeSystem _timeSystem;
    
    private bool _isWaitingForSelection;
    private int _completedDeliveries;
    private readonly int _deliveriesRequiredToWin;
    
    // Event for win condition
    public event Action<int> OnWinConditionMet; // Passes total deliveries completed
    public event Action<int> OnDeliveryCountChanged; // Current count, required count
    
    public int CompletedDeliveries => _completedDeliveries;
    public int DeliveriesRequiredToWin => _deliveriesRequiredToWin;
    
    public DeliveryGameController(
        DeliveryPointService deliveryService,
        GameTimeSystem timeSystem,
        int deliveriesRequiredToWin)
    {
        _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
        _timeSystem = timeSystem ?? throw new ArgumentNullException(nameof(timeSystem));
        _deliveriesRequiredToWin = deliveriesRequiredToWin;
        _completedDeliveries = 0;
    }
    
    public void StartNewDelivery()
    {
        if (!_timeSystem.IsWorkingHours)
        {
            Debug.Log("Cannot start delivery: outside working hours");
            return;
        }
        
        _deliveryService.SpawnNewDeliveryPoints(OnDeliveryPickedUp, OnDeliveryCompleted);
    }
    
    private void OnDeliveryPickedUp()
    {
        Debug.Log("Delivery picked up");
    }
    
    private void OnDeliveryCompleted()
    {
        _completedDeliveries++;
        Debug.Log($"Delivery completed! Total: {_completedDeliveries}/{_deliveriesRequiredToWin}");
        
        OnDeliveryCountChanged?.Invoke(_completedDeliveries);
        if (_completedDeliveries >= _deliveriesRequiredToWin)
        {
            HandleWinCondition();
        }
        if (_timeSystem.IsWorkingHours)
        {
            StartNewDelivery();
        }
    }
    
    private void HandleWinCondition()
    {
        Debug.Log($"WIN! Completed {_completedDeliveries} deliveries!");
        _timeSystem.Pause();
        Time.timeScale = 0f;
        
        OnWinConditionMet?.Invoke(_completedDeliveries);
        
        // Game is now in win state - external systems should handle UI/transition
    }
}

/// <summary>
/// Manages game time progression with pause capability.
/// Separated from UI and delivery logic for better testability.
/// </summary>
public class GameTimeSystem
{
    private float _currentTimeInMinutes;
    private readonly float _startHour;
    private readonly float _endHour;
    private readonly float _gameMinutesPerRealSecond;
    
    private bool _isPaused;
    
    public float CurrentTimeInHours => _currentTimeInMinutes / 60f;
    public int CurrentHour => Mathf.FloorToInt(_currentTimeInMinutes / 60f);
    public int CurrentMinute => Mathf.FloorToInt(_currentTimeInMinutes % 60f);
    public bool IsWorkingHours => CurrentTimeInHours >= _startHour && CurrentTimeInHours < _endHour;
    public bool IsPaused => _isPaused;
    
    public event Action OnWorkDayEnded;
    
    public GameTimeSystem(float startHour, float endHour, float gameMinutesPerRealSecond)
    {
        _startHour = startHour;
        _endHour = endHour;
        _gameMinutesPerRealSecond = gameMinutesPerRealSecond;
        _currentTimeInMinutes = startHour * 60f;
    }
    
    public void Update(float deltaTime)
    {
        if (_isPaused) return;
        
        _currentTimeInMinutes += _gameMinutesPerRealSecond * deltaTime;
        
        if (CurrentTimeInHours >= _endHour)
        {
            _currentTimeInMinutes = _endHour * 60f;
            _isPaused = true;
            OnWorkDayEnded?.Invoke();
        }
    }
    
    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;
    public void ResetToStart() => _currentTimeInMinutes = _startHour * 60f;
}

/// <summary>
/// Service responsible for spawning and positioning delivery points in the city.
/// Handles placement logic, validation, and cleanup.
/// </summary>
public class DeliveryPointService
{
    private readonly CityGenerator _cityGenerator;
    private readonly DeliveryPointMarker _startMarkerPrefab;
    private readonly DeliveryPointMarker _endMarkerPrefab;
    private readonly float _deliveryPointHeight;
    private readonly float _targetDistanceBetweenPoints;
    private readonly float _offsetFromBuilding;
    private readonly int _maxAttempts = 30;
    
    private DeliveryPointMarker _activeStartMarker;
    private DeliveryPointMarker _activeEndMarker;
    
    public GameObject StartDeliveryPoint
    {
        get
        {
            // Unity null check handles destroyed objects properly
            if (_activeStartMarker == null) return null;
            return _activeStartMarker.gameObject;
        }
    }
    
    public GameObject EndDeliveryPoint
    {
        get
        {
            if (_activeEndMarker == null) return null;
            return _activeEndMarker.gameObject;
        }
    }
    
    public DeliveryPointService(
        CityGenerator cityGenerator,
        DeliveryPointMarker startMarkerPrefab,
        DeliveryPointMarker endMarkerPrefab,
        float deliveryPointHeight,
        float targetDistanceBetweenPoints,
        float offsetFromBuilding)
    {
        _cityGenerator = cityGenerator ?? throw new ArgumentNullException(nameof(cityGenerator));
        _startMarkerPrefab = startMarkerPrefab ?? throw new ArgumentNullException(nameof(startMarkerPrefab));
        _endMarkerPrefab = endMarkerPrefab ?? throw new ArgumentNullException(nameof(endMarkerPrefab));
        _deliveryPointHeight = deliveryPointHeight;
        _targetDistanceBetweenPoints = targetDistanceBetweenPoints;
        _offsetFromBuilding = offsetFromBuilding;
    }
    
    public void SpawnNewDeliveryPoints(Action onPickedUp, Action onCompleted)
    {
        CleanupExistingPoints();
        
        float minDistance = _targetDistanceBetweenPoints * 0.9f;
        float maxDistance = _targetDistanceBetweenPoints * 1.1f;
        
        Vector3 startPos = GetPositionNearBuilding();
        Vector3 endPos = GetPositionNearBuilding();
        
        int attempts = 0;
        float distance = Vector3.Distance(startPos, endPos);
        
        while ((distance < minDistance || distance > maxDistance) && attempts < _maxAttempts)
        {
            endPos = GetPositionNearBuilding();
            distance = Vector3.Distance(startPos, endPos);
            attempts++;
        }
        
        _activeStartMarker = UnityEngine.Object.Instantiate(_startMarkerPrefab, startPos, Quaternion.identity);
        _activeStartMarker.onDeliveryPickedUp.AddListener(() => onPickedUp?.Invoke());
        
        _activeEndMarker = UnityEngine.Object.Instantiate(_endMarkerPrefab, endPos, Quaternion.identity);
        _activeEndMarker.onDeliveryCompleted.AddListener(() => onCompleted?.Invoke());
        
        _activeStartMarker.gameObject.SetActive(true);
        _activeEndMarker.gameObject.SetActive(true);
        
        Debug.Log($"Delivery points spawned: Start at {startPos}, End at {endPos}, Distance: {distance:F2}");
    }
    
    private void CleanupExistingPoints()
    {
        if (_activeStartMarker != null)
        {
            UnityEngine.Object.Destroy(_activeStartMarker.gameObject);
            _activeStartMarker = null;
        }
        
        if (_activeEndMarker != null)
        {
            UnityEngine.Object.Destroy(_activeEndMarker.gameObject);
            _activeEndMarker = null;
        }
    }
    
    private Vector3 GetPositionNearBuilding()
    {
        var lots = _cityGenerator.GetLots();
        
        if (lots == null || lots.Count == 0)
        {
            Debug.LogWarning("No lots found, using random position on map");
            return GetSafeRandomPositionOnMap();
        }
        
        int globalAttempts = 0;
        int maxGlobalAttempts = _maxAttempts * 3;
        
        while (globalAttempts < maxGlobalAttempts)
        {
            Block selectedLot = null;
            int lotSelectionAttempts = 0;
            
            while ((selectedLot == null || selectedLot.IsPark) && lotSelectionAttempts < 50)
            {
                int randomLotIndex = UnityEngine.Random.Range(0, lots.Count);
                selectedLot = lots[randomLotIndex];
                lotSelectionAttempts++;
            }
            
            if (selectedLot == null || selectedLot.Nodes == null || selectedLot.Nodes.Count < 2)
            {
                globalAttempts++;
                continue;
            }
            
            int edgeIndex = UnityEngine.Random.Range(0, selectedLot.Nodes.Count);
            BlockNode node1 = selectedLot.Nodes[edgeIndex];
            BlockNode node2 = selectedLot.Nodes[(edgeIndex + 1) % selectedLot.Nodes.Count];
            
            Vector2 edgeMidpoint = new Vector2(
                (node1.X + node2.X) / 2f,
                (node1.Y + node2.Y) / 2f
            );
            
            Vector2 edgeVector = new Vector2(node2.X - node1.X, node2.Y - node1.Y);
            Vector2 outwardNormal = new Vector2(-edgeVector.y, edgeVector.x).normalized;
            
            Vector2 buildingCenter = Vector2.zero;
            foreach (var node in selectedLot.Nodes)
            {
                buildingCenter += new Vector2(node.X, node.Y);
            }
            buildingCenter /= selectedLot.Nodes.Count;
            
            Vector2 toCenter = buildingCenter - edgeMidpoint;
            if (Vector2.Dot(outwardNormal, toCenter) > 0)
            {
                outwardNormal = -outwardNormal;
            }
            
            Vector2 deliveryPoint2D = edgeMidpoint + outwardNormal * _offsetFromBuilding;
            
            bool isInsideAnyBuilding = false;
            foreach (var lot in lots)
            {
                if (lot.IsPark) continue;
                
                if (IsPointInsideBlock(deliveryPoint2D, lot))
                {
                    isInsideAnyBuilding = true;
                    break;
                }
            }
            
            if (!isInsideAnyBuilding)
            {
                Vector3 scaledPosition = new Vector3(
                    deliveryPoint2D.x * _cityGenerator.mapScale,
                    _deliveryPointHeight,
                    deliveryPoint2D.y * _cityGenerator.mapScale
                );
                
                if (IsPositionWithinMapBounds(scaledPosition))
                {
                    return scaledPosition;
                }
            }
            
            globalAttempts++;
        }
        
        Debug.LogWarning("Could not find valid position near buildings, using safe random position");
        return GetSafeRandomPositionOnMap();
    }
    
    private bool IsPositionWithinMapBounds(Vector3 position)
    {
        float halfMapSize = _cityGenerator.MapSize / 2f;
        float safetyOffset = _cityGenerator.MapSize * 0.1f;
        
        float minBound = -halfMapSize + safetyOffset;
        float maxBound = halfMapSize - safetyOffset;
        
        float unscaledX = position.x / _cityGenerator.mapScale;
        float unscaledZ = position.z / _cityGenerator.mapScale;
        
        bool isWithinBounds = unscaledX >= minBound && unscaledX <= maxBound &&
                              unscaledZ >= minBound && unscaledZ <= maxBound;
        
        if (!isWithinBounds)
        {
            Debug.LogWarning($"Position ({unscaledX:F2}, {unscaledZ:F2}) is outside safe map bounds [{minBound:F2}, {maxBound:F2}]");
        }
        
        return isWithinBounds;
    }
    
    private bool IsPointInsideBlock(Vector2 point, Block block)
    {
        if (block.Nodes == null || block.Nodes.Count < 3)
            return false;

        int intersectionCount = 0;
        int nodeCount = block.Nodes.Count;

        for (int i = 0; i < nodeCount; i++)
        {
            BlockNode node1 = block.Nodes[i];
            BlockNode node2 = block.Nodes[(i + 1) % nodeCount];

            Vector2 v1 = new Vector2(node1.X, node1.Y);
            Vector2 v2 = new Vector2(node2.X, node2.Y);

            if ((v1.y > point.y) != (v2.y > point.y))
            {
                float intersectionX = (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x;
                
                if (point.x < intersectionX)
                {
                    intersectionCount++;
                }
            }
        }

        return (intersectionCount % 2) == 1;
    }
    
    private Vector3 GetSafeRandomPositionOnMap()
    {
        float halfMapSize = _cityGenerator.MapSize / 2f;
        float safetyOffset = _cityGenerator.MapSize * 0.1f;
        
        float minBound = -halfMapSize + safetyOffset;
        float maxBound = halfMapSize - safetyOffset;
        
        float x = UnityEngine.Random.Range(minBound, maxBound);
        float z = UnityEngine.Random.Range(minBound, maxBound);
        
        Vector3 position = new Vector3(
            x * _cityGenerator.mapScale,
            _deliveryPointHeight,
            z * _cityGenerator.mapScale
        );
        
        return position;
    }
}