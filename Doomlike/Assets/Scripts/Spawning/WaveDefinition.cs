using System;
using UnityEngine;
using Doomlike.Enemies;

namespace Doomlike.Spawning
{
    [CreateAssetMenu(menuName = "Doomlike/Wave Definition", fileName = "Wave_")]
    public class WaveDefinition : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public EnemyBase prefab;
            public int count;
        }

        public Entry[] entries;
        public float delayBeforeStart = 1f;
    }
}
