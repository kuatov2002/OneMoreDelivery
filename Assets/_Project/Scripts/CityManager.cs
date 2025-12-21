using System.Collections.Generic;
using GameCreator.Runtime.Characters;
using UnityEngine;
using BlockGeneration;
using GameCreator.Runtime.Common;
using Block = BlockGeneration.Block;

public class CityManager : MonoBehaviour
{
    [SerializeField] private CityGenerator cityGenerator;
    [SerializeField] private Character character;
    
    [Header("Character Positioning")]
    [SerializeField] private float characterHeightOffset = 2f;
    
    [Header("Character Settings")]
    [SerializeField] private float characterSpeed = 50f;
    
    [Header("Delivery Points - Reference Existing Objects")]
    [SerializeField] private DeliveryPointMarker startDeliveryMarker;
    [SerializeField] private DeliveryPointMarker endDeliveryMarker;
    [SerializeField] private float deliveryPointHeight = 1f;
    [SerializeField] private float minDistanceBetweenPoints = 100f;
    [SerializeField] private int maxAttempts = 30;
    
    [Header("Delivery Point Positioning")]
    [SerializeField] private float offsetFromBuilding = 3f;
    
    [Header("Game Time System")]
    [SerializeField] private float startHour = 9f;
    [SerializeField] private float endHour = 18f;
    [SerializeField] private float gameMinutesPerRealSecond = 1f;
    [SerializeField] private bool loopTimeAfterEnd = true;
    
    [Header("Buff System")]
    [SerializeField] private string buffSelectionModuleName = "BuffSelection";
    [SerializeField] private bool pauseTimeOnBuffSelection = true;
    
    [Header("Available Choices")]
    [SerializeField] private List<GameCreatorChoice> availableChoices;
    
    private float _currentTimeInMinutes;
    private bool _isTimePaused = false;
    private bool _isBuffSelectionActive = false;
    
    private DeliveryPointMarker _activeStartMarker;
    private DeliveryPointMarker _activeEndMarker;
    private BuffSelectionModule _buffSelectionModule;
    private List<IBuffChoice> _currentSelectionChoices;
    
    public GameObject StartDeliveryPoint => _activeStartMarker != null ? _activeStartMarker.gameObject : null;
    public GameObject EndDeliveryPoint  => _activeEndMarker != null ? _activeEndMarker.gameObject : null;
    
    public float CurrentTimeInHours => _currentTimeInMinutes / 60f;
    public int CurrentHour => Mathf.FloorToInt(_currentTimeInMinutes / 60f);
    public int CurrentMinute => Mathf.FloorToInt(_currentTimeInMinutes % 60f);
    public int CurrentSecond => Mathf.FloorToInt((_currentTimeInMinutes % 1f) * 60f);
    public bool IsWorkingHours => CurrentTimeInHours >= startHour && CurrentTimeInHours < endHour;

