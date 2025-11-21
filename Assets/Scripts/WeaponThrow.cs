using System.Collections;
using UnityEngine;

public class WeaponThrow : MonoBehaviour
{
    public GameObject weaponPrefab;
    public float shootRatio = 1f; // Armas por segundo
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(shootWeapon());
    }

    public IEnumerator shootWeapon()
    {
        while (true)
        {
            Instantiate(weaponPrefab, transform.position, transform.rotation);
            yield return new WaitForSeconds(shootRatio);
        }
    }
}