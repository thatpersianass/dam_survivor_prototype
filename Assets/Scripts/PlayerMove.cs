using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;
using System.Collections.Generic; 

public class PlayerMove : MonoBehaviour
{
    [Header("--- Movimiento Base ---")]
    [SerializeField] private bool canMove = true;
    [SerializeField] private float movementSpeed = 5f;
    
    [Header("--- Habilidad Dash (Tecla E) ---")]
    [SerializeField] private float dashSpeed = 25f;      // Velocidad muy rápida
    [SerializeField] private float dashDuration = 0.2f;  // Duración corta
    [SerializeField] private float dashCooldown = 2.0f;  // Tiempo de espera
    [SerializeField] private int dashDamage = 50;        // Daño al atravesar
    
    [Tooltip("Tiempo extra de inmunidad al terminar el dash (segundos)")]
    [SerializeField] private float postDashImmunityTime = 0.5f; 
    
    [Header("--- Hitbox & Visuals del Dash ---")]
    [Tooltip("Arrastra aquí el Box Collider que usas como área de daño")]
    [SerializeField] private BoxCollider dashHitbox; 
    [SerializeField] private LayerMask enemyLayer;       
    [SerializeField] private TrailRenderer dashTrail;    

    // --- Referencias Internas ---
    private Vector2 planeDirection;
    private Controls control;
    private Camera mainCamera; 
    public LayerMask groundLayer;
    private PlayerStats myStats; 

    // --- Variables de Física ---
    private Vector3 currentVelocity = Vector3.zero; 
    private Vector3 desiredDirection = Vector3.zero;
    private float acceleration = 25f;
    private float deceleration = 35f;

    // --- Estados ---
    private bool isKnockedBack = false;
    private bool isDashing = false;
    private float dashTimer = 0f; 

    private void Awake()
    {
        control = new Controls();
    }

    private void OnEnable() 
    { 
        control.Player.Enable(); 
    }
    
    private void OnDisable() 
    { 
        control.Player.Disable(); 
    }

    void Start()
    {   
        mainCamera = Camera.main; 
        myStats = GetComponent<PlayerStats>();
        
        // Desactivar la estela al inicio por si acaso
        if (dashTrail != null) dashTrail.emitting = false;
        
        // Auto-asignar hitbox si se olvidó
        if (dashHitbox == null) dashHitbox = GetComponent<BoxCollider>();
    }

    void Update()
    {
        // 1. Gestionar Cooldown del Dash
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        // 2. DETECTAR INPUT DASH (Botón E)
        // Debe cumplir: Tecla pulsada, cooldown listo, no estar ya dasheando, no estar empujado, poder moverse.
        if (control.Player.Ability.triggered && dashTimer <= 0 && !isDashing && !isKnockedBack && canMove)
        {
            StartCoroutine(DashRoutine());
        }

        // 3. MOVIMIENTO NORMAL
        // Solo nos movemos si NO estamos en Dash ni en Knockback
        if (canMove && !isKnockedBack && !isDashing)
        {
            // Leer input
            planeDirection = control.Player.Move.ReadValue<Vector2>();
            desiredDirection = new Vector3(planeDirection.x, 0f, planeDirection.y).normalized;

            // Aplicar física
            ApplySmoothMovement();
            RotateTowardsMouseInstant();
        }
    }
    
    // ---------------------------------------------------------
    // LÓGICA PRINCIPAL DEL DASH
    // ---------------------------------------------------------
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        dashTimer = dashCooldown; // Reiniciar cooldown

        // 1. Activar Invencibilidad y Visuales
        if (myStats != null) myStats.isInvincible = true;
        if (dashTrail != null) dashTrail.emitting = true;

