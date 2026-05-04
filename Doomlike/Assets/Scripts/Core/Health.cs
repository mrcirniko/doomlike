using System;
using UnityEngine;

namespace Doomlike.Core
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] bool destroyOnDeath = true;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Killed;

        void Awake() => Current = maxHealth;

        public virtual void ApplyDamage(in DamageInfo info)
        {
            if (IsDead) return;
            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info);
            if (Current <= 0f)
            {
                IsDead = true;
                Killed?.Invoke(info);
                if (destroyOnDeath) Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
