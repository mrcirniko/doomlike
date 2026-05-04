using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Doomlike.Player
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] string playerMapName = "Player";

        InputActionMap map;
        InputAction moveAction, lookAction, attackAction, dashAction, lockOnAction, kickAction, jumpAction;
        InputAction weapon1, weapon2, weapon3, weaponScroll;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool LockOnHeld { get; private set; }

        public event Action AttackPressed;
        public event Action DashPressed;
        public event Action KickPressed;
        public event Action JumpPressed;
        public event Action LockOnStarted;
        public event Action LockOnReleased;
        public event Action<int> WeaponSlotPressed;
        public event Action<int> WeaponScrollChanged;

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerInputReader: InputActionAsset not assigned.", this);
                enabled = false;
                return;
            }
            map = actions.FindActionMap(playerMapName, true);
            moveAction = map.FindAction("Move", true);
            lookAction = map.FindAction("Look", true);
            attackAction = map.FindAction("Attack", true);
            dashAction = map.FindAction("Dash", true);
            lockOnAction = map.FindAction("LockOn", true);
            kickAction = map.FindAction("Kick", true);
            jumpAction = map.FindAction("Jump", true);
            weapon1 = map.FindAction("Weapon1", true);
            weapon2 = map.FindAction("Weapon2", true);
            weapon3 = map.FindAction("Weapon3", true);
            weaponScroll = map.FindAction("WeaponScroll", true);
        }

        void OnEnable()
        {
            if (map == null) return;
            map.Enable();

            attackAction.started += OnAttackStarted;
            attackAction.canceled += OnAttackCanceled;
            dashAction.performed += OnDashPerformed;
            kickAction.performed += OnKickPerformed;
            jumpAction.performed += OnJumpPerformed;
            lockOnAction.started += OnLockOnStarted;
            lockOnAction.canceled += OnLockOnCanceled;
            weapon1.performed += _ => WeaponSlotPressed?.Invoke(0);
            weapon2.performed += _ => WeaponSlotPressed?.Invoke(1);
            weapon3.performed += _ => WeaponSlotPressed?.Invoke(2);
            weaponScroll.performed += OnScroll;
        }

        void OnDisable()
        {
            if (map == null) return;
            attackAction.started -= OnAttackStarted;
            attackAction.canceled -= OnAttackCanceled;
            dashAction.performed -= OnDashPerformed;
            kickAction.performed -= OnKickPerformed;
            jumpAction.performed -= OnJumpPerformed;
            lockOnAction.started -= OnLockOnStarted;
            lockOnAction.canceled -= OnLockOnCanceled;
            map.Disable();
        }

        void Update()
        {
            MoveInput = moveAction.ReadValue<Vector2>();
            LookInput = lookAction.ReadValue<Vector2>();
            AttackHeld = attackAction.IsPressed();
            LockOnHeld = lockOnAction.IsPressed();
        }

        void OnAttackStarted(InputAction.CallbackContext ctx) { AttackHeld = true; AttackPressed?.Invoke(); }
        void OnAttackCanceled(InputAction.CallbackContext ctx) { AttackHeld = false; }
        void OnDashPerformed(InputAction.CallbackContext ctx) { DashPressed?.Invoke(); }
        void OnKickPerformed(InputAction.CallbackContext ctx) { KickPressed?.Invoke(); }
        void OnJumpPerformed(InputAction.CallbackContext ctx) { JumpPressed?.Invoke(); }
        void OnLockOnStarted(InputAction.CallbackContext ctx) { LockOnHeld = true; LockOnStarted?.Invoke(); }
        void OnLockOnCanceled(InputAction.CallbackContext ctx) { LockOnHeld = false; LockOnReleased?.Invoke(); }

        void OnScroll(InputAction.CallbackContext ctx)
        {
            float y = ctx.ReadValue<Vector2>().y;
            if (Mathf.Abs(y) < 0.1f) return;
            WeaponScrollChanged?.Invoke(y > 0f ? 1 : -1);
        }
    }
}
