using UnityEngine;
using Unity.Cinemachine;
using Doomlike.Enemies;

namespace Doomlike.Player
{
    public class LockOnSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform cameraTransform;
        [SerializeField] CinemachineCamera freeLookVcam;
        [SerializeField] CinemachineCamera lockOnVcam;

        [Header("Search")]
        [SerializeField] float maxRange = 35f;
        [SerializeField, Range(0f, 1f)] float dotThreshold = 0.5f;
        [SerializeField] LayerMask enemyMask = ~0;
        [SerializeField] LayerMask occlusionMask = ~0;
        [Tooltip("Height above the enemy pivot where the camera looks (0 = feet, ~1 = centre mass).")]
        [SerializeField] float lookAtHeightOffset = 1f;

        public Transform CurrentTarget { get; private set; }
        public bool IsActive => CurrentTarget != null;

        Transform lookAtProxy;

        void Awake()
        {
            var go = new GameObject("LockOnProxy");
            go.transform.SetParent(transform);
            lookAtProxy = go.transform;
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.LockOnStarted += OnLockOnStarted;
                input.LockOnReleased += OnLockOnReleased;
            }
            UpdateVcams();
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.LockOnStarted -= OnLockOnStarted;
                input.LockOnReleased -= OnLockOnReleased;
            }
        }

        void Update()
        {
            if (CurrentTarget == null) return;
            if (!CurrentTarget.gameObject.activeInHierarchy)
            {
                ReleaseTarget();
                return;
            }

            lookAtProxy.position = CurrentTarget.position + Vector3.up * lookAtHeightOffset;

            float dist = Vector3.Distance(transform.position, CurrentTarget.position);
            if (dist > maxRange * 1.2f) ReleaseTarget();
        }

        void OnLockOnStarted()
        {
            Transform t = FindBestTarget();
            if (t == null) return;
            CurrentTarget = t;
            lookAtProxy.position = t.position + Vector3.up * lookAtHeightOffset;
            if (lockOnVcam != null) lockOnVcam.LookAt = lookAtProxy;
            UpdateVcams();
        }

        void OnLockOnReleased() => ReleaseTarget();

        void ReleaseTarget()
        {
            CurrentTarget = null;
            if (lockOnVcam != null) lockOnVcam.LookAt = null;
            UpdateVcams();
        }

        void UpdateVcams()
        {
            // Activate the lock-on vcam only while locked. Free-look vcam stays disabled
            // so CinemachineBrain has no live vcam and PlayerController drives the camera manually.
            if (freeLookVcam != null && freeLookVcam.gameObject.activeSelf)
                freeLookVcam.gameObject.SetActive(false);
            if (lockOnVcam != null)
                lockOnVcam.gameObject.SetActive(IsActive);
        }

        Transform FindBestTarget()
        {
            Vector3 origin = cameraTransform != null ? cameraTransform.position : transform.position;
            Vector3 fwd = cameraTransform != null ? cameraTransform.forward : transform.forward;

            Collider[] hits = Physics.OverlapSphere(origin, maxRange, enemyMask, QueryTriggerInteraction.Ignore);
            Transform best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var col in hits)
            {
                var enemy = col.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                Vector3 to = enemy.transform.position - origin;
                float dist = to.magnitude;
                if (dist < 0.01f) continue;
                Vector3 dir = to / dist;
                float dot = Vector3.Dot(fwd, dir);
                if (dot < dotThreshold) continue;

                if (Physics.Raycast(origin, dir, out RaycastHit blockHit, dist, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    if (blockHit.transform.GetComponentInParent<EnemyBase>() != enemy) continue;
                }

                float score = dot * 2f - dist / maxRange;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy.transform;
                }
            }
            return best;
        }
    }
}
