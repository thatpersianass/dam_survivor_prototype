using UnityEngine;

[CreateAssetMenu(fileName = "NewWave", menuName = "Waves")]

public class WaveData : ScriptableObject
{
    [Header("Wave Settings")]
    public GameObject EnemyPrefab;
    public float SpawnInterval;
    public int EnemyCount;
    public float WaveDuration;
}
