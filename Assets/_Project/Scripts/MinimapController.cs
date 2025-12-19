using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Расширенный контроллер миникарты с отображением позиции игрока
/// Прикрепите этот скрипт к GameObject с Canvas или к отдельному GameObject
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

    [Header("Settings")]
    [Tooltip("Размер карты (должен совпадать с mapSize в CityGenerator)")]
    private int _mapSize = 300;
    
    [Tooltip("Масштаб города (должен совпадать с mapScale в CityGenerator)")]
    private float _cityScale = 1f;
    
    [Tooltip("Обновлять позицию игрока каждый кадр")]
    public bool updatePlayerPosition = true;
    
    [Tooltip("Сглаживание движения маркера")]
    [Range(0f, 1f)]
    public float markerSmoothness = 0.1f;
    
    [Tooltip("Масштаб миникарты")]
    [Range(0.5f, 2f)]
    public float minimapScale = 1f;

    [Header("Player Marker Appearance")]
    [Tooltip("Цвет маркера игрока")]
    public Color playerMarkerColor = Color.red;
    
    [Tooltip("Размер маркера игрока")]
    public float playerMarkerSize = 10f;
    
    [Tooltip("Вращать маркер в зависимости от направления игрока")]
    public bool rotateMarker = true;

    private RectTransform minimapRect;
    private RectTransform playerMarkerRect;
    private Vector2 targetMarkerPosition;

    void Start()
    {
        InitializeMinimap();
        CreatePlayerMarker();
        SyncCitySettings();
    }

    void Update()
    {
        if (updatePlayerPosition && playerTransform != null && playerMarkerRect != null)
        {
            UpdatePlayerMarkerPosition();
        }
    }

    private void SyncCitySettings()
    {
        // Автоматически синхронизируем настройки с CityGenerator
        if (cityGenerator != null)
        {
            _mapSize = cityGenerator.mapSize;
            _cityScale = cityGenerator.mapScale;
            
            Debug.Log($"MinimapController synced with CityGenerator: mapSize={_mapSize}, cityScale={_cityScale}");
        }
        else
        {
            Debug.LogWarning("MinimapController: CityGenerator is not assigned. Using manual settings.");
        }
    }

    private void InitializeMinimap()
    {
        if (minimapImage != null)
        {
            minimapRect = minimapImage.GetComponent<RectTransform>();
            
            // Применяем масштаб
            minimapRect.localScale = Vector3.one * minimapScale;
        }
        else
        {
            Debug.LogWarning("MinimapController: Minimap Image is not assigned!");
        }
    }

    private void CreatePlayerMarker()
    {
        if (playerMarker == null && minimapImage != null)
        {
            // Создаем маркер автоматически
            GameObject markerObj = new GameObject("PlayerMarker");
            markerObj.transform.SetParent(minimapImage.transform);
            
            playerMarker = markerObj.AddComponent<Image>();
            playerMarkerRect = markerObj.GetComponent<RectTransform>();
            
            // Создаем круглый маркер
            Texture2D markerTexture = CreateCircleTexture(32, playerMarkerColor);
            Sprite markerSprite = Sprite.Create(
                markerTexture,
                new Rect(0, 0, markerTexture.width, markerTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            playerMarker.sprite = markerSprite;
            playerMarkerRect.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
            
            Debug.Log("Player marker created automatically");
        }
        else if (playerMarker != null)
        {
            playerMarkerRect = playerMarker.GetComponent<RectTransform>();
            playerMarker.color = playerMarkerColor;
            playerMarkerRect.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
        }
    }

    private void UpdatePlayerMarkerPosition()
    {
        if (minimapRect == null || playerMarkerRect == null) return;

        // Получаем позицию игрока в мировых координатах
        Vector3 worldPos = playerTransform.position;
        
        // ВАЖНО: Учитываем масштаб города
        // Делим координаты на cityScale, чтобы получить координаты в исходной системе карты
        float adjustedX = worldPos.x / _cityScale;
        float adjustedZ = worldPos.z / _cityScale;
        
        // Преобразуем мировую позицию в локальную позицию на миникарте
        // Предполагаем, что карта центрирована в (0, 0)
        float normalizedX = adjustedX / (_mapSize * 2f);
        float normalizedZ = adjustedZ / (_mapSize * 2f);
        
        // Преобразуем в локальные координаты RectTransform
        float localX = normalizedX * minimapRect.rect.width;
        float localY = normalizedZ * minimapRect.rect.height;
        
        targetMarkerPosition = new Vector2(localX, localY);
        
        // Применяем сглаживание
        if (markerSmoothness > 0)
        {
            playerMarkerRect.anchoredPosition = Vector2.Lerp(
                playerMarkerRect.anchoredPosition,
                targetMarkerPosition,
                1f - markerSmoothness
            );
        }
        else
        {
            playerMarkerRect.anchoredPosition = targetMarkerPosition;
        }
        
        // Вращаем маркер в зависимости от направления игрока
        if (rotateMarker)
        {
            float angle = playerTransform.rotation.eulerAngles.y;
            playerMarkerRect.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    /// <summary>
    /// Обновить текстуру миникарты
    /// </summary>
    public void RefreshMinimap()
    {
        if (cityGenerator != null)
        {
            cityGenerator.UpdateMinimap();
            Debug.Log("Minimap refresh requested");
        }
        else
        {
            Debug.LogWarning("MinimapController: CityGenerator is not assigned!");
        }
    }

    /// <summary>
    /// Синхронизировать настройки с CityGenerator
    /// Вызовите этот метод, если изменили mapSize или mapScale в CityGenerator во время игры
    /// </summary>
    public void RefreshCitySettings()
    {
        SyncCitySettings();
    }

    /// <summary>
    /// Переключить видимость миникарты
    /// </summary>
    public void ToggleMinimapVisibility()
    {
        if (minimapImage != null)
        {
            minimapImage.gameObject.SetActive(!minimapImage.gameObject.activeSelf);
        }
    }

    /// <summary>
    /// Установить масштаб миникарты
    /// </summary>
    public void SetMinimapScale(float scale)
    {
        minimapScale = Mathf.Clamp(scale, 0.5f, 2f);
        if (minimapRect != null)
        {
            minimapRect.localScale = Vector3.one * minimapScale;
        }
    }

    /// <summary>
    /// Установить цвет маркера игрока
    /// </summary>
    public void SetPlayerMarkerColor(Color color)
    {
        playerMarkerColor = color;
        if (playerMarker != null)
        {
            playerMarker.color = color;
        }
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
                
                if (distance <= radius)
                {
                    // Smooth edge
                    float alpha = Mathf.Clamp01(1f - (distance - radius + 1f));
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

    // Вызывается из UI кнопок (если нужно)
    public void OnZoomInButton()
    {
        SetMinimapScale(minimapScale + 0.1f);
    }

    public void OnZoomOutButton()
    {
        SetMinimapScale(minimapScale - 0.1f);
    }

    void OnDrawGizmos()
    {
        // Визуализация для отладки
        if (playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, 2f);
        }
    }
}