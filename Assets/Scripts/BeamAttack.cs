using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeamAttack : MonoBehaviour
{
    [Header("Damage Over Time Settings")]
    [SerializeField] private float lifeTime;
    [SerializeField] private float damageInterval;
    [SerializeField] private int damagePerTick;

    private HashSet<EnemyMovement> enemiesInside = new HashSet<EnemyMovement>();
    private Coroutine damageCoroutine;

    void Start()
    {
        Destroy(gameObject, lifeTime); 
        damageCoroutine = StartCoroutine(ApplyDmg());
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyMovement enemy = other.GetComponent<EnemyMovement>();

        if (enemy != null)
        {
            enemiesInside.Add(enemy);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        EnemyMovement enemy = other.GetComponent<EnemyMovement>();

        if (enemy != null)
        {
            enemiesInside.Remove(enemy);
        }
    }

    private IEnumerator ApplyDmg()
    {
        while (true)
        {
            yield return new WaitForSeconds(damageInterval); 

            EnemyMovement[] targets = new EnemyMovement[enemiesInside.Count];
            enemiesInside.CopyTo(targets);

            foreach (EnemyMovement enemy in targets)
            {
                if (enemy != null)
                {
                    enemy.RecieveDmg(damagePerTick);
                }
                else
                {
                    enemiesInside.Remove(enemy);
                }
            }
        }
    }
}