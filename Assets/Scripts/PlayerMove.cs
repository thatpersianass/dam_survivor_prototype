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
    [SerializeField] private float dashSpeed = 25f;      
    [SerializeField] private float dashDuration = 0.2f;  
    [SerializeField] private float dashCooldown = 2.0f;  
    [SerializeField] private int dashDamage = 50;        
    
    [Tooltip("Tiempo extra de inmunidad al terminar el dash")]
    [SerializeField] private float postDashImmunityTime = 0.5f; 
    
    [Header("--- Visuals (Smear Effect) ---")]
    [Tooltip("Arrastra aquí el objeto HIJO 'Capsule' o Mesh Filter")]
    [SerializeField] private Transform playerVisualModel; 
    [SerializeField] private Vector3 smearScale = new Vector3(0.6f, 2.0f, 0.6f);
    [SerializeField] private Vector3 smearRotation = new Vector3(90f, 0f, 0f);

    [Header("--- Hitbox & Visuals del Dash ---")]
    [SerializeField] private BoxCollider dashHitbox; 
    [SerializeField] private LayerMask enemyLayer;       
    [SerializeField] private TrailRenderer dashTrail;    

    // Referencias
    private Vector2 planeDirection;
    private Controls control;
    private Camera mainCamera; 
    public LayerMask groundLayer;
    private PlayerStats myStats; 
    private Rigidbody rb;

    // Física
    private Vector3 currentVelocity = Vector3.zero; 
    private Vector3 desiredDirection = Vector3.zero;
    private float acceleration = 25f;
    private float deceleration = 35f;

    // Estados
    private bool isKnockedBack = false;
    private bool isDashing = false;
    private float dashTimer = 0f; 

    private void Awake()
    {
        control = new Controls();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable() { control.Player.Enable(); }
    private void OnDisable() { control.Player.Disable(); }

    void Start()
    {   
        mainCamera = Camera.main; 
        myStats = GetComponent<PlayerStats>();
        
        if (dashTrail != null) dashTrail.emitting = false;
        if (dashHitbox == null) dashHitbox = GetComponent<BoxCollider>();

        if (playerVisualModel == null)
        {
            MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            if (mesh != null) playerVisualModel = mesh.transform;
        }
    }

    void Update()
    {
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        if (control.Player.Ability.triggered && dashTimer <= 0 && !isDashing && !isKnockedBack && canMove)
        {
            StartCoroutine(DashRoutine());
        }

        if (canMove && !isKnockedBack && !isDashing)
        {
            planeDirection = control.Player.Move.ReadValue<Vector2>();
            desiredDirection = new Vector3(planeDirection.x, 0f, planeDirection.y).normalized;

            ApplySmoothMovement();
            RotateTowardsMouseInstant();
        }
    }
    
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        dashTimer = dashCooldown; 

        // 1. Bloquear físicas para evitar conflictos
        if (rb != null) rb.isKinematic = true;

        if (myStats != null) myStats.isInvincible = true;
        if (dashTrail != null) dashTrail.emitting = true;

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetDashLag(true);
            CameraFollow.instance.TriggerShake(0.1f, 0.1f); 
        }

        Vector3 originalScale = Vector3.one;
        Quaternion originalRotation = Quaternion.identity;

        if (playerVisualModel != null)
        {
            originalScale = playerVisualModel.localScale;
            originalRotation = playerVisualModel.localRotation;
            playerVisualModel.localScale = smearScale;
            playerVisualModel.localRotation = Quaternion.Euler(smearRotation);
        }

        // --- CORRECCIÓN DE DIRECCIÓN ---
        Vector3 dashDirection = transform.forward; 
        dashDirection.y = 0; // ¡IMPORTANTE! Anular inclinación vertical
        dashDirection.Normalize(); 
        // -------------------------------

        HashSet<GameObject> enemiesHit = new HashSet<GameObject>(); 
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            transform.position += dashDirection * dashSpeed * Time.deltaTime;

            if (dashHitbox != null)
            {
                Vector3 hitboxCenter = transform.TransformPoint(dashHitbox.center);
                Vector3 halfSize = Vector3.Scale(dashHitbox.size, transform.lossyScale) * 0.5f;
                Collider[] hits = Physics.OverlapBox(hitboxCenter, halfSize, transform.rotation, enemyLayer);

                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Enemy") && !enemiesHit.Contains(hit.gameObject))
                    {
                        EnemyMovement enemyScript = hit.GetComponent<EnemyMovement>();
                        if (enemyScript != null)
                        {
                            enemyScript.RecieveDmg(dashDamage);
                            enemiesHit.Add(hit.gameObject);
                            if (CameraFollow.instance != null) CameraFollow.instance.TriggerShake(0.15f, 0.2f);
                        }
                    }
                }
            }
            yield return null; 
        }

        if (rb != null) rb.isKinematic = false; // Reactivar físicas

        if (playerVisualModel != null)
        {
            playerVisualModel.localScale = originalScale;
            playerVisualModel.localRotation = originalRotation;
        }

        if (CameraFollow.instance != null) CameraFollow.instance.SetDashLag(false);

        currentVelocity = Vector3.zero; 
        isDashing = false; 
        if (dashTrail != null) dashTrail.emitting = false;

        if (postDashImmunityTime > 0)
        {
            yield return new WaitForSeconds(postDashImmunityTime);
        }

        if (myStats != null) myStats.isInvincible = false;
    }

    public void ApplyKnockback(Vector3 sourcePosition, float pushPower, float duration)
    {
        if (isKnockedBack || isDashing) return;
        StartCoroutine(KnockbackRoutine(sourcePosition, pushPower, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition, float power, float duration)
    {
        isKnockedBack = true;
        Vector3 direction = (transform.position - sourcePosition).normalized;
        direction.y = 0; 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration); 
            transform.position += direction * power * t * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentVelocity = Vector3.zero; 
        isKnockedBack = false;
    }

    private void ApplySmoothMovement()
    {
        float speedChange = (desiredDirection.magnitude > 0.01f) ? acceleration : deceleration;
        Vector3 targetVelocity = desiredDirection * movementSpeed;
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
            pointToLook.y = transform.position.y; 
            Vector3 lookDirection = pointToLook - transform.position;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection); 
            }
        }
    }
}