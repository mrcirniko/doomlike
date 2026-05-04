using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    /// <summary>
    /// Drives an Animator on a ranged enemy from its NavMeshAgent velocity.
    /// Expects the controller to have:
    ///   - Float "MoveX" (-1..1, +1 = strafe right)
    ///   - Float "MoveZ" (-1..1, +1 = walk forward toward player)
    ///   - Trigger "Die"
    /// Use a 2D Freeform Cartesian blend tree on (MoveX, MoveZ) with motions at
    /// (0,1)=Pistol Walk, (0,-1)=Pistol Walk Backward, (1,0)=Pistol Strafe (mirrored), (-1,0)=Pistol Strafe.
    /// </summary>
    [RequireComponent(typeof(RangedEnemy))]
    public class RangedEnemyAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float damping = 0.15f;
        [SerializeField] string moveXParam = "MoveX";
        [SerializeField] string moveZParam = "MoveZ";
        [SerializeField] string dieTrigger = "Die";

        RangedEnemy enemy;
        float refMoveX, refMoveZ;
        int hashX, hashZ, hashDie;

        void Awake()
        {
            enemy = GetComponent<RangedEnemy>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            hashX = Animator.StringToHash(moveXParam);
            hashZ = Animator.StringToHash(moveZParam);
            hashDie = Animator.StringToHash(dieTrigger);
        }

        void OnEnable() { enemy.Killed += OnKilled; }
        void OnDisable() { enemy.Killed -= OnKilled; }

        void Update()
        {
            if (animator == null || enemy.IsDead) return;

            // Normalize against agent's max speed to keep params in -1..1.
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            float maxSpeed = agent != null && agent.speed > 0.01f ? agent.speed : 1f;

            Vector3 v = enemy.LocalVelocity / maxSpeed;
            float x = Mathf.Clamp(v.x, -1f, 1f);
            float z = Mathf.Clamp(v.z, -1f, 1f);

            animator.SetFloat(hashX, x, damping, Time.deltaTime);
            animator.SetFloat(hashZ, z, damping, Time.deltaTime);
        }

        void OnKilled(EnemyBase _, DamageInfo info)
        {
            if (animator != null) animator.SetTrigger(hashDie);
        }
    }
}
