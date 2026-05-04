using UnityEngine;
using UnityEngine.AI;

namespace Doomlike.Enemies
{
    public class RangedEnemy : EnemyBase
    {
        [Header("Ranged")]
        [SerializeField] protected float preferredMin = 8f;
        [SerializeField] protected float preferredMax = 14f;
        [SerializeField] protected float attackRange = 18f;
        [SerializeField] protected float fireCooldown = 1.6f;
        [SerializeField] protected EnemyProjectile projectilePrefab;
        [SerializeField] protected Transform muzzle;
        [SerializeField] protected float projectileSpeed = 18f;
        [SerializeField] protected float projectileDamage = 12f;

        [Header("Strafing")]
        [SerializeField] protected float strafeSwitchInterval = 2.5f;
        [SerializeField] protected float repathInterval = 0.25f;
        [SerializeField] protected float aimTurnSpeed = 8f;

        protected float fireTimer;
        float strafeSwitchTimer;
        float repathTimer;
        int strafeDir = 1;

        public Vector3 LocalVelocity { get; private set; }
        public bool IsAttacking => fireTimer > fireCooldown - 0.15f;

        protected override void Awake()
        {
            base.Awake();
            strafeDir = Random.value > 0.5f ? 1 : -1;
            if (agent != null)
            {
                agent.updateRotation = false;
                agent.autoBraking = false;
            }
        }

        protected override void UpdateBehavior()
        {
            Vector3 toPlayer = player.position - transform.position;
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float dist = flat.magnitude;
            if (dist < 0.001f) return;
            Vector3 dirToPlayer = flat / dist;

            // strafe direction switch (independent of band)
            strafeSwitchTimer -= Time.deltaTime;
            if (strafeSwitchTimer <= 0f)
            {
                strafeSwitchTimer = strafeSwitchInterval * Random.Range(0.7f, 1.3f);
                strafeDir = -strafeDir;
            }

            // repath at fixed cadence — avoids per-frame destination thrashing
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                Vector3 desired = ComputeDesiredPoint(dirToPlayer, dist);
                if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(desired);
            }

            // face the player
            Quaternion want = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * aimTurnSpeed);

            // local velocity for animator
            LocalVelocity = transform.InverseTransformDirection(agent.velocity);

            // shoot
            if (fireTimer > 0f) fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f && dist <= attackRange) Shoot();
        }

        Vector3 ComputeDesiredPoint(Vector3 dirToPlayer, float dist)
        {
            // Stable orbit point relative to PLAYER, not to current self position.
            // Distance picked inside [preferredMin, preferredMax]; angle offset based on strafe dir.
            float targetDist = Mathf.Clamp((preferredMin + preferredMax) * 0.5f, preferredMin, preferredMax);

            // Angular offset from "directly behind us along player direction" — this gives the
            // orbit a forward bias (we tend to step sideways while staying at targetDist).
            float angle = strafeDir * 35f * Mathf.Deg2Rad;
            Vector3 offsetDir = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f) * (-dirToPlayer);
            return player.position + offsetDir * targetDist;
        }

        protected virtual void Shoot()
        {
            fireTimer = fireCooldown;
            if (projectilePrefab == null) return;
            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up;
            Vector3 dir = (player.position + Vector3.up - from).normalized;
            var p = Instantiate(projectilePrefab, from, Quaternion.LookRotation(dir));
            p.Launch(dir * projectileSpeed, projectileDamage, gameObject);
        }
    }
}
