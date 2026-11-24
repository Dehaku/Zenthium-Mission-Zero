using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>One throw of a dragged prop: by Kick (world.md, Props) or Power Throw (modules.md).</summary>
    public readonly struct ThrowInfo
    {
        /// <param name="kickDamage">The thrower's Kick damage, which the prop multiplies by its own factor.</param>
        /// <param name="distance">Meters it flies unless something stops it.</param>
        public ThrowInfo(Entity thrower, Vector3 origin, Vector3 direction, float distance, float kickDamage)
        {
            Thrower = thrower;
            Origin = origin;
            Direction = direction;
            Distance = distance;
            KickDamage = kickDamage;
        }

        public Entity Thrower { get; }

        /// <summary>The thrower's position: the prop starts just in front of it.</summary>
        public Vector3 Origin { get; }

        public Vector3 Direction { get; }
        public float Distance { get; }
        public float KickDamage { get; }
    }

    /// <summary>
    /// Something dragged that flies and hits for damage when thrown, such as a prop. Thrown entities use their
    /// motor's knockback instead and deal nothing. The World assembly implements it.
    /// </summary>
    public interface IThrowable
    {
        void Throw(in ThrowInfo info);
    }
}
