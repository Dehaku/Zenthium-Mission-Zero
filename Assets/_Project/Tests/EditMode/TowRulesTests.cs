using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// modules.md, Tow Beam: it grabs a prop, citizen or Valuable, and flies it to the loot destination; with none,
    /// the map edge (world.md).
    /// </summary>
    public class TowRulesTests
    {
        [TestCase(DragTargetKind.Citizen, true)]
        [TestCase(DragTargetKind.Prop, true)]
        [TestCase(DragTargetKind.Valuable, true)]
        [TestCase(DragTargetKind.IncapacitatedEntity, false)]
        public void CanTow_PropsCitizensAndValuablesOnly(DragTargetKind kind, bool expected)
        {
            Assert.That(TowRules.CanTow(kind), Is.EqualTo(expected));
        }

        [TestCase(true, LootDelivery.CropCircle)]
        [TestCase(false, LootDelivery.MapEdge)]
        public void TowDelivery_CropCircleElseTheMapEdge(bool hasCropCircle, LootDelivery expected)
        {
            Assert.That(LootRules.TowDelivery(hasCropCircle), Is.EqualTo(expected));
        }
    }
}
