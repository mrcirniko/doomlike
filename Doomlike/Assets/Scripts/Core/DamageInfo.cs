using UnityEngine;

namespace Doomlike.Core
{
    public enum DamageType
    {
        Bullet,
        Plasma,
        Explosion,
        Melee,
        Kick
    }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public GameObject Source;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public Vector3 Direction;

        public DamageInfo(float amount, DamageType type, GameObject source = null,
            Vector3 hitPoint = default, Vector3 hitNormal = default, Vector3 direction = default)
        {
            Amount = amount;
            Type = type;
            Source = source;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Direction = direction;
        }
    }
}
