using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// What an <see cref="Entity"/> is built from. Heroes, enemies and civilians differ only in these values.
    /// </summary>
    public sealed class EntitySetup
    {
        /// <param name="rankTier">Goon, Champion or Leader. Heroes and civilians have none, so no innate boost.</param>
        /// <param name="difficultyLevel">Mission ranks above D (0 at D, 4 at S, 5 at S2), for rank scaling.</param>
        public EntitySetup(Team team, StatBlock baseStats, RankTier? rankTier = null, int difficultyLevel = 0)
        {
            if (difficultyLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(difficultyLevel), difficultyLevel, "Rank D is 0.");
            }

            Team = team;
            BaseStats = baseStats;
            RankTier = rankTier;
            DifficultyLevel = difficultyLevel;
        }

        public Team Team { get; }
        public StatBlock BaseStats { get; }
        public RankTier? RankTier { get; }
        public int DifficultyLevel { get; }

        /// <summary>The entity's modules. A stand-in for Milestone 4's slots: for now they only grant resources.</summary>
        public List<ModuleData> Modules { get; } = new List<ModuleData>();

        public List<EntityTag> Tags { get; } = new List<EntityTag>();

        /// <summary>A preset hero, with its own stats and no rank scaling.</summary>
        public static EntitySetup ForHero(HeroData hero, Team team)
        {
            if (hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return new EntitySetup(team, StatBlock.FromHero(hero));
        }

        /// <summary>A faction entity: the base stat in every stat, with its rank tier and the mission's rank scaling.</summary>
        public static EntitySetup ForFactionEntity(
            EntityData entity, Team team, int difficultyLevel, EntityConstants constants)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return new EntitySetup(team, StatBlock.Uniform(constants.BaseStat), entity.RankTier, difficultyLevel);
        }
    }
}
