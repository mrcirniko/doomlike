using UnityEngine;
using Doomlike.Core;
using Doomlike.Enemies;

namespace Doomlike.Pickups
{
    [RequireComponent(typeof(EnemyBase))]
    public class LootDropper : MonoBehaviour
    {
        [System.Serializable]
        public struct DropEntry
        {
            public GameObject prefab;
            [Range(0f, 1f)] public float chance;
        }

        [SerializeField] DropEntry[] drops;

        EnemyBase enemy;

        void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            enemy.Killed += OnKilled;
        }

        void OnDestroy()
        {
            if (enemy != null) enemy.Killed -= OnKilled;
        }

        void OnKilled(EnemyBase e, DamageInfo info)
        {
            if (drops == null) return;
            foreach (var d in drops)
            {
                if (d.prefab == null) continue;
                if (Random.value <= d.chance)
                {
                    Instantiate(d.prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                }
            }
        }
    }
}
