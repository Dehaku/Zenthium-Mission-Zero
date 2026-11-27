using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// factions.md, Shared rules: a faction entity uses its module list in order, minus modules locked until a
    /// higher rank, and has its own tags plus its faction's.
    /// </summary>
    public class EntitySetupTests
    {
        private static EntitySetup Build(EntityData entity, int difficultyLevel)
        {
            return EntitySetup.ForFactionEntity(entity, Team.Enemy, difficultyLevel,
                CombatTestSetup.CreateRules().EntityConstants);
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(5, true)]
        public void ForFactionEntity_ModuleLockedUntilC_IsLeftOutBelowC(int difficultyLevel, bool expected)
        {
            ModuleData slap = TestModules.Active("Slap");
            ModuleData notice = TestModules.Active("Notice");
            EntityData leader = TestFactions.Entity("Leader", TestFactions.Faction("Repo"), RankTier.Leader,
                modules: new[] { notice, slap }, locks: new[] { new ModuleLock(notice, Rank.C) });

            EntitySetup setup = Build(leader, difficultyLevel);

            Assert.That(setup.Modules.Contains(notice), Is.EqualTo(expected));
            Assert.That(setup.Modules.Contains(slap), Is.True);
        }

        [Test]
        public void ForFactionEntity_KeepsTheListOrderWithoutLockedModules()
        {
            ModuleData first = TestModules.Active("First");
            ModuleData locked = TestModules.Active("Locked");
            ModuleData last = TestModules.Active("Last");
            EntityData leader = TestFactions.Entity("Leader", TestFactions.Faction("Repo"), RankTier.Leader,
                modules: new[] { first, locked, last }, locks: new[] { new ModuleLock(locked, Rank.B) });

            EntitySetup setup = Build(leader, 1);

            Assert.That(setup.Modules, Is.EqualTo(new[] { first, last }));
        }

        [Test]
        public void ForFactionEntity_HasItsOwnTagsAndItsFactionsOnce()
        {
            FactionData repo = TestFactions.Faction("Repo", EntityTag.LootDestinationCropCircle, EntityTag.Coward);
            EntityData grey = TestFactions.Entity("Grey", repo, RankTier.Goon, tags: new[] { EntityTag.Coward });

            EntitySetup setup = Build(grey, 0);

            Assert.That(setup.Tags, Is.EquivalentTo(new[] { EntityTag.Coward, EntityTag.LootDestinationCropCircle }));
        }

        [Test]
        public void Entity_WithFiveActives_GetsAFifthActiveSlot()
        {
            var actives = new ModuleData[5];
            for (int i = 0; i < actives.Length; i++)
            {
                actives[i] = TestModules.Active($"Active {i + 1}");
            }

            EntityData leader = TestFactions.Entity("Leader", TestFactions.Faction("Repo"), RankTier.Leader,
                modules: actives);

            var entity = new Entity(Build(leader, 4), CombatTestSetup.CreateRules(),
                new FixedRandom(CombatTestSetup.RollThatMisses));

            Assert.That(entity.Slots.SlotsPerType, Is.EqualTo(5));
            Assert.That(entity.Slots.Contains(actives[4]), Is.True);
        }

        [TestCase(Rank.B, 1, false)]
        [TestCase(Rank.B, 2, true)]
        [TestCase(Rank.D, 0, true)]
        public void SpawnsAt_OnlyFromItsMinimumRank(Rank minRank, int difficultyLevel, bool expected)
        {
            EntityData saucer = TestFactions.Entity("Saucer", TestFactions.Faction("Repo"), RankTier.Goon, minRank);

            Assert.That(saucer.SpawnsAt(difficultyLevel), Is.EqualTo(expected));
        }
    }
}
