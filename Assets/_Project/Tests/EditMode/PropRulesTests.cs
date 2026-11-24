using NUnit.Framework;
using UnityEngine;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md (Props) with props.csv's first-pass rows: Strength to Drag and throw, thrown damage as a multiple of
    /// Kick, Small props breaking, and vehicles exploding.
    /// </summary>
    public class PropRulesTests
    {
        private static PropData CreateProp(PropSize size, float strengthNeeded, float thrownXKick,
            float explosionDamage = 0f, float explosionRadius = 0f)
        {
            var prop = ScriptableObject.CreateInstance<PropData>();
            prop.Populate(size, strengthNeeded, 100f, thrownXKick, 0f, 0f, explosionDamage, explosionRadius);
            return prop;
        }

        [TestCase(249f, false, TestName = "CanLift_BelowTheStrengthNeeded_CantLift")]
        [TestCase(250f, true, TestName = "CanLift_ExactlyTheStrengthNeeded_CanLift")]
        [TestCase(400f, true, TestName = "CanLift_AboveTheStrengthNeeded_CanLift")]
        public void CanLift_LargeProp_Needs250Strength(float strength, bool expected)
        {
            Assert.That(PropRules.CanLift(strength, CreateProp(PropSize.Large, 250f, 3f)), Is.EqualTo(expected));
        }

        [Test]
        public void CanLift_SmallProp_AnyoneCanLift()
        {
            Assert.That(PropRules.CanLift(0f, CreateProp(PropSize.Small, 0f, 1f)), Is.True);
        }

        [Test]
        public void ThrownDamage_IsKickDamageTimesTheFactor()
        {
            // Strength 20: Kick deals (10 + 20 x 2) x 1 = 50; a Large prop hits for x3.
            Assert.That(PropRules.ThrownDamage(50f, CreateProp(PropSize.Large, 250f, 3f)), Is.EqualTo(150f));
        }

        [TestCase(PropSize.Small, true)]
        [TestCase(PropSize.Medium, false)]
        [TestCase(PropSize.Large, false)]
        public void BreaksWhenThrown_OnlySmallProps(PropSize size, bool expected)
        {
            Assert.That(PropRules.BreaksWhenThrown(CreateProp(size, 0f, 1f)), Is.EqualTo(expected));
        }

        [Test]
        public void Explodes_VehicleWithExplosionDamage_Explodes()
        {
            Assert.That(PropRules.Explodes(CreateProp(PropSize.Large, 250f, 3f, 500f, 15f)), Is.True);
        }

        [Test]
        public void Explodes_PlainProp_DoesNot()
        {
            Assert.That(PropRules.Explodes(CreateProp(PropSize.Large, 250f, 3f)), Is.False);
        }
    }
}
