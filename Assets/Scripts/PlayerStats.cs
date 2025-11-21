using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("--- Supervivencia ---")]
    public int maxHealth = 100;
    public int currentHealth;
    public int defense = 0;
    public float recovery = 0f;
    public bool isAlive = true;

    [Header("--- Progresión y Nivel ---")]
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int experienceToNextLevel = 100;
    [Tooltip("Cuánto aumenta la XP necesaria por nivel (1.2 = 20% más difícil cada vez)")]
    [SerializeField] private float levelCapMultiplier = 1.2f; 

    [Header("--- Atributos de Combate ---")]
    public float moveSpeed = 5f;
    public int might = 1;         // Multiplicador de daño (Empiezas con 100% de daño)
    public float cooldownReduction = 0f;
    private void Awake()
    {
        currentHealth = maxHealth;
        isAlive = true;
    }

    private void Update()
    {
        if (recovery > 0 && currentHealth < maxHealth && isAlive)
        {
            // Lógica simple de regeneración (se puede mejorar con corrutinas)
            currentHealth += (int)(recovery * Time.deltaTime); 
        }
    }
    public void TakeDamage(int dmg)
    {
        if (!isAlive) return;
        int finalDamage = dmg - defense;
        if (finalDamage <= 0) finalDamage = 1;

        currentHealth -= finalDamage;

        // Debug.Log($"Jugador recibió {finalDamage} daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void RestoreHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
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
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * levelCapMultiplier);

        // Debug.Log("¡NIVEL SUBIDO! Nivel actual: " + currentLevel);
    }

    private void Die()
    {
        isAlive = false;
        currentHealth = 0;
        Debug.Log("GAME OVER");
        GetComponent<PlayerMove>().enabled = false;
    }
}