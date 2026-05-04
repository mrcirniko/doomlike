using UnityEngine;

namespace Doomlike.Player
{
    /// <summary>
    /// Mirrors RangedEnemyAnimator: drives an Animator from CharacterController velocity.
    /// Expects on the controller:
    ///   - Float "MoveX" (-1..1, +1 = strafe right)
    ///   - Float "MoveZ" (-1..1, +1 = walk forward)
    ///   - Trigger "Die"
    ///   - (optional) Trigger "Jump" / "Fire"
    /// Use a 2D Freeform Cartesian blend tree on (MoveX, MoveZ) for locomotion.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] CharacterController controller;
        [SerializeField] PlayerHealth health;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Doomlike.Weapons.WeaponHolder weaponHolder;

        [Header("Tuning")]
        [SerializeField] float referenceSpeed = 8f;
        [SerializeField] float damping = 0.12f;

        [Header("Param names")]
        [SerializeField] string moveXParam = "MoveX";
        [SerializeField] string moveZParam = "MoveZ";
        [SerializeField] string dieTrigger = "Die";
        [SerializeField] string jumpTrigger = "Jump";
        [SerializeField] string fireTrigger = "Fire";

        int hashX, hashZ, hashDie, hashJump, hashFire;
        Doomlike.Weapons.WeaponBase subscribedWeapon;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (controller == null) controller = GetComponent<CharacterController>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (input == null) input = GetComponent<PlayerInputReader>();

            hashX = Animator.StringToHash(moveXParam);
            hashZ = Animator.StringToHash(moveZParam);
            hashDie = Animator.StringToHash(dieTrigger);
            hashJump = Animator.StringToHash(jumpTrigger);
            hashFire = Animator.StringToHash(fireTrigger);
        }

        void OnEnable()
        {
            if (health != null) health.Died += OnDied;
            if (input != null) input.JumpPressed += OnJump;
            if (weaponHolder != null)
            {
                weaponHolder.WeaponChanged += OnWeaponChanged;
                OnWeaponChanged(weaponHolder.Current);
            }
        }

        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (input != null) input.JumpPressed -= OnJump;
            if (weaponHolder != null) weaponHolder.WeaponChanged -= OnWeaponChanged;
            if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
            subscribedWeapon = null;
        }

        void Update()
        {
            if (animator == null || controller == null) return;

            // Project world velocity to local space (so strafing is X, forward is Z).
            Vector3 worldVel = controller.velocity;
            worldVel.y = 0f;
            Vector3 localVel = transform.InverseTransformDirection(worldVel);

            float x = Mathf.Clamp(localVel.x / Mathf.Max(0.01f, referenceSpeed), -1f, 1f);
            float z = Mathf.Clamp(localVel.z / Mathf.Max(0.01f, referenceSpeed), -1f, 1f);

            animator.SetFloat(hashX, x, damping, Time.deltaTime);
            animator.SetFloat(hashZ, z, damping, Time.deltaTime);
        }

        void OnDied() { if (animator != null) animator.SetTrigger(hashDie); }
        void OnJump() { if (animator != null) animator.SetTrigger(hashJump); }
        void OnFired() { if (animator != null) animator.SetTrigger(hashFire); }

        void OnWeaponChanged(Doomlike.Weapons.WeaponBase w)
        {
            if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
            subscribedWeapon = w;
            if (subscribedWeapon != null) subscribedWeapon.Fired += OnFired;
        }
    }
}
