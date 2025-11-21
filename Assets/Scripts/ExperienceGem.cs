using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [Header("Configuración de Valor")]
    [SerializeField] private int expAmount = 1;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float initialSpeed = 12f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float turnSpeed = 5f;         
    [SerializeField] private float pickupDistance = 1.5f;
    
    [Tooltip("Distancia a la que la gema deja de curvar y va directo al jugador")]
    [SerializeField] private float lockOnDistance = 4.0f; 

    [Tooltip("Ajuste de altura para apuntar al pecho")]
    [SerializeField] private float heightOffset = 1.0f; 

    private bool isMagnetized = false;
    private Transform playerTransform;
    private float currentSpeed;
    private Vector3 moveDirection;

    public void SetAmount(int amount)
    {
        expAmount = amount;
    }

    private void Update()
    {
        if (isMagnetized && playerTransform != null)
        {
            FlyToPlayer();
        }
    }

    private void FlyToPlayer()
    {
        // 1. Aceleración constante (Sin frenos)
        currentSpeed += acceleration * Time.deltaTime;

        // Calcular posiciones
        Vector3 targetPosition = playerTransform.position + Vector3.up * heightOffset;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // 2. PREDICCIÓN DE IMPACTO (Solución al problema de "Atravesar" al player)
        // Calculamos cuánto nos moveremos en este frame
        float moveDistanceThisFrame = currentSpeed * Time.deltaTime;

        // Si la distancia al objetivo es menor que lo que nos vamos a mover,
        // significa que chocaríamos en este frame. Recogemos YA.
        if (distanceToTarget <= moveDistanceThisFrame || distanceToTarget <= pickupDistance)
        {
            Collect();
            return; // Cortamos aquí para no movernos más
        }

        // 3. Lógica de Giro (Solución al problema de "Orbitar")
        if (distanceToTarget < lockOnDistance)
        {
            // ESTAMOS CERCA: Dejamos de curvar suavemente y fijamos la dirección exacta.
            // Esto evita que de vueltas alrededor.
            moveDirection = directionToTarget;
        }
        else
        {
            // ESTAMOS LEJOS: Usamos Slerp para la curva bonita.
            moveDirection = Vector3.Slerp(moveDirection, directionToTarget, turnSpeed * Time.deltaTime);
        }

        // 4. Aplicar movimiento
        transform.position += moveDirection * moveDistanceThisFrame;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isMagnetized)
        {
            isMagnetized = true;
            playerTransform = other.transform;
            currentSpeed = initialSpeed;
            
            // Dirección inicial hacia el jugador para evitar giros raros al inicio
            moveDirection = (other.transform.position - transform.position).normalized;
        }
    }

    private void Collect()
    {
        if (playerTransform != null)
        {
            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();
            if (stats != null)
            {
                // Llama a mi función AddExperience
                stats.AddExperience(expAmount); 
            }
        }
        Destroy(gameObject);
    }
}