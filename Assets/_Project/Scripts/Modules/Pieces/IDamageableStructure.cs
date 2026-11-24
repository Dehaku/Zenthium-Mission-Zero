using UnityEngine;
using Zenthium.Combat;

namespace Zenthium.Modules
{
    /// <summary>How a structure was hit, which decides the building multipliers (world.md; local design decisions).</summary>
    public enum StructureHitKind
    {
        /// <summary>A module's hit, such as a Kick or a projectile.</summary>
        Direct,

        /// <summary>A knocked-back entity slammed into it.</summary>
        Impact,

        /// <summary>A hit from a module with the Demolition tag, such as Breaching Charge.</summary>
        Demolition,

        /// <summary>A thrown prop hit it.</summary>
        Thrown,
    }

    /// <summary>One hit on a structure, before its Armor, Damage Reduction and multipliers.</summary>
    public readonly struct StructureHit
    {
        public StructureHit(Hit hit, StructureHitKind kind)
        {
            Hit = hit;
            Kind = kind;
        }

        public Hit Hit { get; }
        public StructureHitKind Kind { get; }
    }

    /// <summary>
    /// A wall, door, window or prop that modules, impacts and explosions can damage (world.md). It isn't an entity:
    /// it has HP but no stats, dodge, Defiance or statuses. The World assembly implements it.
    /// </summary>
    public interface IDamageableStructure
    {
        /// <summary>False once broken, and while it can't be hurt, such as a dragged prop.</summary>
        bool CanBeHit { get; }

        /// <summary>The point on it nearest to a position, for melee arcs.</summary>
        Vector3 ClosestPoint(Vector3 position);

        void TakeHit(in StructureHit hit);
    }
}
