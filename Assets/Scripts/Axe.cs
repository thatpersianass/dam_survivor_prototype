using UnityEngine;

public class Axe : MonoBehaviour
{
    [Header("Datos del Hacha")]
    public float speed = 10f;
    public float lifeTime = 5f;
    public int damage = 25;

    // Update is called once per frame
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Enemy"))
        {
            EnemyMovement enemy = other.GetComponent<EnemyMovement>();
            if (enemy != null)
            {
                enemy.RecieveDmg(damage);
            }
        }
    }

}