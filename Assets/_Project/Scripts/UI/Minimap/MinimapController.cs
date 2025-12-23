using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlockGeneration;
using GraphModel;
using Services;

/// <summary>
/// Расширенный контроллер миникарты с отображением позиции игрока и точек доставки
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UI Image для отображения миникарты")]
    public Image minimapImage;
    
    [Tooltip("UI Image для маркера позиции игрока")]
    public Image playerMarker;
    
    [Tooltip("Transform игрока для отслеживания позиции")]
    public Transform playerTransform;
    
    [Tooltip("CityGenerator для доступа к данным карты")]
    public CityGenerator cityGenerator;
    
    [Tooltip("CityManager для доступа к точкам доставки")]
    public CityManager cityManager;

    [Header("Minimap Settings")]
    [Tooltip("Разрешение миникарты")]
    [Range(256, 2048)]
    public int minimapResolution = 512;
    
    [Tooltip("Обновлять позицию игрока каждый кадр")]
    public bool updatePlayerPosition = true;
    
    [Tooltip("Сглаживание движения маркера")]
    [Range(0f, 1f)]
    public float markerSmoothness = 0.1f;
    
    [Tooltip("Масштаб миникарты")]
    [Range(0.5f, 2f)]
    public float minimapScale = 1f;

    [Header("Player Marker")]
    [Tooltip("Цвет маркера игрока")]
    public Color playerMarkerColor = Color.blue;
    
    [Tooltip("Размер маркера игрока")]
    public float playerMarkerSize = 12f;
    
    [Tooltip("Вращать маркер в зависимости от направления игрока")]
    public bool rotateMarker = true;

    [Header("Delivery Points Markers")]
    [Tooltip("Показывать точки доставки на миникарте")]
    public bool showDeliveryPoints = true;
    
    [Tooltip("UI Image для маркера начальной точки")]
    public Image startPointMarker;
    
    [Tooltip("UI Image для маркера конечной точки")]
    public Image endPointMarker;
    
    [Tooltip("Размер маркеров точек доставки")]
    public float deliveryPointMarkerSize = 15f;
    
    [Tooltip("Цвет маркера начальной точки")]
    public Color startPointColor = Color.green;
    
    [Tooltip("Цвет маркера конечной точки")]
    public Color endPointColor = Color.red;

    private RectTransform minimapRect;
    private RectTransform playerMarkerRect;
    private RectTransform startPointMarkerRect;
    private RectTransform endPointMarkerRect;
    
    private Vector2 targetPlayerMarkerPosition;
    private int _mapSize = 300;
    private float _cityScale = 1f;
    
    private bool minimapGenerated = false;

    void Start()
    {
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete += OnCityReady;
        }
    }

    void OnDestroy()
    {
        if (cityGenerator != null)
        {
            cityGenerator.OnCityGenerationComplete -= OnCityReady;
        }
    }

    void Update()
    {
        if (updatePlayerPosition && playerTransform != null && playerMarkerRect != null)
        {
            UpdatePlayerMarkerPosition();
        }
        
        if (showDeliveryPoints && minimapGenerated)
        {
            UpdateDeliveryPointMarkers();
        }
    }

    private void OnCityReady()
    {
        SyncCitySettings();
        InitializeMinimap();
        GenerateMinimapTexture();
        CreatePlayerMarker();
        CreateDeliveryPointMarkers();
        minimapGenerated = true;
        
        Debug.Log("Minimap generated and initialized!");
    }

    private void SyncCitySettings()
    {
        if (cityGenerator != null)
        {
            _mapSize = cityGenerator.MapSize;
            _cityScale = cityGenerator.mapScale;
            
            Debug.Log($"MinimapController synced: mapSize={_mapSize}, cityScale={_cityScale}");
        }
    }

    private void InitializeMinimap()
    {
        if (minimapImage != null)
        {
            minimapRect = minimapImage.GetComponent<RectTransform>();
            minimapRect.localScale = Vector3.one * minimapScale;
        }
        else
        {
            Debug.LogWarning("MinimapController: Minimap Image is not assigned!");
        }
    }

    private void GenerateMinimapTexture()
    {
        if (minimapImage == null || cityGenerator == null)
        {
            Debug.LogWarning("Cannot generate minimap: missing references");
            return;
        }

        Debug.Log("Generating minimap texture...");
        
        // Получаем данные из CityGenerator
        Graph roadGraph = cityGenerator.GetRoadGraph();
        List<Block> blocks = cityGenerator.GetBlocks();
        List<Block> lots = cityGenerator.GetLots();
        
        // Генерируем текстуру миникарты
        Texture2D minimapTexture = MinimapService.GenerateMinimap(
            roadGraph, 
            blocks, 
            lots, 
            _mapSize, 
            minimapResolution
        );

        // Создаем спрайт из текстуры
        Sprite minimapSprite = Sprite.Create(
            minimapTexture,
            new Rect(0, 0, minimapTexture.width, minimapTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        // Применяем спрайт к Image
        minimapImage.sprite = minimapSprite;
        
        Debug.Log("Minimap texture generated successfully!");
    }

    private void CreatePlayerMarker()
    {
        if (playerMarker == null && minimapImage != null)
        {
            GameObject markerObj = new GameObject("PlayerMarker");
            markerObj.transform.SetParent(minimapImage.transform);
            
            playerMarker = markerObj.AddComponent<Image>();
            playerMarkerRect = markerObj.GetComponent<RectTransform>();
            
            Texture2D markerTexture = CreateArrowTexture(32, playerMarkerColor);
            Sprite markerSprite = Sprite.Create(
                markerTexture,
                new Rect(0, 0, markerTexture.width, markerTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            playerMarker.sprite = markerSprite;
            playerMarkerRect.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
            playerMarkerRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("Player marker created");
        }
        else if (playerMarker != null)
        {
            playerMarkerRect = playerMarker.GetComponent<RectTransform>();
            playerMarker.color = playerMarkerColor;
            playerMarkerRect.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
        }
    }

    private void CreateDeliveryPointMarkers()
    {
        if (!showDeliveryPoints || minimapImage == null) return;
        
        // Создаем маркер начальной точки
        if (startPointMarker == null)
        {
            GameObject startObj = new GameObject("StartPointMarker");
            startObj.transform.SetParent(minimapImage.transform);
            
            startPointMarker = startObj.AddComponent<Image>();
            startPointMarkerRect = startObj.GetComponent<RectTransform>();
            
            Texture2D startTexture = CreateCircleTexture(32, startPointColor);
            Sprite startSprite = Sprite.Create(
                startTexture,
                new Rect(0, 0, startTexture.width, startTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            startPointMarker.sprite = startSprite;
            startPointMarkerRect.sizeDelta = new Vector2(deliveryPointMarkerSize, deliveryPointMarkerSize);
            
            Debug.Log("Start point marker created");
        }
        else
        {
            startPointMarkerRect = startPointMarker.GetComponent<RectTransform>();
        }
        
        // Создаем маркер конечной точки
        if (endPointMarker == null)
        {
            GameObject endObj = new GameObject("EndPointMarker");
            endObj.transform.SetParent(minimapImage.transform);
            
            endPointMarker = endObj.AddComponent<Image>();
            endPointMarkerRect = endObj.GetComponent<RectTransform>();
            
            Texture2D endTexture = CreateCircleTexture(32, endPointColor);
            Sprite endSprite = Sprite.Create(
                endTexture,
                new Rect(0, 0, endTexture.width, endTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            endPointMarker.sprite = endSprite;
            endPointMarkerRect.sizeDelta = new Vector2(deliveryPointMarkerSize, deliveryPointMarkerSize);
            
            Debug.Log("End point marker created");
        }
        else
        {
            endPointMarkerRect = endPointMarker.GetComponent<RectTransform>();
        }
    }

    private void UpdatePlayerMarkerPosition()
    {
        if (minimapRect == null || playerMarkerRect == null) return;

        Vector3 worldPos = playerTransform.position;
        
        // Учитываем масштаб города
        float adjustedX = worldPos.x / _cityScale;
        float adjustedZ = worldPos.z / _cityScale;
        
        // Преобразуем в координаты миникарты
        float normalizedX = adjustedX / (_mapSize * 2f);
        float normalizedZ = adjustedZ / (_mapSize * 2f);
        
        float localX = normalizedX * minimapRect.rect.width;
        float localY = normalizedZ * minimapRect.rect.height;
        
        targetPlayerMarkerPosition = new Vector2(localX, localY);
        
        // Применяем сглаживание
        if (markerSmoothness > 0)
        {
            playerMarkerRect.anchoredPosition = Vector2.Lerp(
                playerMarkerRect.anchoredPosition,
                targetPlayerMarkerPosition,
                1f - markerSmoothness
            );
        }
        else
        {
            playerMarkerRect.anchoredPosition = targetPlayerMarkerPosition;
        }
        
        // Вращаем маркер
        if (rotateMarker)
        {
            float angle = playerTransform.rotation.eulerAngles.y;
            playerMarkerRect.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    private void UpdateDeliveryPointMarkers()
    {
        if (cityManager == null || minimapRect == null) return;
        
        // Обновляем позицию начальной точки
        if (cityManager.StartDeliveryPoint != null && startPointMarkerRect != null)
        {
            Vector3 startWorldPos = cityManager.StartDeliveryPoint.transform.position;
            Vector2 startLocalPos = WorldToMinimapPosition(startWorldPos);
            startPointMarkerRect.anchoredPosition = startLocalPos;
        }
        
        // Обновляем позицию конечной точки
        if (cityManager.EndDeliveryPoint != null && endPointMarkerRect != null)
        {
            Vector3 endWorldPos = cityManager.EndDeliveryPoint.transform.position;
            Vector2 endLocalPos = WorldToMinimapPosition(endWorldPos);
            endPointMarkerRect.anchoredPosition = endLocalPos;
        }
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        // Учитываем масштаб города
        float adjustedX = worldPos.x / _cityScale;
        float adjustedZ = worldPos.z / _cityScale;
        
        // Преобразуем в координаты миникарты
        float normalizedX = adjustedX / (_mapSize * 2f);
        float normalizedZ = adjustedZ / (_mapSize * 2f);
        
        float localX = normalizedX * minimapRect.rect.width;
        float localY = normalizedZ * minimapRect.rect.height;
        
        return new Vector2(localX, localY);
    }

    public void RefreshMinimap()
    {
        if (cityGenerator != null && minimapGenerated)
        {
            GenerateMinimapTexture();
            Debug.Log("Minimap refreshed");
        }
    }

    public void ToggleMinimapVisibility()
    {
        if (minimapImage != null)
        {
            minimapImage.gameObject.SetActive(!minimapImage.gameObject.activeSelf);
        }
    }

    public void SetMinimapScale(float scale)
    {
        minimapScale = Mathf.Clamp(scale, 0.5f, 2f);
        if (minimapRect != null)
        {
            minimapRect.localScale = Vector3.one * minimapScale;
        }
    }

    public void ToggleDeliveryPoints(bool show)
    {
        showDeliveryPoints = show;
        
        if (startPointMarker != null)
            startPointMarker.gameObject.SetActive(show);
            
        if (endPointMarker != null)
            endPointMarker.gameObject.SetActive(show);
    }

    private Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        int center = size / 2;
        float radius = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (distance <= radius - 1)
                {
                    texture.SetPixel(x, y, color);
                }
                else if (distance <= radius)
                {
                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }

    private Texture2D CreateArrowTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        int center = size / 2;
        
        // Очищаем текстуру
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }
        
        // Рисуем стрелку (треугольник)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Треугольник направлен вверх
                float normalizedY = (float)y / size;
                float normalizedX = (float)x / size;
                
                // Проверяем, находится ли точка внутри треугольника
                if (normalizedY > 0.2f && normalizedY < 0.8f)
                {
                    float width = (0.8f - normalizedY) * 0.6f;
                    if (Mathf.Abs(normalizedX - 0.5f) < width)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        texture.Apply();
        return texture;
    }

    public void OnZoomInButton()
    {
        SetMinimapScale(minimapScale + 0.1f);
    }

    public void OnZoomOutButton()
    {
        SetMinimapScale(minimapScale - 0.1f);
    }
}