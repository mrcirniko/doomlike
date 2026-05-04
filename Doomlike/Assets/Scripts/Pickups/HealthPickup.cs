using UnityEngine;
using Doomlike.Player;

namespace Doomlike.Pickups
{
    public class HealthPickup : Pickup
    {
        [SerializeField] float amount = 25f;

        protected override bool Apply(PlayerHealth player)
        {
            if (player.Health >= player.MaxHealth) return false;
            player.Heal(amount);
            return true;
        }
    }
}
