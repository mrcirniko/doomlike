using UnityEngine;
using Doomlike.Weapons;

namespace Doomlike.Player
{
    /// <summary>
    /// Drives a weapon view-model transform: idle bob from movement, sway from mouse look,
    /// "aim forward" offset while firing, and per-shot recoil kick.
    /// </summary>
    public class ViewModelSway : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform to animate. Leave empty to animate this object.")]
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;
        [SerializeField] CharacterController controller;
        [SerializeField] WeaponHolder weaponHolder;

        [Header("Idle bob (move)")]
        [SerializeField] float bobFrequency = 6f;
        [SerializeField] float bobVertical = 0.025f;
        [SerializeField] float bobHorizontal = 0.035f;
        [SerializeField] float walkReferenceSpeed = 6f;

        [Header("Sway (mouse look)")]
        [SerializeField] float lookSwayPos = 0.012f;
        [SerializeField] float lookTilt = 3f;

        [Header("Tilt (strafe)")]
        [SerializeField] float strafeTilt = 4f;

        [Header("Aim while firing")]
        [Tooltip("Local offset applied while attack button is held.")]
        [SerializeField] Vector3 aimOffsetPosition = new Vector3(0f, 0.04f, 0.10f);
        [SerializeField] Vector3 aimOffsetRotation = new Vector3(-3f, 0f, 0f);
        [SerializeField] float aimResponse = 10f;

        [Header("Per-shot kick")]
        [SerializeField] Vector3 kickPosition = new Vector3(0f, 0.02f, -0.05f);
        [SerializeField] Vector3 kickRotation = new Vector3(-6f, 0f, 0f);
        [SerializeField] float kickReturnSpeed = 12f;

        [Header("Smoothing")]
        [SerializeField] float positionResponse = 14f;
        [SerializeField] float rotationResponse = 14f;

        Vector3 basePos;
        Quaternion baseRot;
        Vector3 smoothedPos;
        Quaternion smoothedRot;
        float bobPhase;
        Vector3 aimPosCurrent, aimRotCurrent;
        Vector3 kickPosCurrent, kickRotCurrent;
        WeaponBase subscribedWeapon;

        void Awake()
        {
            if (target == null) target = transform;
            basePos = target.localPosition;
            baseRot = target.localRotation;
            smoothedPos = basePos;
            smoothedRot = baseRot;
            if (input == null) input = GetComponentInParent<PlayerInputReader>();
            if (controller == null) controller = GetComponentInParent<CharacterController>();
            if (weaponHolder == null) weaponHolder = GetComponent<WeaponHolder>() ?? GetComponentInChildren<WeaponHolder>();
        }

        void OnEnable()
        {
            // Reset smoothing state when this weapon becomes active, so semi-mode starts clean.
            if (target != null)
            {
                smoothedPos = target.localPosition;
                smoothedRot = target.localRotation;
            }
            kickPosCurrent = Vector3.zero;
            kickRotCurrent = Vector3.zero;

            if (weaponHolder != null)
            {
                weaponHolder.WeaponChanged += OnWeaponChanged;
                OnWeaponChanged(weaponHolder.Current);
            }
        }

        void OnDisable()
        {
            if (weaponHolder != null) weaponHolder.WeaponChanged -= OnWeaponChanged;
            if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
            subscribedWeapon = null;
        }

        void OnWeaponChanged(WeaponBase w)
        {
            if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
            subscribedWeapon = w;
            if (subscribedWeapon != null) subscribedWeapon.Fired += OnFired;
        }

        void OnFired()
        {
            Debug.Log($"[ViewModelSway] {gameObject.name} OnFired. weapon={subscribedWeapon?.name} autoFire={subscribedWeapon?.autoFire}");
            kickPosCurrent = kickPosition;
            kickRotCurrent = kickRotation;
        }

