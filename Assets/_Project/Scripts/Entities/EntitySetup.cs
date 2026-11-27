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
        /// <summary>Every slot unlocked: faction entities' fixed module lists aren't gated by unlocks.</summary>
        public const int AllSlotsUnlocked = int.MaxValue;

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

        /// <summary>
        /// The entity's modules. Each goes in the next slot of its type, in list order (heroes.md, Slots). Base
        /// actions are always available outside the slots, so they're skipped.
        /// </summary>
        public List<ModuleData> Modules { get; } = new List<ModuleData>();

        /// <summary>How many slots of each type are unlocked, counted from the first.</summary>
        public int UnlockedSlotsPerType { get; set; } = AllSlotsUnlocked;

        public List<EntityTag> Tags { get; } = new List<EntityTag>();

        /// <summary>The entity that summoned this one, such as Rune for an Ink Beast; null for everyone else.</summary>
        public Entity Summoner { get; set; }

        /// <summary>
        /// Slots of each type it gets at least, beyond <c>slots_per_type</c>: a faction entity's fixed list can be
        /// longer than a hero's slots (GleebPeep has five actives).
        /// </summary>
        public int MinimumSlotsPerType { get; set; }

        /// <summary>A preset hero: its own stats, no rank scaling, and its fixed kit in slot order.</summary>
        /// <param name="unlockedSlotsPerType">Usually the preset's starting unlocks (<c>preset_slots_unlocked</c>).</param>
        public static EntitySetup ForHero(HeroData hero, Team team, int unlockedSlotsPerType)
        {
            if (hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            var setup = new EntitySetup(team, StatBlock.FromHero(hero)) { UnlockedSlotsPerType = unlockedSlotsPerType };
            setup.Modules.AddRange(hero.GetKit(SlotType.Active));
            setup.Modules.AddRange(hero.GetKit(SlotType.Passive));
            setup.Modules.AddRange(hero.GetKit(SlotType.Gear));
            return setup;
        }

        /// <summary>
        /// A faction entity: the base stat in every stat, with its rank tier and the mission's rank scaling; its
        /// modules in priority order, leaving out the ones still locked at the mission's rank (factions.md, Locked
        /// modules); and its own tags with its faction's.
        /// </summary>
        public static EntitySetup ForFactionEntity(
            EntityData entity, Team team, int difficultyLevel, EntityConstants constants)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var setup = new EntitySetup(team, StatBlock.Uniform(constants.BaseStat), entity.RankTier, difficultyLevel);
            foreach (ModuleData module in entity.Modules)
            {
                if (module != null && entity.IsUnlocked(module, difficultyLevel))
                {
                    setup.Modules.Add(module);
                }
            }

            setup.MinimumSlotsPerType = MostOfOneType(setup.Modules);
            setup.Tags.AddRange(entity.Tags);
            if (entity.Faction != null)
            {
                foreach (EntityTag tag in entity.Faction.Tags)
                {
                    if (!setup.Tags.Contains(tag))
                    {
                        setup.Tags.Add(tag);
                    }
                }
            }

            return setup;
        }

        /// <summary>The most modules of one slot type in the list; base actions don't take slots.</summary>
        public static int MostOfOneType(IReadOnlyList<ModuleData> modules)
        {
            var perType = new int[Enum.GetValues(typeof(SlotType)).Length];
            int most = 0;
            foreach (ModuleData module in modules)
            {
                if (module != null && module.TryGetSlotType(out SlotType type))
                {
                    most = Math.Max(most, ++perType[(int)type]);
                }
            }

            return most;
        }
    }
}
