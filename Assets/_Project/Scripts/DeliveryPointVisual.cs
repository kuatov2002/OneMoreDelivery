using UnityEngine;

[RequireComponent(typeof(DeliveryPointMarker))]
public class DeliveryPointVisual : MonoBehaviour
{
    [Header("3D Визуализация")]
    public GameObject visualPrefab; // Опционально: префаб для визуализации
    
    private GameObject _visualInstance;
    
    private void Start()
    {
        CreateVisual();
    }
    
    private void CreateVisual()
    {
        if (visualPrefab != null)
        {
            _visualInstance = Instantiate(visualPrefab, transform.position, Quaternion.identity, transform);
        }
        else
        {
            // Создаем простой куб как визуализацию
            _visualInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _visualInstance.transform.parent = transform;
            _visualInstance.transform.localPosition = Vector3.zero;
            _visualInstance.transform.localScale = new Vector3(1f, 2f, 1f);
            
            // Настраиваем материал
            Renderer renderer = _visualInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                DeliveryPointMarker marker = GetComponent<DeliveryPointMarker>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = marker.markerColor
                };
                renderer.material = mat;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (_visualInstance != null)
        {
            Destroy(_visualInstance);
        }
    }
}