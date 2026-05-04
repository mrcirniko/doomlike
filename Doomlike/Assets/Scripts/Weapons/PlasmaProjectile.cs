using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Weapons
{
    public class PlasmaProjectile : MonoBehaviour
    {
        [SerializeField] float radius = 0.25f;
        [SerializeField] float lifetime = 4f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] GameObject impactVfxPrefab;
        [SerializeField] float impactVfxLifetime = 2f;
        [SerializeField] AudioClip impactClip;
        [SerializeField, Range(0f, 1f)] float impactVolume = 0.7f;
        [SerializeField, Range(0f, 0.5f)] float impactPitchVariance = 0.1f;

        Vector3 velocity;
        float damage;
        GameObject source;
        float age;

        public void Launch(Vector3 vel, float dmg, GameObject src)
        {
            velocity = vel;
            damage = dmg;
            source = src;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime) { Destroy(gameObject); return; }

            float step = velocity.magnitude * Time.deltaTime;
            Vector3 dir = velocity.normalized;

            if (Physics.SphereCast(transform.position, radius, dir, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject != source && hit.collider.GetComponentInParent<PlasmaProjectile>() == null)
                {
                    var dmg = hit.collider.GetComponentInParent<IDamageable>();
                    dmg?.ApplyDamage(new DamageInfo(damage, DamageType.Plasma, source,
                        hit.point, hit.normal, dir));

                    if (impactVfxPrefab != null)
                    {
                        var inst = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(inst, impactVfxLifetime);
                    }
                    Doomlike.Core.AudioOneShot.PlayAt(impactClip, hit.point, impactVolume, impactPitchVariance);
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position += velocity * Time.deltaTime;
        }
    }
}
