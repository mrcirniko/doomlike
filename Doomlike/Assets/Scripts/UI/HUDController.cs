using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Doomlike.Player;
using Doomlike.Weapons;
using Doomlike.Spawning;

namespace Doomlike.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] WeaponHolder weaponHolder;
        [SerializeField] DashAbility dash;
        [SerializeField] EnemySpawner spawner;

        [Header("UI")]
        [SerializeField] Slider healthBar;
        [SerializeField] Slider armorBar;
        [SerializeField] Image dashFill;
        [SerializeField] TMP_Text weaponLabel;
        [SerializeField] TMP_Text ammoLabel;
        [SerializeField] TMP_Text waveLabel;
        [SerializeField] CanvasGroup damageFlash;
        [SerializeField] float flashFadeSpeed = 2f;

        void OnEnable()
        {
            if (playerHealth != null)
            {
                if (healthBar != null) { healthBar.minValue = 0f; healthBar.maxValue = playerHealth.MaxHealth; }
                if (armorBar != null) { armorBar.minValue = 0f; armorBar.maxValue = playerHealth.MaxArmor; }
                playerHealth.Changed += OnHealthChanged;
                OnHealthChanged(playerHealth.Health, playerHealth.Armor);
            }
            if (weaponHolder != null) weaponHolder.WeaponChanged += OnWeaponChanged;
            if (spawner != null) spawner.WaveStarted += OnWaveStarted;
        }

        void OnDisable()
        {
            if (playerHealth != null) playerHealth.Changed -= OnHealthChanged;
            if (weaponHolder != null) weaponHolder.WeaponChanged -= OnWeaponChanged;
            if (spawner != null) spawner.WaveStarted -= OnWaveStarted;
        }

        void Update()
        {
            if (dash != null && dashFill != null) dashFill.fillAmount = dash.CooldownNormalized;
            if (weaponHolder != null && weaponHolder.Current != null && ammoLabel != null)
                ammoLabel.text = weaponHolder.Current.Ammo.ToString();
            if (damageFlash != null && damageFlash.alpha > 0f)
                damageFlash.alpha = Mathf.Max(0f, damageFlash.alpha - Time.deltaTime * flashFadeSpeed);
        }

        float lastHealth = float.NaN;

        void OnHealthChanged(float h, float a)
        {
            if (healthBar != null) healthBar.value = h;
            if (armorBar != null) armorBar.value = a;
            if (damageFlash != null && !float.IsNaN(lastHealth) && h < lastHealth)
                damageFlash.alpha = 0.5f;
            lastHealth = h;
        }

        void OnWeaponChanged(WeaponBase w)
        {
            if (weaponLabel != null) weaponLabel.text = w != null ? w.weaponName : "";
        }

        void OnWaveStarted(int idx)
        {
            if (waveLabel != null) waveLabel.text = $"Wave {idx + 1}";
        }
    }
}
