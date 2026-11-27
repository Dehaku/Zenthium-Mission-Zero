using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// constants.csv's <c>mission</c> rows for what a mission puts on the map besides enemies: Valuables
    /// (world.md, Placement), citizens' kinds and Heavy Turrets (enemies.md, Neutral spawns).
    /// </summary>
    [Serializable]
    public sealed class MissionFillConstants
    {
        [SerializeField] private int bankValuablesMin;
        [SerializeField] private int bankValuablesMax;
        [SerializeField] private int storeValuablesMin;
        [SerializeField] private int storeValuablesMax;
        [SerializeField] private float buildingValuableChance;
        [SerializeField] private float civilianValuableChance;
        [SerializeField] private float heavyTurretChance;
        [SerializeField] private float scaredShare;
        [SerializeField] private float incapacitatedShare;
        [SerializeField] private float stubbornShare;

        // Unity's serializer needs a parameterless constructor.
        public MissionFillConstants()
        {
        }

        public MissionFillConstants(
            int bankValuablesMin,
            int bankValuablesMax,
            int storeValuablesMin,
            int storeValuablesMax,
            float buildingValuableChance,
            float civilianValuableChance,
            float heavyTurretChance,
            float scaredShare,
            float incapacitatedShare,
            float stubbornShare)
        {
            this.bankValuablesMin = bankValuablesMin;
            this.bankValuablesMax = bankValuablesMax;
            this.storeValuablesMin = storeValuablesMin;
            this.storeValuablesMax = storeValuablesMax;
            this.buildingValuableChance = buildingValuableChance;
            this.civilianValuableChance = civilianValuableChance;
            this.heavyTurretChance = heavyTurretChance;
            this.scaredShare = scaredShare;
            this.incapacitatedShare = incapacitatedShare;
            this.stubbornShare = stubbornShare;
        }

        public int BankValuablesMin => bankValuablesMin;
        public int BankValuablesMax => bankValuablesMax;
        public int StoreValuablesMin => storeValuablesMin;
        public int StoreValuablesMax => storeValuablesMax;

        /// <summary>Chance any other building holds 1 Valuable.</summary>
        public float BuildingValuableChance => buildingValuableChance;

        /// <summary>Chance a civilian carries 1 Valuable.</summary>
        public float CivilianValuableChance => civilianValuableChance;

        /// <summary>Chance per building to have a Heavy Turret, from rank B.</summary>
        public float HeavyTurretChance => heavyTurretChance;

        /// <summary>Relative shares of spawned citizens' kinds: Scared, Incapacitated, Stubborn.</summary>
        public float ScaredShare => scaredShare;

        public float IncapacitatedShare => incapacitatedShare;
        public float StubbornShare => stubbornShare;
    }
}
