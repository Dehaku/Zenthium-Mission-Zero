using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>world</c> rows of constants.csv: wall and door materials, impacts, vehicles and buildings (world.md).
    /// </summary>
    [Serializable]
    public sealed class WorldConstants
    {
        // Indexed by StructureMaterial.
        [SerializeField] private float[] materialHp = new float[0];
        [SerializeField] private TierValue impactStructureDamageTier;
        [SerializeField] private TierValue impactEntityDamageTier;
        [SerializeField] private float buildingImpactMultiplier;
        [SerializeField] private float buildingThrowMultiplier;
        [SerializeField] private float demolitionMultiplier;
        [SerializeField] private float vehicleExplosionDelay;
        [SerializeField] private float buildingNeighborRadius;

        // Unity's serializer needs a parameterless constructor.
        public WorldConstants()
        {
        }

        /// <param name="materialHp">HP per <see cref="StructureMaterial"/>, in the enum's order.</param>
        public WorldConstants(
            float[] materialHp,
            TierValue impactStructureDamageTier,
            TierValue impactEntityDamageTier,
            float buildingImpactMultiplier,
            float buildingThrowMultiplier,
            float demolitionMultiplier,
            float vehicleExplosionDelay,
            float buildingNeighborRadius)
        {
            this.materialHp = materialHp ?? throw new ArgumentNullException(nameof(materialHp));
            this.impactStructureDamageTier = impactStructureDamageTier;
            this.impactEntityDamageTier = impactEntityDamageTier;
            this.buildingImpactMultiplier = buildingImpactMultiplier;
            this.buildingThrowMultiplier = buildingThrowMultiplier;
            this.demolitionMultiplier = demolitionMultiplier;
            this.vehicleExplosionDelay = vehicleExplosionDelay;
            this.buildingNeighborRadius = buildingNeighborRadius;
        }

        /// <summary>The damage tier a knocked-back entity deals to the wall or prop it hits.</summary>
        public TierValue ImpactStructureDamageTier => impactStructureDamageTier;

        /// <summary>The damage tier a knocked-back entity takes when it hits a wall or prop.</summary>
        public TierValue ImpactEntityDamageTier => impactEntityDamageTier;

        /// <summary>Walls, doors and windows take this many times the damage from knocked-back entities.</summary>
        public float BuildingImpactMultiplier => buildingImpactMultiplier;

        /// <summary>Walls, doors and windows take this many times the damage from thrown props.</summary>
        public float BuildingThrowMultiplier => buildingThrowMultiplier;

        /// <summary>Walls, doors and windows take this many times the damage from Demolition modules.</summary>
        public float DemolitionMultiplier => demolitionMultiplier;

        /// <summary>Seconds from a vehicle's destruction to its explosion.</summary>
        public float VehicleExplosionDelay => vehicleExplosionDelay;

        /// <summary>Meters: buildings this close are neighbors.</summary>
        public float BuildingNeighborRadius => buildingNeighborRadius;

        /// <returns>0 if the import had no value for it.</returns>
        public float MaterialHp(StructureMaterial material)
        {
            int index = (int)material;
            return index >= 0 && index < materialHp.Length ? materialHp[index] : 0f;
        }
    }
}
