using UnityEngine;
using UnityEngine.InputSystem; 

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Offset Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f); 

    [Header("Smooth Settings")]
    [SerializeField] private float normalSmoothTime = 0.1f; 
    [SerializeField] private float dashSmoothTime = 0.4f; 
    
    private float currentSmoothTime;
    private Vector3 currentVelocity; 
    private Vector3 internalPosition; 

    [Header("Zoom Settings")]
    [SerializeField] private float scrollSensitivity = 0.005f;
    [SerializeField] private float zoomSpeed = 5f; 
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;

    // --- NUEVO: DASH ZOOM ---
    [Header("Dash Zoom FX")]
    [Tooltip("Cuánto se aleja la cámara al hacer dash (Ej: 3)")]
    [SerializeField] private float dashZoomAmount = 2.0f; 
    [Tooltip("Qué tan rápido entra y sale el zoom (Ej: 5)")]
    [SerializeField] private float dashZoomSpeed = 5.0f;
    
    private float targetDashZoom = 0f;
    private float currentDashZoom = 0f;
    // ------------------------

    private float shakeTimer;
    private float shakeMagnitude;

    private float zoom;
    private float finalZoom;
    private Controls controls; 

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        controls = new Controls();
    }
    
    void OnEnable() { if(controls != null) controls.Camera.Enable(); }
    void OnDisable() { if(controls != null) controls.Camera.Disable(); }

    void Start()
    {
        zoom = (minZoom + maxZoom) / 2f;
        finalZoom = zoom;
        currentSmoothTime = normalSmoothTime;
        
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
        internalPosition = transform.position;
    }

    public void SetDashLag(bool isDashing)
    {
        currentSmoothTime = isDashing ? dashSmoothTime : normalSmoothTime;
        
        // --- ACTIVAR ZOOM ---
        // Si hacemos dash, el objetivo es dashZoomAmount. Si no, es 0.
        targetDashZoom = isDashing ? dashZoomAmount : 0f;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. ZOOM MANUAL (Rueda del ratón)
        float scrollDelta = controls.Camera.Zoom.ReadValue<float>(); 
        if (Mathf.Abs(scrollDelta) > 0.001f)
        {
            finalZoom -= scrollDelta * scrollSensitivity; 
            finalZoom = Mathf.Clamp(finalZoom, minZoom, maxZoom);
        }
        zoom += (finalZoom - zoom) * zoomSpeed * Time.deltaTime;
        
        // 2. ZOOM AUTOMÁTICO (DASH) - Interpolación suave
        currentDashZoom = Mathf.Lerp(currentDashZoom, targetDashZoom, dashZoomSpeed * Time.deltaTime);

        // 3. CALCULAR POSICIÓN DESTINO
        // Sumamos el Zoom Manual + el Zoom del Dash
        float totalZoom = zoom + currentDashZoom;
        
        Vector3 targetPosition = target.position + offset.normalized * totalZoom;

        // 4. MOVIMIENTO SUAVE
        internalPosition = Vector3.SmoothDamp(
            internalPosition, 
            targetPosition, 
            ref currentVelocity, 
            currentSmoothTime
        );

        // 5. APLICAR TEMBLOR Y ASIGNAR
        Vector3 finalPosition = internalPosition;
        if (shakeTimer > 0)
        {
            finalPosition += Random.insideUnitSphere * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }
        transform.position = finalPosition;
    }
}