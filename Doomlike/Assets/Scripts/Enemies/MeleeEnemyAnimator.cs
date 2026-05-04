using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    /// <summary>
    /// Drives an Animator on a melee zombie from a MeleeEnemy script.
    /// Expected Animator parameters:
    ///   - Float "Speed" (0..1, drives Idle ↔ Walk blend)
    ///   - Trigger "Attack" — fires on each punch
    ///   - Trigger "Agonize" — fires occasionally while idle
    ///   - Trigger "Die"
    /// </summary>
    [RequireComponent(typeof(MeleeEnemy))]
    public class MeleeEnemyAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float speedReference = 2.5f;
        [SerializeField] float damping = 0.15f;

        [Header("Param names")]
        [SerializeField] string speedParam = "Speed";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] string agonizeTrigger = "Agonize";
        [SerializeField] string dieTrigger = "Die";

        [Header("State names")]
        [Tooltip("Animator state to fall back to when Idle ends (the locomotion blend tree).")]
        [SerializeField] string locomotionState = "Locomotion";
        [SerializeField] float idleEndCrossfade = 0.15f;

        MeleeEnemy enemy;
        int hashSpeed, hashAttack, hashAgonize, hashDie;

        void Awake()
        {
            enemy = GetComponent<MeleeEnemy>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            hashSpeed = Animator.StringToHash(speedParam);
            hashAttack = Animator.StringToHash(attackTrigger);
            hashAgonize = Animator.StringToHash(agonizeTrigger);
            hashDie = Animator.StringToHash(dieTrigger);
        }

        void OnEnable()
        {
            enemy.Killed += OnKilled;
            enemy.AttackTriggered += OnAttack;
            enemy.IdleVariationTriggered += OnAgonize;
            enemy.IdleEnded += OnIdleEnded;
        }

        void OnDisable()
        {
            enemy.Killed -= OnKilled;
            enemy.AttackTriggered -= OnAttack;
            enemy.IdleVariationTriggered -= OnAgonize;
            enemy.IdleEnded -= OnIdleEnded;
        }

        void OnIdleEnded()
        {
            if (animator == null) return;
            // Force a transition to Locomotion — interrupts Agonize / any other idle variation.
            animator.CrossFadeInFixedTime(locomotionState, idleEndCrossfade);
        }

        void Update()
        {
            if (animator == null || enemy.IsDead) return;

            float speed = enemy.LocalVelocity.magnitude / Mathf.Max(0.01f, speedReference);
            speed = Mathf.Clamp01(speed);
            animator.SetFloat(hashSpeed, speed, damping, Time.deltaTime);
        }

        void OnKilled(EnemyBase _, DamageInfo info)
        {
            if (animator != null) animator.SetTrigger(hashDie);
        }

        void OnAttack()
        {
            if (animator != null) animator.SetTrigger(hashAttack);
        }

        void OnAgonize()
        {
            if (animator != null) animator.SetTrigger(hashAgonize);
        }
    }
}