    private void Start()
    {
        _currentTimeInMinutes = startHour * 60f;
        
        if (startDeliveryMarker == null || endDeliveryMarker == null)
        {
            Debug.LogError("CityManager: Delivery point markers are not assigned in the inspector!");
            return;
        }
        
        InitializeBuffSystem();
        
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete += OnCityReady;
        }
    }

    private void Update()
    {
        UpdateGameTime();
    }

    private void OnDestroy()
    {
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete -= OnCityReady;
        }
        
        UnsubscribeFromDeliveryEvents();
        UnsubscribeFromBuffEvents();
    }
    
    private void InitializeBuffSystem()
    {
        _buffSelectionModule = UIController.Instance.GetModule<BuffSelectionModule>(buffSelectionModuleName);
        
        if (_buffSelectionModule == null)
        {
            Debug.LogError($"BuffSelectionModule '{buffSelectionModuleName}' not found in UIController");
            return;
        }
        
        _buffSelectionModule.OnChoiceSelected += OnChoiceSelected;
        _buffSelectionModule.OnSelectionCompleted += OnSelectionCompleted;
    }
    
    private void UnsubscribeFromBuffEvents()
    {
        if (_buffSelectionModule != null)
        {
            _buffSelectionModule.OnChoiceSelected -= OnChoiceSelected;
            _buffSelectionModule.OnSelectionCompleted -= OnSelectionCompleted;
        }
    }
    
    private void UpdateGameTime()
    {
        if (_isTimePaused || _isBuffSelectionActive) return;
        
        _currentTimeInMinutes += gameMinutesPerRealSecond * Time.deltaTime;
        
        if (CurrentTimeInHours >= endHour)
        {
            if (loopTimeAfterEnd)
            {
                _currentTimeInMinutes = startHour * 60f;
                Debug.Log("Working day ended, time reset to start of day");
            }
            else
            {
                _currentTimeInMinutes = endHour * 60f;
                _isTimePaused = true;
                Debug.Log("Working day ended, time paused");
            }
        }
    }

    private void OnCityReady()
    {
        PlaceCharacterInCityCenter();
        SetupCharacterSpeed();
        PositionDeliveryPoints();
        Debug.Log("City generation complete! Character positioned and delivery points configured.");
    }

    private void PlaceCharacterInCityCenter()
    {
        if (character == null) return;
        
        Vector3 cityCenter = new Vector3(0, characterHeightOffset, 0);
        character.transform.position = cityCenter;
        character.transform.rotation = Quaternion.identity;
    }

    private void SetupCharacterSpeed()
    {
        if (character == null || character.Motion == null) return;
        
        character.Motion.LinearSpeed = characterSpeed;
        Debug.Log($"Character speed set to: {characterSpeed}");
    }
    
    private void UnsubscribeFromDeliveryEvents()
    {
        if (_activeStartMarker != null)
        {
            _activeStartMarker.onDeliveryPickedUp.RemoveListener(OnDeliveryPickedUp);
        }
        
        if (_activeEndMarker != null)
        {
            _activeEndMarker.onDeliveryCompleted.RemoveListener(OnDeliveryCompleted);
        }
    }
    
    private void OnDeliveryPickedUp()
    {
        Debug.Log("CityManager: Delivery picked up!");
    }

    private void OnDeliveryCompleted()
    {
        Debug.Log("CityManager: Delivery completed!");
        ShowBuffSelection();
    }
    
    private void ShowBuffSelection()
    {
        if (_buffSelectionModule == null)
        {
            Debug.LogError("Cannot show buff selection: module not initialized");
            ContinueAfterSelection();
            return;
        }
        
        if (availableChoices == null || availableChoices.Count == 0)
        {
            Debug.LogWarning("No choices available for selection");
            ContinueAfterSelection();
            return;
        }
        
        _currentSelectionChoices = GetRandomChoices(3); // Сохраняем выбранные варианты
        
        _isBuffSelectionActive = true;
        
        if (pauseTimeOnBuffSelection)
        {
            Time.timeScale = 0f;
        }
        
        _buffSelectionModule.ShowWithChoices(_currentSelectionChoices);
    }
    
    private void OnChoiceSelected(int index)
    {
        if (_currentSelectionChoices == null || index < 0 || index >= _currentSelectionChoices.Count)
        {
            Debug.LogError($"Invalid choice index: {index}");
            return;
        }
    
        var selectedChoice = _currentSelectionChoices[index]; // Используем список выбранных вариантов
        
        if (!(selectedChoice is GameCreatorChoice gameCreatorChoice))
        {
            Debug.LogError("Selected choice is not a GameCreatorChoice");
            return;
        }
        
        Debug.Log($"Choice selected: {gameCreatorChoice.name}");
    
        if (gameCreatorChoice.instructionToRun != null)
        {
            Args args = new Args(character.gameObject, gameObject);
            _ = gameCreatorChoice.instructionToRun.Run(args);
        }
        else
        {
            Debug.LogWarning($"No instruction assigned to choice: {gameCreatorChoice.name}");
        }
    }
    
    private void OnSelectionCompleted()
    {
        _isBuffSelectionActive = false;
        _currentSelectionChoices = null; // Очищаем после использования
        
        if (pauseTimeOnBuffSelection)
        {
            Time.timeScale = 1f;
        }
        
        ContinueAfterSelection();
    }
    
    private List<IBuffChoice> GetRandomChoices(int count)
    {
        var validChoices = new List<IBuffChoice>();
        
        foreach (var choice in availableChoices)
        {
            if (choice is IBuffChoice buffChoice)
            {
                validChoices.Add(buffChoice);
            }
        }
        
        if (validChoices.Count == 0)
        {
            Debug.LogError("No valid IBuffChoice implementations found in availableChoices");
            return new List<IBuffChoice>();
        }
        
        var shuffled = new List<IBuffChoice>(validChoices);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffled.Count);
            var temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        
        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }
    
    private void ContinueAfterSelection()
    {
        if (IsWorkingHours)
        {
            StartNewDelivery();
        }
        else
        {
            Debug.Log("Working hours ended.");
        }
    }

    private void StartNewDelivery()
    {
        if (startDeliveryMarker == null || endDeliveryMarker == null)
        {
            Debug.LogError("Cannot start new delivery: delivery markers are not assigned!");
            return;
        }
        
        PositionDeliveryPoints();
        Debug.Log("New delivery started - delivery points repositioned");
    }

    private void PositionDeliveryPoints()
    {
        if (cityGenerator == null)
        {
            Debug.LogWarning("CityGenerator is not assigned, cannot position delivery points");
            return;
        }
        
        if (startDeliveryMarker == null || endDeliveryMarker == null)
        {
            Debug.LogError("Delivery markers are not assigned in the inspector!");
            return;
        }
        
        if (_activeStartMarker != null)
        {
            Destroy(_activeStartMarker.gameObject);
        }
        if (_activeEndMarker != null)
        {
            Destroy(_activeEndMarker.gameObject);
        }
        
        Vector3 startPos = GetPositionNearBuilding();
        Vector3 endPos = GetPositionNearBuilding();
        
        int attempts = 0;
        while (Vector3.Distance(startPos, endPos) < minDistanceBetweenPoints && attempts < maxAttempts)
        {
            endPos = GetPositionNearBuilding();
            attempts++;
        }
        
        _activeStartMarker = Instantiate(startDeliveryMarker, startPos, Quaternion.identity);
        _activeStartMarker.onDeliveryPickedUp.AddListener(OnDeliveryPickedUp);
        
        _activeEndMarker = Instantiate(endDeliveryMarker, endPos, Quaternion.identity);
        _activeEndMarker.onDeliveryCompleted.AddListener(OnDeliveryCompleted);
        
        _activeStartMarker.gameObject.SetActive(true);
        _activeEndMarker.gameObject.SetActive(true);
        
        Debug.Log($"Delivery points positioned: Start at {startPos}, End at {endPos}, Distance: {Vector3.Distance(startPos, endPos):F2}");
    }

    private bool IsPositionWithinMapBounds(Vector3 position)
    {
        float halfMapSize = cityGenerator.mapSize / 2f;
        float safetyOffset = cityGenerator.mapSize * 0.1f;
        
        float minBound = -halfMapSize + safetyOffset;
        float maxBound = halfMapSize - safetyOffset;
        
        float unscaledX = position.x / cityGenerator.mapScale;
        float unscaledZ = position.z / cityGenerator.mapScale;
        
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

    private Vector3 GetPositionNearBuilding()
    {
        var lots = cityGenerator.GetLots();
        
        if (lots == null || lots.Count == 0)
        {
            Debug.LogWarning("No lots found, using random position on map");
            return GetRandomPositionOnMap();
        }
        
        int globalAttempts = 0;
        int maxGlobalAttempts = maxAttempts * 3;
        
        while (globalAttempts < maxGlobalAttempts)
        {
            Block selectedLot = null;
            int lotSelectionAttempts = 0;
            
            while ((selectedLot == null || selectedLot.IsPark) && lotSelectionAttempts < 50)
            {
                int randomLotIndex = Random.Range(0, lots.Count);
                selectedLot = lots[randomLotIndex];
                lotSelectionAttempts++;
            }
            
            if (selectedLot == null || selectedLot.Nodes == null || selectedLot.Nodes.Count < 2)
            {
                globalAttempts++;
                continue;
            }
            
            int edgeIndex = Random.Range(0, selectedLot.Nodes.Count);
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
            
            Vector2 deliveryPoint2D = edgeMidpoint + outwardNormal * offsetFromBuilding;
            
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
                    deliveryPoint2D.x * cityGenerator.mapScale,
                    deliveryPointHeight,
                    deliveryPoint2D.y * cityGenerator.mapScale
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

    private Vector3 GetSafeRandomPositionOnMap()
    {
        float halfMapSize = cityGenerator.mapSize / 2f;
        float safetyOffset = cityGenerator.mapSize * 0.1f;
        
        float minBound = -halfMapSize + safetyOffset;
        float maxBound = halfMapSize - safetyOffset;
        
        float x = Random.Range(minBound, maxBound);
        float z = Random.Range(minBound, maxBound);
        
        Vector3 position = new Vector3(
            x * cityGenerator.mapScale,
            deliveryPointHeight,
            z * cityGenerator.mapScale
        );
        
        return position;
    }

    private Vector3 GetRandomPositionOnMap()
    {
        return GetSafeRandomPositionOnMap();
    }
}