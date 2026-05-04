using System;
using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    public class MeleeEnemy : EnemyBase
    {
        public enum BehaviorState { Idle, Chase, Attack }

        [Header("Vision")]
        [Tooltip("Player must be within this distance for the zombie to wake up.")]
        [SerializeField] float visionRange = 10f;
        [Tooltip("Once aggroed, the zombie stays chasing until the player is further than this.")]
        [SerializeField] float deAggroRange = 16f;
        [SerializeField] bool requireLineOfSight = false;
        [SerializeField] LayerMask sightObstructionMask = ~0;

        [Header("Combat")]
        [SerializeField] float attackRange = 2.2f;
        [SerializeField] float attackCooldown = 1.6f;
        [SerializeField] float attackDamage = 15f;
        [SerializeField] float attackHitRadius = 1.6f;
        [Tooltip("Delay between attack animation start and the actual damage check (sync with the punch impact frame).")]
        [SerializeField] float attackImpactDelay = 0.45f;

        [Header("Idle behaviour")]
        [SerializeField] float minIdleVariationDelay = 15f;
        [SerializeField] float maxIdleVariationDelay = 35f;
        [Tooltip("Speed multiplier while idle so we never animate the walk in place.")]
        [SerializeField] float idleAgentSpeed = 0f;

        [Header("Movement")]
        [SerializeField] float chaseAgentSpeed = 2.0f;
        [SerializeField] float aimTurnSpeed = 4f;

        [Header("Audio")]
        [Tooltip("Played when the swing starts (animation begins).")]
        [SerializeField] AudioClip attackSwingClip;
        [Tooltip("Played at the impact frame, when damage is actually dealt.")]
        [SerializeField] AudioClip attackHitClip;
        [Tooltip("Occasional zombie growl/agony sound while idle.")]
        [SerializeField] AudioClip idleVariationClip;
        [SerializeField, Range(0f, 1f)] float audioVolume = 0.7f;
        [SerializeField, Range(0f, 0.5f)] float audioPitchVariance = 0.1f;

        // events for the animator
        public event Action AttackTriggered;
        public event Action IdleVariationTriggered;
        public event Action IdleEnded;

        public BehaviorState State { get; private set; }
        public bool IsAttacking => attackPending;
        public Vector3 LocalVelocity { get; private set; }

        bool isAggroed;
        float attackCdTimer;
        float attackImpactTimer;
        bool attackPending;
        float idleVariationTimer;

        protected override void Awake()
        {
            base.Awake();
            if (agent != null)
            {
                agent.updateRotation = false;
                agent.speed = chaseAgentSpeed;
            }
            idleVariationTimer = UnityEngine.Random.Range(minIdleVariationDelay, maxIdleVariationDelay);
        }

        protected override void UpdateBehavior()
        {
            Vector3 toPlayer = player.position - transform.position;
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float dist = flat.magnitude;
            if (dist < 0.001f) return;

            // Aggro hysteresis: see player at visionRange, lose aggro past deAggroRange.
            bool seePlayer = CanSeePlayer(dist);
            if (!isAggroed && seePlayer && dist <= visionRange) isAggroed = true;
            else if (isAggroed && dist > deAggroRange) isAggroed = false;

            if (!isAggroed)
            {
                EnterIdle();
            }
            else if (dist <= attackRange)
            {
                EnterAttack();
            }
            else
            {
                EnterChase(flat / dist);
            }

            LocalVelocity = transform.InverseTransformDirection(agent.velocity);
        }

        bool CanSeePlayer(float dist)
        {
            if (dist > visionRange) return false;
            if (!requireLineOfSight) return true;

            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1f;
            Vector3 dir = target - origin;
            float d = dir.magnitude;
            if (d < 0.01f) return true;
            dir /= d;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, d, sightObstructionMask, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            return true;
        }

        void EnterIdle()
        {
            bool wasOther = State != BehaviorState.Idle;
            State = BehaviorState.Idle;
            agent.speed = idleAgentSpeed;
            agent.ResetPath();
            attackPending = false;

            // Reset variation timer when we just entered idle, so a freshly-idled zombie
            // doesn't immediately fire Agonize while still in transition.
            if (wasOther)
                idleVariationTimer = UnityEngine.Random.Range(minIdleVariationDelay, maxIdleVariationDelay);

            idleVariationTimer -= Time.deltaTime;
            if (idleVariationTimer <= 0f)
            {
                idleVariationTimer = UnityEngine.Random.Range(minIdleVariationDelay, maxIdleVariationDelay);
                IdleVariationTriggered?.Invoke();
                Doomlike.Core.AudioOneShot.PlayAt(idleVariationClip, transform.position, audioVolume, audioPitchVariance);
            }
        }

        void EnterChase(Vector3 dirToPlayer)
        {
            bool leftIdle = State == BehaviorState.Idle;
            State = BehaviorState.Chase;
            agent.speed = chaseAgentSpeed;
            agent.SetDestination(player.position);
            FaceTowards(dirToPlayer);
            if (attackCdTimer > 0f) attackCdTimer -= Time.deltaTime;
            attackPending = false;
            if (leftIdle) IdleEnded?.Invoke();
        }

        void EnterAttack()
        {
            bool leftIdle = State == BehaviorState.Idle;
            State = BehaviorState.Attack;
            agent.ResetPath();
            agent.speed = idleAgentSpeed;
            if (leftIdle) IdleEnded?.Invoke();

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) FaceTowards(toPlayer.normalized);

            if (attackCdTimer > 0f) attackCdTimer -= Time.deltaTime;

            if (attackCdTimer <= 0f && !attackPending)
            {
                attackCdTimer = attackCooldown;
                attackPending = true;
                attackImpactTimer = attackImpactDelay;
                AttackTriggered?.Invoke();
                Doomlike.Core.AudioOneShot.PlayAt(attackSwingClip, transform.position, audioVolume, audioPitchVariance);
            }

            if (attackPending)
            {
                attackImpactTimer -= Time.deltaTime;
                if (attackImpactTimer <= 0f)
                {
                    attackPending = false;
                    DoAttackHit();
                }
            }
        }

        void FaceTowards(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * aimTurnSpeed);
        }

        void DoAttackHit()
        {
            Vector3 forward = transform.forward;
            Vector3 hitCenter = transform.position + forward * attackRange * 0.6f + Vector3.up;
            Doomlike.Core.AudioOneShot.PlayAt(attackHitClip, hitCenter, audioVolume, audioPitchVariance);
            Collider[] hits = Physics.OverlapSphere(hitCenter, attackHitRadius);
            foreach (var c in hits)
            {
                var ph = c.GetComponentInParent<Doomlike.Player.PlayerHealth>();
                if (ph != null)
                {
                    ph.ApplyDamage(new DamageInfo(attackDamage, DamageType.Melee, gameObject,
                        c.transform.position, -forward, forward));
                    return;
                }
            }
        }
    }
}
