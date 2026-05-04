using UnityEngine;

namespace Doomlike.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class DashAbility : MonoBehaviour
    {
        [Header("Dash")]
        [Tooltip("Speed during dash (m/s). Distance = dashSpeed * dashDuration.")]
        [SerializeField] float dashSpeed = 28f;
        [Tooltip("How long a single dash lasts (seconds).")]
        [SerializeField] float dashDuration = 0.18f;

        [Header("Charges")]
        [Tooltip("Max simultaneous stored dashes.")]
        [SerializeField] int maxCharges = 2;
        [Tooltip("Seconds to regenerate one charge.")]
        [SerializeField] float chargeRegenTime = 1.2f;

        PlayerController player;
        PlayerInputReader input;
        PlayerHealth health;

        float dashTimer;          // remaining time of active dash
        float regenTimer;         // remaining time to regen the next charge
        int charges;
        Vector3 dashVelocity;

        public int Charges => charges;
        public int MaxCharges => maxCharges;
        public bool IsDashing => dashTimer > 0f;

        /// <summary>0..1. Total fullness of charge pool, smooth across regen cycles.</summary>
        public float CooldownNormalized
        {
            get
            {
                if (maxCharges <= 0) return 1f;
                float partial = charges < maxCharges
                    ? 1f - regenTimer / Mathf.Max(0.0001f, chargeRegenTime)
                    : 0f;
                return Mathf.Clamp01((charges + partial) / maxCharges);
            }
        }

        public float DashDistance => dashSpeed * dashDuration;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<PlayerHealth>();
            charges = maxCharges;
        }

        void OnEnable() => input.DashPressed += TryDash;
        void OnDisable() => input.DashPressed -= TryDash;

        void Update()
        {
            // regen
            if (charges < maxCharges)
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0f)
                {
                    charges++;
                    regenTimer = charges < maxCharges ? chargeRegenTime : 0f;
                }
            }

            // active dash
            if (dashTimer > 0f)
            {
                dashTimer -= Time.deltaTime;
                player.SetExternalVelocity(dashVelocity, useGravity: false);
                if (dashTimer <= 0f)
                {
                    player.ClearExternalVelocity();
                    if (health != null) health.IsInvulnerable = false;
                }
            }
        }

        void TryDash()
        {
            if (dashTimer > 0f) return;
            if (charges <= 0) return;

            Vector2 m = input.MoveInput;
            Vector3 dir;
            if (m.sqrMagnitude < 0.01f)
                dir = transform.forward;
            else
                dir = (transform.right * m.x + transform.forward * m.y).normalized;

            dashVelocity = dir * dashSpeed;
            dashTimer = dashDuration;

            // spend a charge; start regen if it wasn't already running
            bool wasFull = charges == maxCharges;
            charges--;
            if (wasFull) regenTimer = chargeRegenTime;

            if (health != null) health.IsInvulnerable = true;
        }
    }
}
