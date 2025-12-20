using UnityEngine;
using TMPro;

/// <summary>
/// Простой UI компонент для отображения игрового времени из CityManager.
/// Этот скрипт не содержит логики времени - он только визуализирует данные.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class GameTimeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CityManager cityManager;
    
    [Header("Display Settings")]
    [SerializeField] private bool use24HourFormat = true;
    [SerializeField] private bool showSeconds = false;
    
    private TextMeshProUGUI _timeText;

    private void Awake()
    {
        // Получаем компонент TextMeshProUGUI на этом же объекте
        _timeText = GetComponent<TextMeshProUGUI>();
        
        // Если CityManager не назначен в инспекторе, пытаемся найти его автоматически
        if (cityManager == null)
        {
            cityManager = FindObjectOfType<CityManager>();
            
            if (cityManager == null)
            {
                Debug.LogError("GameTimeUI: CityManager not found in scene! Please assign it in the inspector.");
                enabled = false;
                return;
            }
        }
    }

    private void Update()
    {
        // Каждый кадр обновляем отображение времени
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Обновляет текст UI на основе текущего времени из CityManager
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (cityManager == null) return;
        
        // Получаем время из CityManager - он является единственным источником правды о времени
        int hours = cityManager.CurrentHour;
        int minutes = cityManager.CurrentMinute;
        int seconds = cityManager.CurrentSecond;
        
        // Форматируем строку в зависимости от настроек
        string timeString;
        
        if (use24HourFormat)
        {
            // 24-часовой формат (например, 09:45 или 09:45:30)
            if (showSeconds)
            {
                timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
            }
            else
            {
                timeString = string.Format("{0:D2}:{1:D2}", hours, minutes);
            }
        }
        else
        {
            // 12-часовой формат с AM/PM (например, 9:45 AM или 9:45:30 AM)
            int displayHours = hours > 12 ? hours - 12 : hours;
            if (displayHours == 0) displayHours = 12;
            
            string amPm = hours >= 12 ? "PM" : "AM";
            
            if (showSeconds)
            {
                timeString = string.Format("{0}:{1:D2}:{2:D2} {3}", displayHours, minutes, seconds, amPm);
            }
            else
            {
                timeString = string.Format("{0}:{1:D2} {2}", displayHours, minutes, amPm);
            }
        }
        
        // Применяем префикс, если он указан
        _timeText.SetText(timeString);
    }
    
    /// <summary>
    /// Позволяет изменить ссылку на CityManager во время игры, если потребуется
    /// </summary>
    public void SetCityManager(CityManager manager)
    {
        cityManager = manager;
    }
}