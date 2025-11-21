using UnityEngine;
using UnityEngine.InputSystem; 

public class CameraFollow : MonoBehaviour
{
    // --- SINGLETON (Acceso Global) ---
    public static CameraFollow instance;

    [Header("Target Settings")]
    [Tooltip("Arrastra aquí a tu Jugador")]
    [SerializeField] private Transform target;

    [Header("Offset Settings")]
    [Tooltip("Posición relativa de la cámara (Altura y distancia)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f); 

    [Header("Smooth Settings (Movimiento & Lag)")]
    [Tooltip("Velocidad de seguimiento normal (0.1 = Rápido y pegado)")]
    [SerializeField] private float normalSmoothTime = 0.1f; 
    
    [Tooltip("Velocidad durante el Dash (0.4 = La cámara tarda en llegar)")]
    [SerializeField] private float dashSmoothTime = 0.4f; 
    
    // Variables internas para el SmoothDamp
    private float currentSmoothTime;
    private Vector3 currentVelocity; 

    [Header("Zoom Settings")]
    [SerializeField] private float scrollSensitivity = 0.005f;
    [SerializeField] private float zoomSpeed = 5f; 
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;

    // Variables de Shake (Temblor)
    private float shakeTimer;
    private float shakeMagnitude;

    // Variables de Zoom
    private float zoom;
    private float finalZoom;
    private Controls controls; 

    private void Awake()
    {
        // Configuración del Singleton:
        // Si ya existe una cámara, destruimos esta para no tener duplicados.
        if (instance == null) 
        {
            instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        controls = new Controls();
    }
    
    private void OnEnable() 
    { 
        if(controls != null) controls.Camera.Enable(); 
    }
    
    private void OnDisable() 
    { 
        if(controls != null) controls.Camera.Disable(); 
    }

    void Start()
    {
        // Inicializar zoom en un punto medio
        zoom = (minZoom + maxZoom) / 2f;
        finalZoom = zoom;
        
        // Empezamos con la velocidad de seguimiento normal
        currentSmoothTime = normalSmoothTime;
        
        // Si se te olvidó asignar el target, intentamos buscar al Player
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

    // ---------------------------------------------------------
    // MÉTODOS PÚBLICOS (Llamados por otros scripts)
    // ---------------------------------------------------------

    // Activa o desactiva el "Lag" de la cámara (Llamado por PlayerMove en el Dash)
    public void SetDashLag(bool isDashing)
    {
        if (isDashing)
            currentSmoothTime = dashSmoothTime; // Modo lento/perezoso
        else
            currentSmoothTime = normalSmoothTime; // Modo normal
    }

    // Activa el temblor de pantalla (Llamado por EnemyMovement o PlayerStats)
    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    // ---------------------------------------------------------
    // LÓGICA DE SEGUIMIENTO
    // ---------------------------------------------------------
    private void LateUpdate()
    {
        if (target == null) return;

        // 1. CÁLCULO DE ZOOM
        float scrollDelta = controls.Camera.Zoom.ReadValue<float>(); 
        
        // Solo recalculamos si se mueve la rueda
        if (Mathf.Abs(scrollDelta) > 0.001f)
        {
            finalZoom -= scrollDelta * scrollSensitivity; 
            finalZoom = Mathf.Clamp(finalZoom, minZoom, maxZoom);
        }

        // Suavizado del Zoom
        float leftDistance = finalZoom - zoom;
        float distance2Move = leftDistance  * zoomSpeed * Time.deltaTime; 
        zoom += distance2Move;
        
        // 2. CALCULAR POSICIÓN OBJETIVO (Sin Temblor aún)
        // Posición del jugador + (Dirección del offset * Distancia de zoom)
        Vector3 targetPosition = target.position + offset.normalized * zoom;

        // 3. APLICAR SHAKE (TEMBLOR)
        if (shakeTimer > 0)
        {
            // Añadimos un desplazamiento aleatorio a la posición destino
            targetPosition += Random.insideUnitSphere * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }

        // 4. MOVIMIENTO FINAL (SMOOTHDAMP)
        // Aquí es donde ocurre la magia del "Lag" y el movimiento fluido
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            currentSmoothTime
        );
    }
}