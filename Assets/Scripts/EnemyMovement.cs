using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject player;
    public EnemyStats Stats;
    private PlayerStats playerStats;

    [Header("Visual Effects (Daño)")]
    [Tooltip("Arrastra aquí el material NORMAL del enemigo (con su shader y texturas).")]
    [SerializeField] private Material originalMaterial; 
    [Tooltip("Arrastra aquí el material de DAÑO.")]
    [SerializeField] private Material hitMaterial; 
    [SerializeField] private float flashDuration = 0.2f;
    
    // --- NUEVO: Referencias para la Animación de Muerte ---
    [Header("Visual Effects (Muerte)")]
    [Tooltip("Arrastra el componente Animator aquí.")]
    [SerializeField] private Animator animator; 
    [Tooltip("Tiempo exacto que dura tu animación de muerte (ej: 1.5).")]
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
        
        // Si no asignaste el animator manualmente, intentamos buscarlo
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
        // Si está muerto o no hay player, no hacemos nada
        if (isDead || player == null) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (!isKnockedBack)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Lógica de ataque y movimiento
            if (distanceToPlayer <= attackRange && attackTimer <= 0)
            {
                AttackPlayer();
            }
            else if (distanceToPlayer > attackRange)
            {
                Vector3 direction = (player.transform.position - transform.position).normalized;
                
                // Rotación opcional para mirar al jugador
                if (direction != Vector3.zero)
                {
                    Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
                }

                transform.position += direction * speed * Time.deltaTime;
            }
            
            // Sincronizar animación de caminar (Si tienes un parámetro 'Speed' en el Animator)
            // if (animator != null) animator.SetFloat("Speed", speed);
        }
    }

    public void RecieveDmg(int dmg)
    {
        if (isDead) return;

        StopCoroutine("KnockbackRoutine");
        StartCoroutine(KnockbackRoutine());

        if (enemyRenderer != null && originalMaterial != null && hitMaterial != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashMaterialRoutine());
        }

        int finalDamage = dmg - defense;
        if (finalDamage < 0) finalDamage = 0;
        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashMaterialRoutine()
    {
        float elapsedTime = 0f;
        float halfDuration = flashDuration / 2f; 

        while (elapsedTime < halfDuration)
        {
            enemyRenderer.material.Lerp(originalMaterial, hitMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        enemyRenderer.material = hitMaterial;
        
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            enemyRenderer.material.Lerp(hitMaterial, originalMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        enemyRenderer.material = originalMaterial;
        flashCoroutine = null;
    }

    private void AttackPlayer()
    {
        if (playerStats != null)
        {
            // Opcional: Si tienes animación de ataque
            // if(animator != null) animator.SetTrigger("Attack");
            
            attackTimer = attackCooldown;
            playerStats.TakeDamage(damage);
        }
    }
    
    private IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true;
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        Vector3 knockbackDirection = -directionToPlayer;

        float elapsedTime = 0f;
        while (elapsedTime < knockbackDuration)
        {
            float t = 1f - (elapsedTime / knockbackDuration);
            transform.position += knockbackDirection * knockbackPower * t * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        isKnockedBack = false;
    }

    private void Die()
    {
        isDead = true;

        // 1. Desactivar Collider inmediatamente
        if (enemyCollider != null) enemyCollider.enabled = false;

        // 2. Iniciar secuencia
        StartCoroutine(DeathSequence());
    }

    // --------------------------------------------------------
    // SECUENCIA DE MUERTE CON ANIMATOR
    // --------------------------------------------------------
    private IEnumerator DeathSequence()
    {
        // A. Disparar animación en el Animator
        if (animator != null)
        {
            animator.SetTrigger("Die"); // Asegúrate de crear este Trigger en la ventana Animator
        }

        // B. Soltar experiencia
        SpawnLoot();

        // C. Esperar lo que dura la animación (configurado en inspector)
        yield return new WaitForSeconds(deathAnimationDuration);

        // D. (Opcional) Hundirse bajo tierra para desaparecer suavemente
        float sinkTimer = 0f;
        while (sinkTimer < 2f)
        {
            transform.position -= Vector3.up * 1f * Time.deltaTime;
            sinkTimer += Time.deltaTime;
            yield return null;
        }

        // E. Destruir
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        if (experiencePrefab != null)
        {
            for (int i = 0; i < maxDrops; i++)
            {
                if (Random.Range(0f, 100f) <= dropChance)
                {
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