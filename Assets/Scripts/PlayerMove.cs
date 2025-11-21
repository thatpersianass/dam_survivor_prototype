using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private bool canMove = true;
    [SerializeField] private float movementSpeed;
    private Vector2 planeDirection;
    private Controls control;
    private Camera mainCamera; 
    public LayerMask groundLayer;

    private Vector3 currentVelocity = Vector3.zero; 
    private Vector3 desiredDirection = Vector3.zero;
    private float acceleration = 25f;
    private float deceleration = 35f;
    private PlayerStats stats;


    private void Awake()
    {
        control = new Controls();
    }

    private void OnEnable()
    {
        control.Player.Enable();
    }
    void Start()
    {   
        mainCamera = Camera.main; 
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (canMove)
        {
            // 1. OBTENER INPUT DESEADO
            planeDirection = control.Player.Move.ReadValue<Vector2>();
            desiredDirection = new Vector3(planeDirection.x, 0f, planeDirection.y).normalized;

            // 2. APLICAR ACELERACIÓN / FRICCIÓN
            ApplySmoothMovement();

            // 3. ROTACIÓN (Sin cambios)
            RotateTowardsMouseInstant();
        }
    }
    
    private void ApplySmoothMovement()
    {
        // Determinar la velocidad de cambio (aceleración o deceleración)
        float speedChange;

        if (desiredDirection.magnitude > 0.01f)
        {
            // Acelerando: Si el jugador presiona una tecla.
            speedChange = acceleration;
        }
        else
        {
            // Desacelerando: Si no hay input, el jugador se desliza.
            speedChange = deceleration;
        }

        // 1. Calcular la velocidad a la que queremos llegar (Velocidad Deseada * Máx. Velocidad)
        Vector3 targetVelocity = desiredDirection * stats.moveSpeed;

        // 2. Suavizar la velocidad actual hacia la velocidad objetivo (Aceleración/Fricción)
        currentVelocity = Vector3.MoveTowards(
            currentVelocity, 
            targetVelocity, 
            speedChange * Time.deltaTime
        );

        // 3. Aplicar el movimiento usando la velocidad suavizada
        transform.position += currentVelocity * Time.deltaTime;
    }
    
    private void RotateTowardsMouseInstant()
    {
        Vector2 mouseScreenPosition = control.Player.MousePosition.ReadValue<Vector2>();
        Ray cameraRay = mainCamera.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;
        Debug.DrawRay(cameraRay.origin, cameraRay.direction * 100f, Color.red); 
        if (Physics.Raycast(cameraRay, out hit, 100f, groundLayer))
        {
            Vector3 pointToLook = hit.point;
            pointToLook.y = transform.position.y;
            Vector3 lookDirection = pointToLook - transform.position;
            
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = targetRotation; 
            }
        }
        else
        {
            // Debug.LogWarning("Raycast NO golpeó el suelo.");
        }
    }
}