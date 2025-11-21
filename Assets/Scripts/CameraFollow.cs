using UnityEngine;
using UnityEngine.InputSystem; 

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Offset Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f); 

    [Header("Zoom Settings")]
    [SerializeField] private float scrollSensitivity = 0.005f;
    [SerializeField] private float zoomSpeed = 5f; 
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;

    private float zoom;
    private float finalZoom;
    private Controls controls; 

    void Awake()
    {
        controls = new Controls();
    }
    
    void OnEnable()
    {
        controls.Camera.Enable();
    }

    void Start()
    {
        zoom = (minZoom + maxZoom) / 2f;
        finalZoom = zoom;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float scrollDelta = controls.Camera.Zoom.ReadValue<float>(); 

        if (scrollDelta > 0.001f || scrollDelta < -0.001f)
        {
            finalZoom -= scrollDelta * scrollSensitivity; 
            
            if (finalZoom < minZoom)
            {
                finalZoom = minZoom;
            }
            else if (finalZoom > maxZoom)
            {
                finalZoom = maxZoom;
            }
        }

        float leftDistance = finalZoom - zoom;

        float distance2Move = leftDistance  * zoomSpeed * Time.deltaTime; 
        
        zoom += distance2Move;
        
        Vector3 desiredPosition = target.position + offset.normalized * zoom;

        transform.position = desiredPosition;
    }
}