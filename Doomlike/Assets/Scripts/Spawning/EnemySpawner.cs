using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doomlike.Core;
using Doomlike.Enemies;

namespace Doomlike.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] WaveDefinition[] waves;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] float perSpawnDelay = 0.25f;
        [SerializeField] bool loopFinalWave;
        [Tooltip("Spawn points with a living enemy within this radius are skipped.")]
        [SerializeField] float minSpawnSpacing = 3f;
        [Tooltip("Random horizontal offset added to each spawn so enemies don't stack on the exact spot.")]
        [SerializeField] float spawnJitter = 0.5f;

        readonly List<EnemyBase> alive = new();

        public int CurrentWaveIndex { get; private set; } = -1;
        public int AliveCount => alive.Count;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;

        IEnumerator Start()
        {
            yield return null;
            yield return RunWaves();
        }

        IEnumerator RunWaves()
        {
            if (waves == null || waves.Length == 0) yield break;
            for (int i = 0; i < waves.Length; i++)
            {
                CurrentWaveIndex = i;
                yield return new WaitForSeconds(waves[i].delayBeforeStart);
                WaveStarted?.Invoke(i);
                yield return SpawnWave(waves[i]);
                while (alive.Count > 0) yield return null;
                WaveCompleted?.Invoke(i);
            }

            if (loopFinalWave && waves.Length > 0)
            {
                while (true)
                {
                    yield return new WaitForSeconds(2f);
                    yield return SpawnWave(waves[waves.Length - 1]);
                    while (alive.Count > 0) yield return null;
                }
            }

            AllWavesCompleted?.Invoke();
        }

        IEnumerator SpawnWave(WaveDefinition wave)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) yield break;
            foreach (var entry in wave.entries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    Spawn(entry.prefab);
                    yield return new WaitForSeconds(perSpawnDelay);
                }
            }
        }

        void Spawn(EnemyBase prefab)
        {
            if (prefab == null) return;
            Transform sp = PickSpawnPoint();
            if (sp == null) return;

            Vector3 pos = sp.position;
            if (spawnJitter > 0f)
            {
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnJitter;
                pos += new Vector3(jitter.x, 0f, jitter.y);
            }

            var enemy = Instantiate(prefab, pos, sp.rotation);
            alive.Add(enemy);
            enemy.Killed += OnEnemyKilled;
        }

        Transform PickSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return null;
            // Filter points that don't have a living enemy too close.
            var available = new List<Transform>(spawnPoints.Length);
            float sqr = minSpawnSpacing * minSpawnSpacing;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                if (sp == null) continue;
                bool busy = false;
                for (int j = 0; j < alive.Count; j++)
                {
                    var e = alive[j];
                    if (e == null) continue;
                    if ((e.transform.position - sp.position).sqrMagnitude < sqr)
                    {
                        busy = true;
                        break;
                    }
                }
                if (!busy) available.Add(sp);
            }
            if (available.Count == 0)
                return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return available[UnityEngine.Random.Range(0, available.Count)];
        }

        void OnEnemyKilled(EnemyBase enemy, DamageInfo info)
        {
            enemy.Killed -= OnEnemyKilled;
            alive.Remove(enemy);
        }
    }
}
