using UnityEngine;
using UnityEngine.UI; 
using System.Collections; // ¡NECESARIO PARA LAS CORRUTINAS!

public class GameHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text levelNumberText; 

    private PlayerStats playerStats;
    private float animationSpeed = 5f; 
    
    // Controla la Corrutina de Level Up
    private Coroutine xpCoroutine; 

    void Start()
    {
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
    }

    void Update()
    {
        if (playerStats != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Si ya estamos manejando el reset visual, salimos.
        if (xpCoroutine != null) return; 

        // 1. Vida (Sigue con animación suave)
        if (playerStats.maxHealth > 0)
        {
            float targetHp = (float)playerStats.currentHealth / (float)playerStats.maxHealth;
            hpSlider.value = Mathf.Lerp(hpSlider.value, targetHp, animationSpeed * Time.deltaTime);
        }

        // 2. Experiencia: Calculamos el destino
        float targetXp = 0f;
        if (playerStats.experienceToNextLevel > 0)
        {
            targetXp = (float)playerStats.currentExperience / (float)playerStats.experienceToNextLevel;
        }

        // 3. NUEVA LÓGICA DE RESET VISUAL 💡
        // Si la barra está casi llena (> 95%) Y el destino es un valor bajo (ej. 5%), significa que subimos de nivel.
        if (xpSlider.value > 0.95f && targetXp < 0.5f)
        {
            // Detenemos el Update y empezamos la Corrutina de transición
            xpCoroutine = StartCoroutine(HandleLevelReset(targetXp));
            return; 
        }

        // 4. Aplicamos la animación normal
        xpSlider.value = Mathf.Lerp(xpSlider.value, targetXp, animationSpeed * Time.deltaTime);

        // 5. Texto
        if (levelNumberText != null)
        {
            levelNumberText.text = playerStats.currentLevel.ToString();
        }
    }

    // Método que se ejecuta en paralelo para gestionar el reset suave
    private IEnumerator HandleLevelReset(float newTargetXp)
    {
        // 1. Terminamos de llenar la barra visualmente (Si no está al 100%)
        while (xpSlider.value < 1f)
        {
            xpSlider.value = Mathf.Lerp(xpSlider.value, 1f, animationSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        // 2. Pausa breve (Efecto "Flash" o transición)
        yield return new WaitForSeconds(0.05f); 
        
        // 3. Snap the bar to the new low value (el excedente)
        // La barra pasa de 100% a 5% (el overflow)
        xpSlider.value = newTargetXp; 
        
        // 4. Espera a que el Lerp normal tome el control.
        xpCoroutine = null;
    }
}