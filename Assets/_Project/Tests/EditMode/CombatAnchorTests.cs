using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The rank D anchors of balance.md section 3 through the full damage pipeline, for starting heroes
    /// with key stats of 10 to 20. Expected counts were worked out by hand (Docs/BuildLog.md, Milestone 1).
    /// Rolls are fixed so nothing dodges and Defiance never procs.
    /// </summary>
    public class CombatAnchorTests
    {
        private const int GiveUpAfter = 1000;

        private static int HitsToIncapacitate(Entity source, float damagePerHit, Entity target)
        {
            DamagePipeline pipeline = CombatTestSetup.CreatePipeline();
            int hits = 0;
            while (!target.IsIncapacitated && hits < GiveUpAfter)
            {
                pipeline.Apply(new Hit(source, damagePerHit, DamageType.Physical), target);
                hits++;
            }

            return hits;
        }

        // Kick is Normal damage from Strength: 30 at Strength 10, 50 at Strength 20.
        [TestCase(RankTier.Goon, 50f, 3, TestName = "Kicks_Goon_Strength20_Takes3")]
        [TestCase(RankTier.Goon, 30f, 4, TestName = "Kicks_Goon_Strength10_Takes4")]
        [TestCase(RankTier.Champion, 50f, 5, TestName = "Kicks_Champion_Strength20_Takes5")]
        [TestCase(RankTier.Champion, 30f, 10, TestName = "Kicks_Champion_Strength10_Takes10")]
        [TestCase(RankTier.Leader, 50f, 18, TestName = "Kicks_Leader_Strength20_Takes18")]
        [TestCase(RankTier.Leader, 30f, 40, TestName = "Kicks_Leader_Strength10_Takes40")]
        public void KicksToDefeat_RankD(RankTier tier, float kickDamage, int expectedKicks)
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            Entity enemy = CombatTestSetup.CreateEntity(Team.Enemy, tier);

            Assert.That(HitsToIncapacitate(hero, kickDamage, enemy), Is.EqualTo(expectedKicks));
        }

        // A rank D goon hits at the enemy default tier, Light: half of Normal damage at Strength 10 is 15.
        [TestCase(10f, 10, TestName = "GoonHits_HeroToughness10_Takes10")]
        [TestCase(20f, 40, TestName = "GoonHits_HeroToughness20_Takes40")]
        public void GoonHitsToDefeatAStartingHero(float heroToughness, int expectedHits)
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            Entity hero = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(10f, 10f, 10f, 10f, heroToughness));

            Assert.That(HitsToIncapacitate(goon, 15f, hero), Is.EqualTo(expectedHits));
        }
    }
}
