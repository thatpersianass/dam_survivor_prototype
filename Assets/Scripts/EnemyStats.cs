using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Stats/EnemyStats", order = 0)]
public class EnemyStats : ScriptableObject {
    public int MaxHP;
    public int Damage;
    public int Defense;
    public float Speed;

}