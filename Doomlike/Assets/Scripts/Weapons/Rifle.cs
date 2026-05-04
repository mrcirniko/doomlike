using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Weapons
{
    public class Rifle : WeaponBase
    {
        [Header("Rifle")]
        [SerializeField] float maxRange = 100f;
        [SerializeField] float spread = 0.02f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("VFX")]
        [SerializeField] GameObject impactVfxPrefab;
        [SerializeField] float impactVfxLifetime = 2f;
        [SerializeField] GameObject tracerPrefab;
        [SerializeField] float tracerLifetime = 0.06f;

        [Header("Audio")]
        [SerializeField] AudioClip impactClip;
        [SerializeField, Range(0f, 1f)] float impactVolume = 0.6f;
        [SerializeField, Range(0f, 0.5f)] float impactPitchVariance = 0.1f;

        protected override void Awake()
        {
            base.Awake();
            autoFire = true;
        }

        protected override void Fire(Vector3 origin, Vector3 direction)
        {
            Vector3 jitter = Random.insideUnitSphere * spread;
            Vector3 dir = (direction + jitter).normalized;

            Vector3 endPoint = origin + dir * maxRange;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.ApplyDamage(new DamageInfo(damage, DamageType.Bullet, gameObject,
                    hit.point, hit.normal, dir));

                if (impactVfxPrefab != null)
                {
                    var inst = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(inst, impactVfxLifetime);
                }

                Doomlike.Core.AudioOneShot.PlayAt(impactClip, hit.point, impactVolume, impactPitchVariance);
            }

            SpawnTracer(origin, endPoint);
        }

        void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (tracerPrefab == null) return;
            Vector3 spawnFrom = muzzle != null ? muzzle.position : from;
            var inst = Instantiate(tracerPrefab);
            var tracer = inst.GetComponent<Doomlike.Weapons.BulletTracer>();
            if (tracer != null) tracer.Play(spawnFrom, to);
            Destroy(inst, tracerLifetime);
        }
    }
}
