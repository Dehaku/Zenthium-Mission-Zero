using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Local design decisions: walls, doors and windows take extra damage from knockback impacts, thrown props and
    /// Demolition modules; props and direct hits don't.
    /// </summary>
    public class StructureDamageRulesTests
    {
        private static WorldConstants CreateConstants()
        {
            return new WorldConstants(
                materialHp: new[] { 10f, 100f, 300f, 600f, 1000f },
                impactStructureDamageTier: new TierValue("Heavy", 2f),
                impactEntityDamageTier: new TierValue("Light", 0.5f),
                buildingImpactMultiplier: 10f,
                buildingThrowMultiplier: 2f,
                demolitionMultiplier: 8f,
                vehicleExplosionDelay: 1f,
                buildingNeighborRadius: 20f);
        }

        [TestCase(StructureHitKind.Direct, true, 1f, TestName = "Multiplier_DirectHitOnAWall_IsOne")]
        [TestCase(StructureHitKind.Impact, true, 10f, TestName = "Multiplier_ImpactOnAWall_IsTheImpactMultiplier")]
        [TestCase(StructureHitKind.Demolition, true, 8f, TestName = "Multiplier_DemolitionOnAWall_IsTheDemolitionMultiplier")]
        [TestCase(StructureHitKind.Thrown, true, 2f, TestName = "Multiplier_ThrownPropOnAWall_IsTheThrowMultiplier")]
        [TestCase(StructureHitKind.Impact, false, 1f, TestName = "Multiplier_ImpactOnAProp_IsOne")]
        [TestCase(StructureHitKind.Thrown, false, 1f, TestName = "Multiplier_ThrownPropOnAProp_IsOne")]
        [TestCase(StructureHitKind.Demolition, false, 1f, TestName = "Multiplier_DemolitionOnAProp_IsOne")]
        public void Multiplier_ByHitAndStructure(StructureHitKind kind, bool isBuildingPart, float expected)
        {
            Assert.That(StructureDamageRules.Multiplier(kind, isBuildingPart, CreateConstants()), Is.EqualTo(expected));
        }
    }
}
