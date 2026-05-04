using UnityEngine;
using Doomlike.Core;

namespace Doomlike.Enemies
{
    public class ShieldedRangedEnemy : RangedEnemy
    {
        [Header("Shield")]
        [SerializeField] EnemyShield shield;

        public override void ApplyDamage(in DamageInfo info)
        {
            if (shield != null && shield.IsActive)
            {
                shield.AbsorbDamage(info);
                if (info.Type == DamageType.Explosion) base.ApplyDamage(info);
                return;
            }
            base.ApplyDamage(info);
        }
    }
}
