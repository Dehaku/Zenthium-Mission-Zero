using NUnit.Framework;
using Zenthium.Data;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// balance.md sections 1 and 4 at the rank D anchor inputs (section 3). Constants are the first-pass
    /// values from constants.csv, written out so the tests don't read game data.
    /// </summary>
    public class BalanceFormulasTests
    {
        private const float Tolerance = 0.0001f;

        private static BalanceFormulas CreateFormulas()
        {
            return new BalanceFormulas(new FormulaConstants(
                hpPerToughness: 10f,
                leaderHpMultiplier: 2f,
                armorPerToughness: 0.5f,
                normalDamageBase: 10f,
                normalDamagePerStat: 2f,
                kickCooldown: 3f,
                minDamageFraction: 0.05f,
                dodgePerFinesse: 0.005f,
                dodgeCap: 0.75f,
                visionRange: 100f,
                awarenessInstantRange: 10f,
                awarenessRange: 100f,
                awarenessMaxTime: 10f,
                resourceSecondsPerPoint: 1f,
                enemyDefaultDamageTier: new TierValue("Light", 0.5f)));
        }

        [TestCase(10f, false, 100f, TestName = "MaxHp_RankDGoon")]
        [TestCase(20f, false, 200f, TestName = "MaxHp_RankDChampion")]
        [TestCase(30f, true, 600f, TestName = "MaxHp_RankDLeader_DoubledForLeaders")]
        public void MaxHp_IsToughnessTimesHpPerToughness(float toughness, bool isLeader, float expected)
        {
            Assert.That(CreateFormulas().MaxHp(toughness, isLeader), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(10f, 5f)]
        [TestCase(20f, 10f)]
        [TestCase(30f, 15f)]
        public void Armor_IsToughnessTimesArmorPerToughness(float toughness, float expected)
        {
            Assert.That(CreateFormulas().Armor(toughness), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(10f, 30f, TestName = "NormalDamage_KickFromStrength10")]
        [TestCase(20f, 50f, TestName = "NormalDamage_KickFromStrength20")]
        public void NormalDamage_IsBasePlusPerStat(float stat, float expected)
        {
            Assert.That(CreateFormulas().NormalDamage(stat), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(10f, "Light", 0.5f, 15f, TestName = "TierDamage_GoonHitAtDefaultEnemyTier")]
        [TestCase(20f, "Heavy", 2f, 100f, TestName = "TierDamage_HeavyFromStat20")]
        public void TierDamage_MultipliesNormalDamage(float stat, string word, float multiplier, float expected)
        {
            float damage = CreateFormulas().TierDamage(stat, new TierValue(word, multiplier));

            Assert.That(damage, Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(20f, 0.1f, TestName = "DodgeChance_Finesse20")]
        [TestCase(150f, 0.75f, TestName = "DodgeChance_ExactlyAtCap")]
        [TestCase(400f, 0.75f, TestName = "DodgeChance_CappedAboveCap")]
        public void DodgeChance_IsFinesseTimesPerPointUpToCap(float finesse, float expected)
        {
            Assert.That(CreateFormulas().DodgeChance(finesse), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(0f, 0f, TestName = "AwarenessFillSeconds_TouchingIsInstant")]
        [TestCase(10f, 0f, TestName = "AwarenessFillSeconds_InstantAt10m")]
        [TestCase(55f, 5f, TestName = "AwarenessFillSeconds_HalfwayTakesHalfTime")]
        [TestCase(100f, 10f, TestName = "AwarenessFillSeconds_10sAt100m")]
        public void AwarenessFillSeconds_ScalesLinearlyBetweenInstantAndVisionRange(float distance, float expected)
        {
            Assert.That(CreateFormulas().AwarenessFillSeconds(distance), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void AwarenessFillSeconds_BeyondVisionRange_NeverFills()
        {
            Assert.That(CreateFormulas().AwarenessFillSeconds(101f), Is.EqualTo(float.PositiveInfinity));
        }

        // The user's tuning (2026-11-25): instant at 1 m, about 3 s at 10 m, nothing past 15 m though vision is 100 m.
        [TestCase(1f, 0f, TestName = "AwarenessFillSeconds_ShortRange_InstantAt1m")]
        [TestCase(10f, 3f, TestName = "AwarenessFillSeconds_ShortRange_About3sAt10m")]
        [TestCase(20f, float.PositiveInfinity, TestName = "AwarenessFillSeconds_ShortRange_NeverPast15m")]
        public void AwarenessFillSeconds_AwarenessRangeShorterThanVision_UsesTheAwarenessRange(float distance,
            float expected)
        {
            var formulas = new BalanceFormulas(new FormulaConstants(
                hpPerToughness: 10f,
                leaderHpMultiplier: 2f,
                armorPerToughness: 0.5f,
                normalDamageBase: 10f,
                normalDamagePerStat: 2f,
                kickCooldown: 3f,
                minDamageFraction: 0.05f,
                dodgePerFinesse: 0.005f,
                dodgeCap: 0.75f,
                visionRange: 100f,
                awarenessInstantRange: 1f,
                awarenessRange: 15f,
                awarenessMaxTime: 4.67f,
                resourceSecondsPerPoint: 1f,
                enemyDefaultDamageTier: new TierValue("Light", 0.5f)));

            Assert.That(formulas.AwarenessFillSeconds(distance), Is.EqualTo(expected).Within(0.01f));
        }
    }
}
