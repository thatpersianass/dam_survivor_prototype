using UnityEngine;
using System.Collections; // Necesario para Corrutinas

public class PlayerStats : MonoBehaviour
{
    [Header("--- Salud y Defensa ---")]
    public int maxHealth = 100;
    public int currentHealth;
    public int defense = 0;
    public bool isAlive = true;

    [Header("--- Estados ---")]
    public bool isInvincible = false;

    [Header("--- Hit Feedback (Física/Cámara) ---")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackTime = 0.2f;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeForce = 0.3f;
    
    [Header("--- Hit Animation (Tilt) ---")]
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float tiltSpeed = 15f;

    // --- NUEVO: EFECTO DE MATERIAL (FLASH) ---
    [Header("--- Visual Effects (Flash) ---")]
    [Tooltip("Arrastra aquí el material NORMAL del jugador.")]
    [SerializeField] private Material originalMaterial; 
    [Tooltip("Arrastra aquí el material de DAÑO (Blanco o Rojo).")]
    [SerializeField] private Material hitMaterial; 
    [SerializeField] private float flashDuration = 0.2f;
    
    private Renderer playerRenderer;
    private Coroutine flashCoroutine;

    [Header("--- Nivel y Experiencia ---")]
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int experienceToNextLevel = 100; 
    [SerializeField] private float levelMultiplier = 1.2f; 

    [Header("--- Stats de Movimiento ---")]
    public float moveSpeed = 5f; 

    // Referencias internas
    private PlayerMove playerMove;
    private Coroutine tiltCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        isAlive = true;
        playerMove = GetComponent<PlayerMove>();
        
        // Buscamos el Renderer (sirve para MeshRenderer y SkinnedMeshRenderer)
        playerRenderer = GetComponentInChildren<Renderer>();

        // Auto-asignar material original si se te olvidó ponerlo
        if (originalMaterial == null && playerRenderer != null)
        {
            originalMaterial = playerRenderer.sharedMaterial;
        }
    }

    // Sobrecarga simple
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position + transform.forward);
    }

    // Función principal de Daño
    public void TakeDamage(int damage, Vector3 hitSourcePosition)
    {
        if (!isAlive || isInvincible) return;

        int finalDamage = damage - defense;
        if (finalDamage <= 0) finalDamage = 1;

        currentHealth -= finalDamage;

        // 1. CAMERA SHAKE
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.TriggerShake(shakeDuration, shakeForce);
        }

        // 2. KNOCKBACK
        if (playerMove != null)
        {
            playerMove.ApplyKnockback(hitSourcePosition, knockbackForce, knockbackTime);
        }

        // 3. ANIMACIÓN TILT (Inclinación)
        if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
        tiltCoroutine = StartCoroutine(TiltRoutine());

        // 4. --- NUEVO: FLASH DE MATERIAL ---
        if (playerRenderer != null && originalMaterial != null && hitMaterial != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        Debug.Log($"Jugador recibió {finalDamage} daño.");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    // --- CORRUTINA DE FLASH (Igual que la del enemigo) ---
    private IEnumerator FlashRoutine()
    {
        float elapsedTime = 0f;
        float halfDuration = flashDuration / 2f; 

        // Fase 1: Ir al color de golpe
        while (elapsedTime < halfDuration)
        {
            playerRenderer.material.Lerp(originalMaterial, hitMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        playerRenderer.material = hitMaterial;

        // Fase 2: Volver al original
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            playerRenderer.material.Lerp(hitMaterial, originalMaterial, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Restauración forzosa para no romper shaders
        playerRenderer.material = originalMaterial;
        flashCoroutine = null;
    }

    private IEnumerator TiltRoutine()
    {
        Quaternion originalRot = transform.rotation;
        // Inclinación en eje X local
        Quaternion targetRot = originalRot * Quaternion.Euler(-tiltAngle, 0, 0);

        float t = 0f;
        // Ida rápida
        while (t < 1f)
        {
            t += Time.deltaTime * tiltSpeed;
            transform.rotation = Quaternion.Lerp(originalRot, targetRot, t);
            yield return null;
        }

        t = 0f;
        // Vuelta suave
        while (t < 1f)
        {
            t += Time.deltaTime * (tiltSpeed / 2f);
            transform.rotation = Quaternion.Lerp(targetRot, originalRot, t);
            yield return null;
        }
        // Asegurar rotación final limpia
        transform.rotation = originalRot;
    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * levelMultiplier);
        currentHealth = maxHealth; 
        Debug.Log("¡NIVEL SUBIDO! Ahora eres nivel " + currentLevel);
    }

    public void Die()
    {
        isAlive = false;
        Debug.Log("GAME OVER");
        if (playerMove != null) playerMove.enabled = false;
        
        // Efecto visual de muerte (Opcional: quedarse tirado en el suelo)
        // transform.Rotate(-90, 0, 0); 
    }
}