using UnityEngine;
using Doomlike.Core;
using Doomlike.Player;

namespace Doomlike.Pickups
{
    [RequireComponent(typeof(Collider))]
    public abstract class Pickup : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] float spinSpeed = 60f;

        [Header("Audio")]
        [SerializeField] AudioClip pickupClip;
        [SerializeField, Range(0f, 1f)] float pickupVolume = 0.7f;
        [SerializeField, Range(0f, 0.5f)] float pickupPitchVariance = 0.08f;

        [Header("VFX")]
        [SerializeField] GameObject pickupVfxPrefab;
        [SerializeField] float pickupVfxLifetime = 1f;

        Vector3 startPos;

        protected virtual void Start()
        {
            startPos = transform.position;
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph == null) return;
            if (Apply(ph))
            {
                AudioOneShot.PlayAt(pickupClip, transform.position, pickupVolume, pickupPitchVariance, spatialBlend: 0.4f);
                if (pickupVfxPrefab != null)
                {
                    var inst = Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
                    Destroy(inst, pickupVfxLifetime);
                }
                Destroy(gameObject);
            }
        }

        protected abstract bool Apply(PlayerHealth player);
    }
}
