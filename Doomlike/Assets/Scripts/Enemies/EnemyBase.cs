using System;
using UnityEngine;
using UnityEngine.AI;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] protected float maxHealth = 60f;
        [SerializeField] protected float sightRange = 30f;
        [SerializeField] protected bool destroyOnDeath = true;
        [SerializeField] protected float deathDelay = 0f;

        protected NavMeshAgent agent;
        protected Transform player;
        protected float currentHealth;

        public bool IsDead { get; protected set; }
        public Transform Player => player;

        public event Action<EnemyBase, DamageInfo> Killed;
        public event Action<EnemyBase, DamageInfo> Damaged;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        protected virtual void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        protected virtual void Update()
        {
            if (IsDead || player == null) return;
            UpdateBehavior();
        }

        protected abstract void UpdateBehavior();

        public virtual void ApplyDamage(in DamageInfo info)
        {
            if (IsDead) return;
            currentHealth -= info.Amount;
            Damaged?.Invoke(this, info);
            if (currentHealth <= 0f) Die(info);
        }

        protected virtual void Die(DamageInfo info)
        {
            IsDead = true;
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
            Killed?.Invoke(this, info);
            if (destroyOnDeath) Destroy(gameObject, deathDelay);
        }
    }
}
