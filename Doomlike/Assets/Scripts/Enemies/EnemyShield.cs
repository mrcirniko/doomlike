using System.Collections;
using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    public class EnemyShield : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] EnemyBase owner;

        [Header("Capacity")]
        [SerializeField] float maxHeat = 150f;
        [SerializeField] float heatDecayPerSec = 3f;
        [Tooltip("Heat fraction added per damage point for non-plasma weapons (0..1).")]
        [SerializeField, Range(0f, 1f)] float bulletHeatFactor = 0.15f;

        [Header("Overload")]
        [SerializeField] float explosionRadius = 5f;
        [SerializeField] float explosionDamage = 60f;
        [SerializeField] LayerMask aoeMask = ~0;
        [SerializeField] GameObject explosionVfxPrefab;
        [SerializeField] float explosionVfxLifetime = 3f;
        [Tooltip("Number of VFX instances scattered across the sphere surface on overload.")]
        [SerializeField, Range(1, 24)] int explosionVfxPoints = 8;
        [Tooltip("Extra burst time during which the shield expands and fades before disappearing.")]
        [SerializeField] float overloadBurstDuration = 0.35f;
        [Tooltip("Final scale multiplier reached at the end of the burst (1 = no growth).")]
        [SerializeField] float overloadBurstScale = 1.5f;

        [Header("Visual")]
        [SerializeField] Renderer shieldRenderer;
        [SerializeField] GameObject impactSparkPrefab;
        [SerializeField] float impactSparkLifetime = 1.5f;

        [Header("Audio")]
        [SerializeField] AudioClip overloadClip;
        [SerializeField] AudioClip impactClip;
        [SerializeField, Range(0f, 1f)] float overloadVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] float impactVolume = 0.4f;
        [SerializeField, Range(0f, 0.5f)] float audioPitchVariance = 0.1f;

        public bool IsActive { get; private set; } = true;
        public float Heat { get; private set; }

        MaterialPropertyBlock mpb;
        Vector4 lastImpact = new Vector4(0f, 0f, 0f, -1000f);

        static readonly int IdImpactPos = Shader.PropertyToID("_ImpactPos");
        static readonly int IdHeat = Shader.PropertyToID("_Heat");

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            ApplyMpb();
        }

        void Update()
        {
            if (!IsActive) return;
            if (Heat > 0f)
            {
                Heat = Mathf.Max(0f, Heat - heatDecayPerSec * Time.deltaTime);
                ApplyMpb();
            }
            else
            {
                ApplyMpb();
            }
        }

        public void AbsorbDamage(in DamageInfo info)
        {
            if (!IsActive) return;

            RegisterImpact(info.HitPoint);
            if (impactSparkPrefab != null)
            {
                var rot = info.HitNormal.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(info.HitNormal)
                    : Quaternion.identity;
                var spark = Instantiate(impactSparkPrefab, info.HitPoint, rot);
                Destroy(spark, impactSparkLifetime);
            }

            if (impactClip != null)
                AudioOneShot.PlayAt(impactClip, info.HitPoint, impactVolume, audioPitchVariance);

            float heatAdd = info.Type == DamageType.Plasma
                ? info.Amount
                : info.Amount * bulletHeatFactor;
            Heat += heatAdd;
            if (Heat >= maxHeat) Overload();
            ApplyMpb();
        }

        void RegisterImpact(Vector3 worldPos)
        {
            lastImpact = new Vector4(worldPos.x, worldPos.y, worldPos.z, Time.timeSinceLevelLoad);
        }

        void ApplyMpb()
        {
            if (shieldRenderer == null) return;
            shieldRenderer.GetPropertyBlock(mpb);
            mpb.SetVector(IdImpactPos, lastImpact);
            mpb.SetFloat(IdHeat, Mathf.Clamp01(Heat / maxHeat));
            shieldRenderer.SetPropertyBlock(mpb);
        }

        void Overload()
        {
            IsActive = false;
            Heat = maxHeat;
            ApplyMpb();

            // Loud explosion sound at the shield center.
            if (overloadClip != null)
                AudioOneShot.PlayAt(overloadClip, transform.position, overloadVolume, audioPitchVariance, maxDistance: 40f);

            // Spawn VFX at evenly distributed points on the sphere surface (Fibonacci sphere).
            float radius = 1f;
            if (shieldRenderer != null)
            {
                Vector3 ext = shieldRenderer.bounds.extents;
                radius = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
            }
            Vector3 center = shieldRenderer != null ? shieldRenderer.bounds.center : transform.position;

            if (explosionVfxPrefab != null)
            {
                int n = Mathf.Max(1, explosionVfxPoints);
                for (int i = 0; i < n; i++)
                {
                    Vector3 dir = FibonacciDirection(i, n);
                    Vector3 pos = center + dir * radius;
                    var inst = Instantiate(explosionVfxPrefab, pos, Quaternion.LookRotation(dir));
                    Destroy(inst, explosionVfxLifetime);
                }
            }

            // AoE damage (gameplay) is still center-based.
            // Owner is intentionally exempt — losing the shield is already a heavy penalty,
            // dying from your own overload feels unfair.
            Collider[] hits = Physics.OverlapSphere(center, explosionRadius, aoeMask);
            IDamageable ownerDamageable = owner != null ? owner.GetComponent<IDamageable>() : null;
            foreach (var c in hits)
            {
                var dmg = c.GetComponentInParent<IDamageable>();
                if (dmg == null) continue;
                if ((object)dmg == (object)this) continue;
                if (ownerDamageable != null && (object)dmg == (object)ownerDamageable) continue;
                Vector3 to = (c.transform.position - center).normalized;
                dmg.ApplyDamage(new DamageInfo(explosionDamage, DamageType.Explosion,
                    gameObject, c.transform.position, -to, to));
            }

            // Visual burst: briefly expand the shield and fade it instead of vanishing instantly.
            if (shieldRenderer != null) StartCoroutine(BurstAndHide());
        }

        IEnumerator BurstAndHide()
        {
            Transform t = shieldRenderer.transform;
            Vector3 startScale = t.localScale;
            Vector3 endScale = startScale * overloadBurstScale;
            float elapsed = 0f;
            while (elapsed < overloadBurstDuration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / overloadBurstDuration);
                t.localScale = Vector3.Lerp(startScale, endScale, k);
                yield return null;
            }
            t.localScale = startScale;
            shieldRenderer.enabled = false;
        }

        static Vector3 FibonacciDirection(int i, int total)
        {
            float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));
            float y = total <= 1 ? 0f : 1f - (i / (float)(total - 1)) * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = phi * i;
            float x = Mathf.Cos(theta) * r;
            float z = Mathf.Sin(theta) * r;
            return new Vector3(x, y, z);
        }
    }
}
