using System;
using UnityEngine;
using Doomlike.Player;

namespace Doomlike.Weapons
{
    public class WeaponHolder : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform cameraTransform;
        [SerializeField] WeaponBase[] weapons;
        [SerializeField] int startingIndex = 0;

        public WeaponBase Current { get; private set; }
        public int CurrentIndex { get; private set; } = -1;

        public event Action<WeaponBase> WeaponChanged;

        void OnEnable()
        {
            if (input != null)
            {
                input.WeaponSlotPressed += SelectSlot;
                input.WeaponScrollChanged += SelectScroll;
                input.AttackPressed += OnAttackPressed;
            }
            SelectSlot(startingIndex);
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.WeaponSlotPressed -= SelectSlot;
                input.WeaponScrollChanged -= SelectScroll;
                input.AttackPressed -= OnAttackPressed;
            }
        }

        void Update()
        {
            if (Current == null || input == null) return;
            if (input.AttackHeld && Current.autoFire)
                FireCurrent();
        }

        void OnAttackPressed()
        {
            if (Current == null) return;
            if (!Current.autoFire) FireCurrent();
        }

        void FireCurrent()
        {
            Vector3 origin = cameraTransform != null ? cameraTransform.position : transform.position;
            Vector3 dir = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Current.TryFire(origin, dir);
        }

        public void SelectSlot(int index)
        {
            if (weapons == null || weapons.Length == 0) return;
            if (index < 0 || index >= weapons.Length) return;
            if (index == CurrentIndex) return;

            for (int i = 0; i < weapons.Length; i++)
                if (weapons[i] != null) weapons[i].gameObject.SetActive(i == index);

            CurrentIndex = index;
            Current = weapons[index];
            WeaponChanged?.Invoke(Current);
        }

        void SelectScroll(int dir)
        {
            if (weapons == null || weapons.Length == 0) return;
            int next = (CurrentIndex + dir + weapons.Length) % weapons.Length;
            SelectSlot(next);
        }
    }
}
