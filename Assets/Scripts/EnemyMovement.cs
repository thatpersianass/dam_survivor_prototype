using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject player;
    public EnemyStats Stats;
    private PlayerStats playerStats;

    [Header("Visual Effects (Daño)")]
    [SerializeField] private Material originalMaterial; 
    [SerializeField] private Material hitMaterial; 
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Visual Effects (Hit Anim)")]
    [SerializeField] private float tiltAngle = 25f; 
    [SerializeField] private float tiltRecoverSpeed = 5f;
    private Coroutine tiltCoroutine;
    
    // --- NUEVO: Configuración de Temblor de Cámara ---
    [Header("Visual Effects (Camera Shake)")]
    [Tooltip("Tiempo que tiembla la pantalla (ej: 0.1 seg)")]
    [SerializeField] private float shakeDuration = 0.1f;
    [Tooltip("Fuerza del temblor (ej: 0.2). Cuidado con ponerlo muy alto.")]
    [SerializeField] private float shakeAmount = 0.2f; 

    [Header("Visual Effects (Muerte)")]
    [SerializeField] private Animator animator; 
    [SerializeField] private float deathAnimationDuration = 1.5f; 

    private Renderer enemyRenderer;                     
    private Coroutine flashCoroutine;                   
    private Collider enemyCollider; 
    private bool isDead = false;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDuration = 0.25f;
    [SerializeField] private float knockbackPower = 5f;
    private bool isKnockedBack = false;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float attackTimer = 0f;

    [Header("Loot Settings")]
    [SerializeField] private GameObject experiencePrefab;
    [SerializeField] private int experienceAmount;
    [Range(0f, 100f)]
    [SerializeField] private float dropChance = 100f;
    [SerializeField] private float dropSpread = 0.5f;
    [SerializeField] private int maxDrops = 3;

    private int maxHP;
    private int currentHP;
    private int damage;
    private int defense;
    private float speed;
    private bool isHitAnimating = false; 

    void Awake()
    {
        if (Stats != null)
        {
            maxHP = Stats.MaxHP;
            currentHP = maxHP;
            damage = Stats.Damage;
            defense = Stats.Defense;
            speed = Stats.Speed;
        }
        enemyCollider = GetComponent<Collider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

        enemyRenderer = GetComponentInChildren<Renderer>();
        
        if (originalMaterial == null && enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.sharedMaterial;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (!isKnockedBack)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && attackTimer <= 0)
            {
                AttackPlayer();
            }
            else if (distanceToPlayer > attackRange)
            {
                Vector3 direction = (player.transform.position - transform.position).normalized;
                
                transform.position += direction * speed * Time.deltaTime;

                if (direction != Vector3.zero && !isHitAnimating)
                {
                    Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
                }
            }
        }
    }

    public void RecieveDmg(int dmg)
    {
        if (isDead) return;

        StopCoroutine("KnockbackRoutine");
        StartCoroutine(KnockbackRoutine());

        // 1. Flash Material
        if (enemyRenderer != null && originalMaterial != null && hitMaterial != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashMaterialRoutine());
        }

        // 2. Animación de Inclinación
        if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
        tiltCoroutine = StartCoroutine(HitTiltRoutine());

        // 3. --- NUEVO: LLAMAR AL TEMBLOR DE CÁMARA ---
        // Usamos 'instance' para no tener que arrastrar la cámara a cada enemigo
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.TriggerShake(shakeDuration, shakeAmount);
        }

        int finalDamage = dmg - defense;
        if (finalDamage < 0) finalDamage = 0;
        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // ... [RESTO DEL CÓDIGO IGUAL QUE ANTES] ...
    // (He omitido los métodos HitTiltRoutine, FlashMaterialRoutine, etc. para ahorrar espacio, 
    // pero asegúrate de mantenerlos en tu archivo)

    private IEnumerator HitTiltRoutine()
    {
        isHitAnimating = true;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(-tiltAngle, 0, 0);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 15f; 
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * tiltRecoverSpeed;
            transform.rotation = Quaternion.Lerp(targetRot, startRot, t);
            yield return null;
        }
        transform.rotation = startRot;
        isHitAnimating = false;
    }

    private IEnumerator FlashMaterialRoutine()
    {
        float elapsedTime = 0f;
        float halfDuration = flashDuration / 2f; 
        while (elapsedTime < halfDuration)
        {
            enemyRenderer.material.Lerp(originalMaterial, hitMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime; yield return null;
        }
        enemyRenderer.material = hitMaterial;
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            enemyRenderer.material.Lerp(hitMaterial, originalMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime; yield return null;
        }
        enemyRenderer.material = originalMaterial;
        flashCoroutine = null;
    }

    private void AttackPlayer()
    {
        if (playerStats != null)
        {
            attackTimer = attackCooldown;
            
            playerStats.TakeDamage(damage, transform.position);
        }
    }

    private IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true;
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        Vector3 knockbackDirection = -directionToPlayer;
        float elapsedTime = 0f;
        while (elapsedTime < knockbackDuration) {
            float t = 1f - (elapsedTime / knockbackDuration);
            transform.position += knockbackDirection * knockbackPower * t * Time.deltaTime;
            elapsedTime += Time.deltaTime; yield return null;
        }
        isKnockedBack = false;
    }

    private void Die()
    {
        isDead = true;
        if (enemyCollider != null) enemyCollider.enabled = false;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (animator != null) animator.SetTrigger("Die"); 
        SpawnLoot();
        yield return new WaitForSeconds(deathAnimationDuration);
        float sinkTimer = 0f;
        while (sinkTimer < 1.5f) {
            transform.position -= Vector3.up * 1f * Time.deltaTime;
            sinkTimer += Time.deltaTime; yield return null;
        }
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        if (experiencePrefab != null) {
            for (int i = 0; i < maxDrops; i++) {
                if (Random.Range(0f, 100f) <= dropChance) {
                    Vector3 randomOffset = Random.insideUnitCircle * dropSpread;
                    Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0.5f, randomOffset.y);
                    GameObject xpInstance = Instantiate(experiencePrefab, spawnPosition, Quaternion.identity);
                    ExperienceGem xpScript = xpInstance.GetComponent<ExperienceGem>();
                    if (xpScript != null) xpScript.SetAmount(experienceAmount);
                }
            }
        }
    }
}