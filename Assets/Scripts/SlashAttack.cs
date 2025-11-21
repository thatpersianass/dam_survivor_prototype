using UnityEngine;

public class SlashAttack : MonoBehaviour
{
    [SerializeField]public float lifeTime = 0.2f;
    [SerializeField]public int damage = 10;
    private System.Collections.Generic.HashSet<GameObject> enemiesHit = new System.Collections.Generic.HashSet<GameObject>();
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {

        if (enemiesHit.Contains(other.gameObject))
        {
            return;
        }

        EnemyMovement Enemy = other.GetComponent<EnemyMovement>();

        if (Enemy != null)
        {
            Enemy.RecieveDmg(damage);
            // Debug.Log("slash golpeó a " + other.name + " e infligió " + damage + " de daño.");
            enemiesHit.Add(other.gameObject);
        }
    }
}