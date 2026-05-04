using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Weapons
{
    public class Shotgun : WeaponBase
    {
        [Header("Shotgun")]
        [SerializeField] int pellets = 12;
        [SerializeField] float coneAngle = 14f;
        [SerializeField] float maxRange = 35f;
        [SerializeField] float falloffStart = 12f;
        [SerializeField, Range(0f, 1f)] float falloffMin = 0.4f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("VFX")]
        [SerializeField] GameObject impactVfxPrefab;
        [SerializeField] float impactVfxLifetime = 2f;
        [SerializeField] GameObject tracerPrefab;
        [SerializeField] float tracerLifetime = 0.06f;
        [Tooltip("If true, only spawn one tracer per shot (the central one). Cheaper visually.")]
        [SerializeField] bool singleTracer = true;

        [Header("Audio")]
        [SerializeField] AudioClip impactClip;
        [SerializeField, Range(0f, 1f)] float impactVolume = 0.5f;
        [SerializeField, Range(0f, 0.5f)] float impactPitchVariance = 0.15f;
        [Tooltip("Only play impact sound on first hit per shot to avoid 12 sounds at once.")]
        [SerializeField] bool singleImpactSound = true;

        protected override void Awake()
        {
            base.Awake();
            autoFire = false;
            fireRate = Mathf.Min(fireRate, 1.5f);
        }

        protected override void Fire(Vector3 origin, Vector3 direction)
        {
            bool soundPlayed = false;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplyCone(direction, coneAngle);
                Vector3 endPoint = origin + dir * maxRange;
                bool hitSomething = false;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    hitSomething = true;
                    endPoint = hit.point;
                    float falloff = 1f;
                    if (hit.distance > falloffStart)
                    {
                        float t = Mathf.InverseLerp(falloffStart, maxRange, hit.distance);
                        falloff = Mathf.Lerp(1f, falloffMin, t);
                    }
                    var dmg = hit.collider.GetComponentInParent<IDamageable>();
                    dmg?.ApplyDamage(new DamageInfo(damage * falloff, DamageType.Bullet, gameObject,
                        hit.point, hit.normal, dir));

                    if (impactVfxPrefab != null)
                    {
                        var inst = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(inst, impactVfxLifetime);
                    }

                    if (impactClip != null && (!singleImpactSound || !soundPlayed))
                    {
                        Doomlike.Core.AudioOneShot.PlayAt(impactClip, hit.point, impactVolume, impactPitchVariance);
                        soundPlayed = true;
                    }
                }

                if (!singleTracer || i == pellets / 2)
                {
                    SpawnTracer(origin, endPoint);
                }
                _ = hitSomething; // suppress unused
            }
        }

        void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (tracerPrefab == null) return;
            Vector3 spawnFrom = muzzle != null ? muzzle.position : from;
            var inst = Instantiate(tracerPrefab);
            var tracer = inst.GetComponent<BulletTracer>();
            if (tracer != null) tracer.Play(spawnFrom, to);
            Destroy(inst, tracerLifetime);
        }

        static Vector3 ApplyCone(Vector3 forward, float halfAngleDeg)
        {
            Quaternion baseRot = Quaternion.LookRotation(forward);
            float yaw = Random.Range(-halfAngleDeg, halfAngleDeg);
            float pitch = Random.Range(-halfAngleDeg, halfAngleDeg);
            return baseRot * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        }
    }
}
