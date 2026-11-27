using NUnit.Framework;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>Sockets offer weighted options (maps.md, Sockets), and missions pick by those weights.</summary>
    public class SocketPickerTests
    {
        [TestCase(SocketContent.CropCircle, 2.5f)]
        [TestCase(SocketContent.Citizen, 1f)]
        [TestCase(SocketContent.Goon, 0f)]
        public void WeightFor_SumsTheMatchingOptions(SocketContent content, float expected)
        {
            var options = new[]
            {
                new SocketOption(SocketContent.CropCircle, 2f),
                new SocketOption(SocketContent.Citizen, 1f),
                new SocketOption(SocketContent.CropCircle, 0.5f),
            };

            Assert.That(SocketPicker.WeightFor(options, content), Is.EqualTo(expected).Within(0.001f));
        }

        // Weights 1, 0 and 3: the first takes the bottom quarter of the roll, the zero is never picked.
        [TestCase(0f, 0)]
        [TestCase(0.2f, 0)]
        [TestCase(0.25f, 2)]
        [TestCase(0.9f, 2)]
        [TestCase(1f, 2)]
        public void Pick_ChanceIsItsShareOfTheWeight(float roll, int expected)
        {
            Assert.That(SocketPicker.Pick(new[] { 1f, 0f, 3f }, roll), Is.EqualTo(expected));
        }

        [Test]
        public void Pick_NoWeight_PicksNothing()
        {
            Assert.That(SocketPicker.Pick(new[] { 0f, 0f }, 0.5f), Is.EqualTo(-1));
        }

        [TestCase(0.1f, SocketContent.Valuables)]
        [TestCase(0.5f, SocketContent.Citizen)]
        public void TryRollContent_RollsByTheSocketsWeights(float roll, SocketContent expected)
        {
            var options = new[]
            {
                new SocketOption(SocketContent.Valuables, 1f),
                new SocketOption(SocketContent.Citizen, 3f),
            };

            bool rolled = SocketPicker.TryRollContent(options, roll, out SocketContent content);

            Assert.That(rolled, Is.True);
            Assert.That(content, Is.EqualTo(expected));
        }

        [Test]
        public void TryRollContent_NoOptions_Fails()
        {
            Assert.That(SocketPicker.TryRollContent(new SocketOption[0], 0.5f, out _), Is.False);
        }
    }
}
