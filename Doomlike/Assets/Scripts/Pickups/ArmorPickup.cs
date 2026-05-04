using UnityEngine;
using Doomlike.Player;

namespace Doomlike.Pickups
{
    public class ArmorPickup : Pickup
    {
        [SerializeField] float amount = 25f;

        protected override bool Apply(PlayerHealth player)
        {
            if (player.Armor >= player.MaxArmor) return false;
            player.AddArmor(amount);
            return true;
        }
    }
}
