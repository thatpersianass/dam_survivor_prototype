using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] public GameObject attackPrefab; 
    [SerializeField] public Transform attackPoint;
    [SerializeField] public float attackRatio = 1f; 

    void Start()
    {
        if (attackPrefab != null && attackPoint != null)
        {
            StartCoroutine(AutoAttackRoutine());
        }
        else
        {
            Debug.LogError("Error: Asigna el Prefab y el Attack Point en el Inspector.");
        }
    }

    public IEnumerator AutoAttackRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        
        while (true)
        {
            GameObject instantiatedAttack = Instantiate(
                attackPrefab, 
                attackPoint.position, 
                attackPoint.rotation, 
                attackPoint
            );

            yield return new WaitForSeconds(attackRatio);
        }
    }
}