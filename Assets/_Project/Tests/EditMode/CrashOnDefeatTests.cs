using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Factions/GalacticRepoAgency.md: a Tow Saucer crashes when destroyed, damaging everything nearby, with its
    /// entities.csv values (crash_damage=50; crash_radius_m=5). An entity without them doesn't crash.
    /// </summary>
    public class CrashOnDefeatTests
    {
        [Test]
        public void TryRead_BothValues_ReadsThem()
        {
            var values = new DataValueList(new List<DataValue>
            {
                DataValue.FromNumber("crash_damage", 50f),
                DataValue.FromNumber("crash_radius_m", 5f),
            });

            bool read = CrashOnDefeat.TryRead(values, out float damage, out float radius);

            Assert.That(read, Is.True);
            Assert.That(damage, Is.EqualTo(50f));
            Assert.That(radius, Is.EqualTo(5f));
        }

        [Test]
        public void TryRead_NoRadius_DoesNotCrash()
        {
            var values = new DataValueList(new List<DataValue> { DataValue.FromNumber("crash_damage", 50f) });

            Assert.That(CrashOnDefeat.TryRead(values, out _, out _), Is.False);
        }
    }
}
