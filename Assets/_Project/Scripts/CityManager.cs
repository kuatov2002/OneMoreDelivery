using GameCreator.Runtime.Characters;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    [SerializeField] private CityGenerator cityGenerator;
    [SerializeField] private Character character;
    
    [Header("Character Positioning")]
    [SerializeField] private float characterHeightOffset = 2f;
    
    private void Start()
    {
        if (cityGenerator != null)
        {
            // Подписываемся на событие завершения генерации
            cityGenerator.OnCityGenerationComplete += OnCityReady;
        }
    }

    private void OnDestroy()
    {
        if (cityGenerator != null)
        {
            // Отписываемся от события
            cityGenerator.OnCityGenerationComplete -= OnCityReady;
        }
    }

    private void OnCityReady()
    {
        PlaceCharacterInCityCenter();
        Debug.Log("City generation complete! Character placed.");
        
        character.Motion.LinearSpeed = 50;
    }

    private void PlaceCharacterInCityCenter()
    {
        if (character == null) return;
        
        Vector3 cityCenter = new Vector3(0, characterHeightOffset, 0);
        character.transform.position = cityCenter;
        character.transform.rotation = Quaternion.identity;
    }
}