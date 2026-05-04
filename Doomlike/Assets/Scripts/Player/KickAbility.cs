using UnityEngine;
using Doomlike.Core;
using Doomlike.Enemies;

namespace Doomlike.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class KickAbility : MonoBehaviour
    {
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] float lungeSpeed = 35f;
        [SerializeField] float maxLungeTime = 0.5f;
        [SerializeField] float kickRange = 2.5f;
        [SerializeField] float launchSpeed = 30f;
        [SerializeField] float kickDamage = 50f;
        [SerializeField] float cooldown = 3f;

        PlayerController player;
        PlayerInputReader input;
        PlayerHealth health;

        float cooldownTimer;
        bool lunging;
        float lungeTimer;
        Transform target;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<PlayerHealth>();
        }

        void OnEnable() => input.KickPressed += TryKick;
        void OnDisable() => input.KickPressed -= TryKick;

        void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (!lunging) return;

            lungeTimer -= Time.deltaTime;
            if (target == null || lungeTimer <= 0f)
            {
                EndLunge();
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist <= kickRange)
            {
                LaunchTarget();
                EndLunge();
                return;
            }

            Vector3 dir = toTarget.normalized;
            player.SetExternalVelocity(dir * lungeSpeed, useGravity: false);
        }

        void TryKick()
        {
            if (cooldownTimer > 0f || lunging) return;
            if (lockOn == null || !lockOn.IsActive) return;

            target = lockOn.CurrentTarget;
            lunging = true;
            lungeTimer = maxLungeTime;
            cooldownTimer = cooldown;
            if (health != null) health.IsInvulnerable = true;
        }

        void LaunchTarget()
        {
            if (target == null) return;
            Vector3 dir = (target.position - transform.position).normalized;
            var enemy = target.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                var kicked = target.gameObject.AddComponent<KickedEnemy>();
                kicked.Init(dir * launchSpeed, kickDamage, gameObject);
            }
            // direct damage to the kicked target as well
            var dmg = target.GetComponent<IDamageable>();
            dmg?.ApplyDamage(new DamageInfo(kickDamage, DamageType.Kick, gameObject,
                target.position, -dir, dir));
        }

        void EndLunge()
        {
            lunging = false;
            target = null;
            player.ClearExternalVelocity();
            if (health != null) health.IsInvulnerable = false;
        }
    }
}
