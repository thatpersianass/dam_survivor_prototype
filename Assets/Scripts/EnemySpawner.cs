using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius;

    [Header("GameObjects")]
    [SerializeField] private Transform Player;

    [Header("Waves")]
    [SerializeField] List<WaveData> waves;

    void Start()
    {
        StartCoroutine(GenerateWaves());
    }

    private IEnumerator SpawnEnemy(WaveData wave)
    {
        int spawned = 0;
        while (spawned < wave.EnemyCount)
        {
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = Player.position + new Vector3(randomPoint.x, 0f, randomPoint.y);
            Instantiate(wave.EnemyPrefab, spawnPosition, Quaternion.identity);

            spawned++;
            yield return new WaitForSeconds(wave.SpawnInterval);
        }
    }

    private IEnumerator GenerateWaves()
    {
        foreach (WaveData wave in waves)
        {
            // Espera a que termine de spawnear todos los enemigos de esta wave
            yield return StartCoroutine(SpawnEnemy(wave));

            // Espera adicional opcional: wave.WaveDuration para mantener la wave activa
            if (wave.WaveDuration > 0)
            {
                yield return new WaitForSeconds(wave.WaveDuration);
            }
        }

        Debug.Log("Todas las waves han terminado.");
    }
}
