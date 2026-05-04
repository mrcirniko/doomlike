using UnityEngine;

namespace Doomlike.Weapons
{
    public class PlasmaGun : WeaponBase
    {
        [Header("Plasma")]
        [SerializeField] PlasmaProjectile projectilePrefab;
        [SerializeField] float projectileSpeed = 50f;

        public enum SpawnPattern
        {
            Line,    // horizontal line (default)
            Ring,    // circle perpendicular to fire direction
            RandomDisk // uniformly random within a disk
        }

        [Header("Multi-shot")]
        [Tooltip("Number of projectiles spawned per shot.")]
        [SerializeField, Min(1)] int projectileCount = 1;
        [Tooltip("Spatial pattern of the projectiles around the muzzle axis.")]
        [SerializeField] SpawnPattern pattern = SpawnPattern.Line;
        [Tooltip("Distance from center axis (radius for Ring/Disk, step for Line).")]
        [SerializeField] float patternRadius = 0.3f;
        [Tooltip("Cone spread in degrees. 0 = perfectly parallel, >0 = each projectile gets a small angular jitter.")]
        [SerializeField] float spreadAngle = 0f;
        [Tooltip("Rotation offset of the Ring pattern in degrees (rotates the wheel).")]
        [SerializeField] float ringStartAngle = 0f;

        protected override void Awake()
        {
            base.Awake();
            autoFire = true;
            fireRate = Mathf.Min(fireRate, 5f);
        }

        protected override void Fire(Vector3 origin, Vector3 direction)
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = muzzle != null ? muzzle.position : origin;
            Vector3 fwd = direction.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            Vector3 up = Vector3.Cross(fwd, right).normalized;

            int n = Mathf.Max(1, projectileCount);
            for (int i = 0; i < n; i++)
            {
                Vector3 offset = ComputeOffset(i, n, right, up);

                Vector3 dir = fwd;
                if (spreadAngle > 0f)
                {
                    Quaternion baseRot = Quaternion.LookRotation(fwd);
                    float yaw = Random.Range(-spreadAngle, spreadAngle);
                    float pitch = Random.Range(-spreadAngle, spreadAngle);
                    dir = baseRot * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                }

                var proj = Instantiate(projectilePrefab, spawnPos + offset, Quaternion.LookRotation(dir));
                proj.Launch(dir.normalized * projectileSpeed, damage, gameObject);
            }
        }

        Vector3 ComputeOffset(int i, int n, Vector3 right, Vector3 up)
        {
            switch (pattern)
            {
                case SpawnPattern.Ring:
                {
                    if (n <= 1) return Vector3.zero;
                    float a = (i / (float)n) * Mathf.PI * 2f + ringStartAngle * Mathf.Deg2Rad;
                    return right * Mathf.Cos(a) * patternRadius + up * Mathf.Sin(a) * patternRadius;
                }
                case SpawnPattern.RandomDisk:
                {
                    Vector2 rd = Random.insideUnitCircle * patternRadius;
                    return right * rd.x + up * rd.y;
                }
                default: // Line
                {
                    if (n == 1) return Vector3.zero;
                    float t = i - (n - 1) * 0.5f;
                    return right * t * patternRadius;
                }
            }
        }
    }
}
