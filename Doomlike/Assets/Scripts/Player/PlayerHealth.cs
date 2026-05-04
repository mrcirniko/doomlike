using System;
using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxArmor = 100f;
        [SerializeField, Range(0f, 1f)] float armorAbsorption = 0.5f;

        [Header("Audio")]
        [SerializeField] AudioClip damageClip;
        [SerializeField] AudioClip armorHitClip;
        [SerializeField] AudioClip lowHealthClip;
        [SerializeField] AudioClip deathClip;
        [SerializeField, Range(0f, 1f)] float audioVolume = 0.7f;
        [SerializeField, Range(0f, 0.5f)] float audioPitchVariance = 0.08f;
        [Tooltip("Trigger lowHealthClip when health drops to this fraction or below.")]
        [SerializeField, Range(0f, 1f)] float lowHealthThreshold = 0.25f;
        bool lowHealthPlayed;

        public float MaxHealth => maxHealth;
        public float MaxArmor => maxArmor;
        public float Health { get; private set; }
        public float Armor { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get; set; }

        public event Action<float, float> Changed;
        public event Action Died;

        void Awake()
        {
            Health = maxHealth;
            Armor = 0f;
        }

        void Start()
        {
            Changed?.Invoke(Health, Armor);
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (IsDead || IsInvulnerable) return;

            float remaining = info.Amount;
            bool armorAbsorbedSomething = false;
            if (Armor > 0f)
            {
                float absorbed = remaining * armorAbsorption;
                float armorTaken = Mathf.Min(Armor, absorbed);
                Armor -= armorTaken;
                remaining -= armorTaken;
                if (armorTaken > 0f) armorAbsorbedSomething = true;
            }

            Health = Mathf.Max(0f, Health - remaining);
            Changed?.Invoke(Health, Armor);

            // play damage / armor hit sound
            if (armorAbsorbedSomething && armorHitClip != null)
                AudioOneShot.PlayAt(armorHitClip, transform.position, audioVolume, audioPitchVariance, spatialBlend: 0f);
            else if (damageClip != null)
                AudioOneShot.PlayAt(damageClip, transform.position, audioVolume, audioPitchVariance, spatialBlend: 0f);

            // low health warning (one-shot — once per low-health bracket)
            float frac = Health / Mathf.Max(0.01f, maxHealth);
            if (!lowHealthPlayed && frac > 0f && frac <= lowHealthThreshold)
            {
                lowHealthPlayed = true;
                AudioOneShot.PlayAt(lowHealthClip, transform.position, audioVolume, 0f, spatialBlend: 0f);
            }
            if (frac > lowHealthThreshold) lowHealthPlayed = false;

            if (Health <= 0f)
            {
                IsDead = true;
                AudioOneShot.PlayAt(deathClip, transform.position, audioVolume, 0f, spatialBlend: 0f);
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Health = Mathf.Min(maxHealth, Health + amount);
            Changed?.Invoke(Health, Armor);
        }

        public void AddArmor(float amount)
        {
            if (IsDead) return;
            Armor = Mathf.Min(maxArmor, Armor + amount);
            Changed?.Invoke(Health, Armor);
        }
    }
}
