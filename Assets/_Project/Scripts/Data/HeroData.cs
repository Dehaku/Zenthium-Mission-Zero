using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A preset hero's starting stats, from a row of heroes.csv.
    /// </summary>
    public sealed class HeroData : DataEntry
    {
        [SerializeField] private int strength;
        [SerializeField] private int will;
        [SerializeField] private int intelligence;
        [SerializeField] private int finesse;
        [SerializeField] private int toughness;
        [SerializeField] private bool isStarter;

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

        internal void Populate(
            int newStrength, int newWill, int newIntelligence, int newFinesse, int newToughness, bool newIsStarter)
        {
            strength = newStrength;
            will = newWill;
            intelligence = newIntelligence;
            finesse = newFinesse;
            toughness = newToughness;
            isStarter = newIsStarter;
        }
    }
}
