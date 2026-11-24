using Zenthium.Data;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// The extra damage building parts take (local design decisions): walls, doors and windows take a multiple of the
    /// damage from knocked-back entities, thrown props and Demolition modules, each with its own multiplier, so
    /// knockback and throws break them easily. Props and direct hits take the damage as it is.
    /// </summary>
    public static class StructureDamageRules
    {
        /// <param name="isBuildingPart">A wall, door or window, rather than a prop.</param>
        public static float Multiplier(StructureHitKind kind, bool isBuildingPart, WorldConstants constants)
        {
            if (!isBuildingPart)
            {
                return 1f;
            }

            switch (kind)
            {
                case StructureHitKind.Impact:
                    return constants.BuildingImpactMultiplier;
                case StructureHitKind.Demolition:
                    return constants.DemolitionMultiplier;
                case StructureHitKind.Thrown:
                    return constants.BuildingThrowMultiplier;
                default:
                    return 1f;
            }
        }
    }
}