        void Update()
        {
            // movement amplitude
            float speed = 0f;
            if (controller != null)
            {
                Vector3 v = controller.velocity;
                speed = new Vector2(v.x, v.z).magnitude;
                if (!controller.isGrounded) speed *= 0.3f;
            }
            float strength = Mathf.Clamp01(speed / Mathf.Max(0.01f, walkReferenceSpeed));

            bobPhase += Time.deltaTime * bobFrequency * Mathf.Lerp(0.6f, 1.4f, strength);
            float bobX = Mathf.Sin(bobPhase) * bobHorizontal * strength;
            float bobY = Mathf.Sin(bobPhase * 2f) * bobVertical * strength;

            // mouse-look sway
            Vector2 look = input != null ? input.LookInput : Vector2.zero;
            float sx = -look.x * lookSwayPos;
            float sy = -look.y * lookSwayPos;
            float lookTiltZ = -look.x * lookTilt;

            // strafe tilt
            float strafe = input != null ? input.MoveInput.x : 0f;
            float strafeTiltZ = -strafe * strafeTilt;

            // aim-while-firing toward target offsets — only for auto-fire weapons.
            // For semi-fire weapons (shotgun) aim never engages, so the kick is not masked
            // by a forward push.
            bool firing = input != null && input.AttackHeld;
            bool autoFire = subscribedWeapon == null || subscribedWeapon.autoFire;
            bool aimActive = firing && autoFire;
            Vector3 aimPosTarget = aimActive ? aimOffsetPosition : Vector3.zero;
            Vector3 aimRotTarget = aimActive ? aimOffsetRotation : Vector3.zero;
            aimPosCurrent = Vector3.Lerp(aimPosCurrent, aimPosTarget, aimResponse * Time.deltaTime);
            aimRotCurrent = Vector3.Lerp(aimRotCurrent, aimRotTarget, aimResponse * Time.deltaTime);

            Vector3 desiredBasePos = basePos
                + new Vector3(bobX + sx, bobY + sy, 0f)
                + aimPosCurrent;

            Quaternion desiredBaseRot = baseRot * Quaternion.Euler(
                aimRotCurrent.x,
                aimRotCurrent.y,
                lookTiltZ + strafeTiltZ + aimRotCurrent.z);

            bool isAuto = subscribedWeapon != null && subscribedWeapon.autoFire;
            float dt = Time.deltaTime;

            if (isAuto)
            {
                // Auto-fire path: original smoothed feel — kick rolls through the same smoother
                // as everything else, multiple rapid shots accumulate into a continuous "rumble".
                Vector3 finalPos = desiredBasePos + kickPosCurrent;
                Quaternion finalRot = desiredBaseRot * Quaternion.Euler(kickRotCurrent);

                target.localPosition = Vector3.Lerp(target.localPosition, finalPos, positionResponse * dt);
                target.localRotation = Quaternion.Slerp(target.localRotation, finalRot, rotationResponse * dt);

                // Keep smoothed* in sync so a swap to semi has a sane starting point.
                smoothedPos = target.localPosition - kickPosCurrent;
                smoothedRot = target.localRotation * Quaternion.Inverse(Quaternion.Euler(kickRotCurrent));
            }
            else
            {
                // Semi-fire path (shotgun): smooth only the base, then snap the kick on top with
                // no smoothing — a single shot becomes a clearly visible punch.
                smoothedPos = Vector3.Lerp(smoothedPos, desiredBasePos, positionResponse * dt);
                smoothedRot = Quaternion.Slerp(smoothedRot, desiredBaseRot, rotationResponse * dt);

                target.localPosition = smoothedPos + kickPosCurrent;
                target.localRotation = smoothedRot * Quaternion.Euler(kickRotCurrent);
            }

            // Kick decays toward zero on its own (works the same in both modes).
            kickPosCurrent = Vector3.Lerp(kickPosCurrent, Vector3.zero, kickReturnSpeed * dt);
            kickRotCurrent = Vector3.Lerp(kickRotCurrent, Vector3.zero, kickReturnSpeed * dt);
        }
    }
}
