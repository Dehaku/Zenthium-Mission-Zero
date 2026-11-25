using NUnit.Framework;
using Zenthium.AI;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// enemies.md, Using modules: in combat, the first module in list order that's off cooldown, affordable and has
    /// a target in range.
    /// </summary>
    public class ModuleChooserTests
    {
        [Test]
        public void FirstUsable_SkipsNotReadyAndOutOfReach_PicksTheFirstUsableInListOrder()
        {
            ModuleOption[] options =
            {
                new ModuleOption(false, 20f),
                new ModuleOption(true, 2f),
                new ModuleOption(true, 10f),
                new ModuleOption(true, 15f),
            };

            Assert.That(ModuleChooser.FirstUsable(options, 5f), Is.EqualTo(2));
        }

        [Test]
        public void FirstUsable_EarlierModuleReadyAndInReach_WinsOverLongerReach()
        {
            ModuleOption[] options =
            {
                new ModuleOption(true, 6f),
                new ModuleOption(true, 30f),
            };

            Assert.That(ModuleChooser.FirstUsable(options, 6f), Is.EqualTo(0));
        }

        [Test]
        public void FirstUsable_NoneReadyOrInReach_ReturnsMinusOne()
        {
            ModuleOption[] options =
            {
                new ModuleOption(false, 20f),
                new ModuleOption(true, 2f),
            };

            Assert.That(ModuleChooser.FirstUsable(options, 5f), Is.EqualTo(-1));
        }

        [Test]
        public void FirstUsable_InfiniteReach_IsAlwaysInReach()
        {
            ModuleOption[] options = { new ModuleOption(true, float.PositiveInfinity) };

            Assert.That(ModuleChooser.FirstUsable(options, 90f), Is.EqualTo(0));
        }
    }
}
