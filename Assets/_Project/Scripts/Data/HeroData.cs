using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A preset hero's starting stats and fixed kit, from a row of heroes.csv.
    /// </summary>
    public sealed class HeroData : DataEntry
    {
        [SerializeField] private int strength;
        [SerializeField] private int will;
        [SerializeField] private int intelligence;
        [SerializeField] private int finesse;
        [SerializeField] private int toughness;
        [SerializeField] private bool isStarter;
        [SerializeField] private List<ModuleData> actives = new List<ModuleData>();
        [SerializeField] private List<ModuleData> passives = new List<ModuleData>();
        [SerializeField] private List<ModuleData> gear = new List<ModuleData>();

        /// <summary>One of the heroes the player starts with.</summary>
        public bool IsStarter => isStarter;

        public int GetBaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.Strength: return strength;
                case Stat.Will: return will;
                case Stat.Intelligence: return intelligence;
                case Stat.Finesse: return finesse;
                case Stat.Toughness: return toughness;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        /// <summary>The kit's modules for one slot type, in slot (and unlock) order (heroes.md, Kits).</summary>
        public IReadOnlyList<ModuleData> GetKit(SlotType slotType)
        {
            switch (slotType)
            {
                case SlotType.Active: return actives;
                case SlotType.Passive: return passives;
                case SlotType.Gear: return gear;
                default: throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
            }
        }

        internal void Populate(
            int newStrength, int newWill, int newIntelligence, int newFinesse, int newToughness, bool newIsStarter,
            List<ModuleData> newActives, List<ModuleData> newPassives, List<ModuleData> newGear)
        {
            strength = newStrength;
            will = newWill;
            intelligence = newIntelligence;
            finesse = newFinesse;
            toughness = newToughness;
            isStarter = newIsStarter;
            actives = newActives;
            passives = newPassives;
            gear = newGear;
        }
    }
}
