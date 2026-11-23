using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// One value per SWIFT stat.
    /// </summary>
    public readonly struct StatBlock
    {
        private readonly float strength;
        private readonly float will;
        private readonly float intelligence;
        private readonly float finesse;
        private readonly float toughness;

        public StatBlock(float strength, float will, float intelligence, float finesse, float toughness)
        {
            this.strength = strength;
            this.will = will;
            this.intelligence = intelligence;
            this.finesse = finesse;
            this.toughness = toughness;
        }

        public static StatBlock Uniform(float value)
        {
            return new StatBlock(value, value, value, value, value);
        }

        public static StatBlock FromHero(HeroData hero)
        {
            return new StatBlock(
                hero.GetBaseStat(Stat.Strength),
                hero.GetBaseStat(Stat.Will),
                hero.GetBaseStat(Stat.Intelligence),
                hero.GetBaseStat(Stat.Finesse),
                hero.GetBaseStat(Stat.Toughness));
        }

        public float Get(Stat stat)
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
    }
}