        // 2. Avisar a la cámara para que active el efecto de LAG
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetDashLag(true);
        }

        Vector3 dashDirection = transform.forward; // Dash hacia donde miras (ratón)
        HashSet<GameObject> enemiesHit = new HashSet<GameObject>(); // Para no pegar 2 veces al mismo

        float startTime = Time.time;

        // --- BUCLE DE MOVIMIENTO ---
        while (Time.time < startTime + dashDuration)
        {
            // A. Mover al jugador manualmente (sin física compleja)
            transform.position += dashDirection * dashSpeed * Time.deltaTime;

            // B. Detectar Enemigos con el Box Collider
            if (dashHitbox != null)
            {
                // Calculamos posición y rotación exacta del Hitbox en el mundo
                Vector3 hitboxCenter = transform.TransformPoint(dashHitbox.center);
                // OverlapBox usa "Half Extents" (mitad del tamaño), así que multiplicamos por 0.5
                Vector3 halfSize = Vector3.Scale(dashHitbox.size, transform.lossyScale) * 0.5f;

                Collider[] hits = Physics.OverlapBox(hitboxCenter, halfSize, transform.rotation, enemyLayer);

                foreach (Collider hit in hits)
                {
                    // Si es enemigo y no está en la lista de "ya golpeados"
                    if (hit.CompareTag("Enemy") && !enemiesHit.Contains(hit.gameObject))
                    {
                        EnemyMovement enemyScript = hit.GetComponent<EnemyMovement>();
                        if (enemyScript != null)
                        {
                            // ¡PAM! Daño
                            enemyScript.RecieveDmg(dashDamage);
                            enemiesHit.Add(hit.gameObject);
                            // Debug.Log("Dash atravesó a: " + hit.name);
                        }
                    }
                }
            }

            yield return null; // Esperar al siguiente frame
        }

        // --- FIN DEL DASH (LIMPIEZA) ---
        
        // 3. Quitar Lag de cámara
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetDashLag(false);
        }

        currentVelocity = Vector3.zero; // Frenar en seco para no patinar
        isDashing = false; 
        if (dashTrail != null) dashTrail.emitting = false;

        // 4. Fase de Inmunidad Extra (Post-Dash)
        if (postDashImmunityTime > 0)
        {
            yield return new WaitForSeconds(postDashImmunityTime);
        }

        // 5. Volver a ser vulnerable
        if (myStats != null) myStats.isInvincible = false;
    }

    // ---------------------------------------------------------
    // LÓGICA DE KNOCKBACK (EMPUJÓN)
    // ---------------------------------------------------------
    public void ApplyKnockback(Vector3 sourcePosition, float pushPower, float duration)
    {
        // Si estamos haciendo Dash, ignoramos el empujón (somos imparables)
        if (isKnockedBack || isDashing) return;
        
        StartCoroutine(KnockbackRoutine(sourcePosition, pushPower, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition, float power, float duration)
    {
        isKnockedBack = true;
        
        // Dirección contraria al golpe
        Vector3 direction = (transform.position - sourcePosition).normalized;
        direction.y = 0; // Mantener en el suelo

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Movimiento suavizado (Lerp inverso)
            float t = 1f - (elapsed / duration); 
            transform.position += direction * power * t * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        currentVelocity = Vector3.zero; 
        isKnockedBack = false;
    }

    // ---------------------------------------------------------
    // UTILIDADES DE MOVIMIENTO
    // ---------------------------------------------------------
    private void ApplySmoothMovement()
    {
        // Aceleración vs Deceleración (Fricción)
        float speedChange = (desiredDirection.magnitude > 0.01f) ? acceleration : deceleration;
        
        Vector3 targetVelocity = desiredDirection * movementSpeed;
        
        // Suavizado de velocidad
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, speedChange * Time.deltaTime);
        
        transform.position += currentVelocity * Time.deltaTime;
    }

    private void RotateTowardsMouseInstant()
    {
        Vector2 mouseScreenPosition = control.Player.MousePosition.ReadValue<Vector2>();
        Ray cameraRay = mainCamera.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(cameraRay, out hit, 100f, groundLayer))
        {
            Vector3 pointToLook = hit.point;
            pointToLook.y = transform.position.y; // Mantener altura para no mirar al suelo
            Vector3 lookDirection = pointToLook - transform.position;
            
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection); 
            }
        }
    }
}