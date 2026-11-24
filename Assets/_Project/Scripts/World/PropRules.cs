using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// Prop rules (world.md, Props; props.csv): the Strength needed to Drag and throw one, how hard it hits when
    /// thrown, and Small props always breaking when thrown.
    /// </summary>
    public static class PropRules
    {
        public static bool CanLift(float strength, PropData prop)
        {
            return strength >= prop.StrengthNeeded;
        }

        /// <summary>Damage to what it hits, and to itself: the thrower's Kick damage times the prop's factor.</summary>
        public static float ThrownDamage(float kickDamage, PropData prop)
        {
            return kickDamage * prop.ThrownDamageXKick;
        }

        public static bool BreaksWhenThrown(PropData prop)
        {
            return prop.Size == PropSize.Small;
        }

        /// <summary>Vehicles explode when destroyed; other props don't.</summary>
        public static bool Explodes(PropData prop)
        {
            return prop.ExplosionDamage > 0f && prop.ExplosionRadius > 0f;
        }
    }
}
