using UnityEngine;
using UnityEngine.AI;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    public class KickedEnemy : MonoBehaviour
    {
        Vector3 velocity;
        float damage;
        GameObject source;
        float lifetime = 2f;
        float age;
        EnemyBase self;

        public void Init(Vector3 vel, float dmg, GameObject src)
        {
            velocity = vel;
            damage = dmg;
            source = src;
            self = GetComponent<EnemyBase>();

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime || self == null || self.IsDead)
            {
                Destroy(this);
                return;
            }

            float step = velocity.magnitude * Time.deltaTime;
            Vector3 dir = velocity.normalized;

            if (Physics.SphereCast(transform.position, 0.6f, dir, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                var other = hit.collider.GetComponentInParent<EnemyBase>();
                if (other != null && other != self)
                {
                    other.ApplyDamage(new DamageInfo(damage, DamageType.Kick, source,
                        hit.point, hit.normal, dir));
                    self.ApplyDamage(new DamageInfo(damage, DamageType.Kick, source,
                        transform.position, -dir, -dir));
                    Destroy(this);
                    return;
                }
                if (hit.collider.GetComponentInParent<EnemyBase>() == null)
                {
                    velocity = Vector3.zero;
                    Destroy(this);
                    return;
                }
            }

            transform.position += velocity * Time.deltaTime;
            velocity += Physics.gravity * Time.deltaTime * 0.3f;
        }
    }
}
