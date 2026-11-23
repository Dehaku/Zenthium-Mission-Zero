using NUnit.Framework;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// modules.md (Drag row): carry capacity grows with Strength, and targets are picked citizens first, then
    /// Valuables, then any other Incapacitated entity, then props. Valuables don't use capacity (world.md).
    /// </summary>
    public class DragRulesTests
    {
        // modules.csv: capacity_base=1; capacity_per_strength=0.01.
        private static DragRules CreateRules()
        {
            return new DragRules(1f, 0.01f);
        }

        [TestCase(10f, 1, TestName = "Capacity_Strength10_Is1")]
        [TestCase(20f, 1, TestName = "Capacity_Strength20_Is1")]
        [TestCase(99f, 1, TestName = "Capacity_Strength99_Is1")]
        [TestCase(100f, 2, TestName = "Capacity_Strength100_Is2")]
        [TestCase(250f, 3, TestName = "Capacity_Strength250_Is3")]
        public void Capacity_IsTheWholePartOfTheFormula(float strength, int expected)
        {
            Assert.That(CreateRules().Capacity(strength), Is.EqualTo(expected));
        }

        [Test]
        public void SelectTarget_PicksByKindBeforeDistance()
        {
            var candidates = new[]
            {
                new DragCandidate(DragTargetKind.Prop, 0.5f, true),
                new DragCandidate(DragTargetKind.IncapacitatedEntity, 1f, true),
                new DragCandidate(DragTargetKind.Citizen, 1.9f, true),
                new DragCandidate(DragTargetKind.Valuable, 0.2f, false),
            };

            Assert.That(DragRules.SelectTarget(candidates, capacityLeft: 1), Is.EqualTo(2));
        }

        [Test]
        public void SelectTarget_SameKind_PicksTheNearest()
        {
            var candidates = new[]
            {
                new DragCandidate(DragTargetKind.IncapacitatedEntity, 1.5f, true),
                new DragCandidate(DragTargetKind.IncapacitatedEntity, 0.7f, true),
                new DragCandidate(DragTargetKind.IncapacitatedEntity, 1.1f, true),
            };

            Assert.That(DragRules.SelectTarget(candidates, capacityLeft: 1), Is.EqualTo(1));
        }

        [Test]
        public void SelectTarget_NoCapacityLeft_SkipsTargetsThatUseIt()
        {
            var candidates = new[]
            {
                new DragCandidate(DragTargetKind.Citizen, 0.5f, true),
                new DragCandidate(DragTargetKind.Valuable, 1.5f, false),
                new DragCandidate(DragTargetKind.IncapacitatedEntity, 0.5f, true),
            };

            Assert.That(DragRules.SelectTarget(candidates, capacityLeft: 0), Is.EqualTo(1));
        }

        [Test]
        public void SelectTarget_NothingGrabbable_ReturnsMinusOne()
        {
            var candidates = new[] { new DragCandidate(DragTargetKind.IncapacitatedEntity, 1f, true) };

            Assert.That(DragRules.SelectTarget(candidates, capacityLeft: 0), Is.EqualTo(-1));
            Assert.That(DragRules.SelectTarget(new DragCandidate[0], capacityLeft: 1), Is.EqualTo(-1));
        }
    }
}
