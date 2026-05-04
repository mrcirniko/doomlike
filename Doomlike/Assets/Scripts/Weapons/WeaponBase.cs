using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Doomlike.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        public event Action Fired;

        [Header("Common")]
        public string weaponName = "Weapon";
        public float damage = 10f;
        public float fireRate = 8f;
        public int magazineSize = 30;
        public int reserveAmmo = 90;
        public bool autoFire = true;

        [Header("References")]
        public Transform muzzle;
        public VisualEffect muzzleVfx;

        [Header("Fire Audio")]
        [Tooltip("Single fire clip. If empty, fireClips[] is used. If both empty, no sound.")]
        public AudioClip fireClip;
        [Tooltip("Optional pool of variations — picked at random for each shot.")]
        public AudioClip[] fireClips;
        [Range(0f, 1f)] public float fireVolume = 0.7f;
        [Range(0f, 0.5f)] public float firePitchVariance = 0.08f;

        protected float nextFireTime;
        public int Ammo { get; protected set; }

        protected virtual void Awake()
        {
            Ammo = magazineSize;
        }

        public bool CanFire() => Time.time >= nextFireTime && Ammo > 0;

        public void TryFire(Vector3 origin, Vector3 direction)
        {
            if (!CanFire()) return;
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            Ammo--;
            Fire(origin, direction);
            if (muzzleVfx != null) muzzleVfx.Play();
            PlayFireSound();
            Fired?.Invoke();
        }

        void PlayFireSound()
        {
            AudioClip clip = PickFireClip();
            if (clip == null) return;
            Vector3 pos = muzzle != null ? muzzle.position : transform.position;
            // 2D blend (0) — fire sound is "in the player's head", not positional.
            Doomlike.Core.AudioOneShot.PlayAt(clip, pos, fireVolume, firePitchVariance, spatialBlend: 0f);
        }

        AudioClip PickFireClip()
        {
            if (fireClips != null && fireClips.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, fireClips.Length);
                if (fireClips[idx] != null) return fireClips[idx];
            }
            return fireClip;
        }

        protected abstract void Fire(Vector3 origin, Vector3 direction);

        public void Refill(int amount) => Ammo = Mathf.Min(magazineSize, Ammo + amount);
    }
}
