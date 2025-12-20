using UnityEngine;

public class DeliveryPointMarker : MonoBehaviour
{
    [Header("Визуализация")]
    public Color markerColor = Color.green;
    public float markerSize = 2f;
    public bool showLabel = true;
    
    [Header("Анимация")]
    public bool animateHeight = true;
    public float animationSpeed = 1f;
    public float animationAmplitude = 0.5f;
    
    private Vector3 _initialPosition;
    
    private void Start()
    {
        _initialPosition = transform.position;
    }
    
    private void Update()
    {
        if (animateHeight)
        {
            // Плавная анимация вверх-вниз
            float newY = _initialPosition.y + Mathf.Sin(Time.time * animationSpeed) * animationAmplitude;
            transform.position = new Vector3(
                _initialPosition.x, 
                newY, 
                _initialPosition.z
            );
        }
    }
    
    private void OnDrawGizmos()
    {
        // Рисуем сферу
        Gizmos.color = markerColor;
        Gizmos.DrawSphere(transform.position, markerSize);
        
        // Рисуем полупрозрачную область вокруг
        Color transparentColor = markerColor;
        transparentColor.a = 0.3f;
        Gizmos.color = transparentColor;
        Gizmos.DrawSphere(transform.position, markerSize * 1.5f);
        
        // Рисуем вертикальную линию к земле
        Gizmos.color = markerColor;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, 0, transform.position.z));
        
#if UNITY_EDITOR
        if (showLabel)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (markerSize + 1f), 
                gameObject.name,
                new GUIStyle() 
                { 
                    normal = new GUIStyleState() { textColor = markerColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                }
            );
        }
#endif
    }
}