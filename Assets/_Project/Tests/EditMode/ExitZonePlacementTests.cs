using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Exit zones go on the map's edges (missions.md, Exits): in the middle of the outer street, spread evenly. On a
    /// 172 m map with 12 m streets, the outer street's middle line is 80 m from the center.
    /// </summary>
    public class ExitZonePlacementTests
    {
        private const float Side = 172f;
        private const float Street = 12f;

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void Place_MakesOneZonePerCount(int count)
        {
            var points = new List<Vector2>();

            ExitZonePlacement.Place(Side, Street, count, 0.3f, points);

            Assert.That(points.Count, Is.EqualTo(count));
        }

        [TestCase(0.1f)]
        [TestCase(0.6f)]
        public void Place_EveryZoneIsInTheOuterStreet(float startRoll)
        {
            var points = new List<Vector2>();

            ExitZonePlacement.Place(Side, Street, 5, startRoll, points);

            foreach (Vector2 point in points)
            {
                float fromCenter = Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y));
                Assert.That(fromCenter, Is.EqualTo(80f).Within(0.001f), $"{point} isn't in the outer street.");
            }
        }

        [Test]
        public void Place_FourZones_AreSpreadEvenly()
        {
            var points = new List<Vector2>();

            ExitZonePlacement.Place(Side, Street, 4, 0f, points);

            Assert.That(points[0], Is.EqualTo(new Vector2(-80f, -80f)));
            Assert.That(points[1], Is.EqualTo(new Vector2(80f, -80f)));
            Assert.That(points[2], Is.EqualTo(new Vector2(80f, 80f)));
            Assert.That(points[3], Is.EqualTo(new Vector2(-80f, 80f)));
        }

        [TestCase(0.125f, 0f, -80f)]
        [TestCase(0.375f, 80f, 0f)]
        [TestCase(0.875f, -80f, 0f)]
        public void Place_StartRoll_SetsWhereTheFirstZoneGoes(float startRoll, float expectedX, float expectedZ)
        {
            var points = new List<Vector2>();

            ExitZonePlacement.Place(Side, Street, 1, startRoll, points);

            Assert.That(points[0].x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(points[0].y, Is.EqualTo(expectedZ).Within(0.001f));
        }

        [TestCase(-80f, -80f, 80f, 80f)]
        [TestCase(80f, 20f, -80f, -80f)]
        [TestCase(-80f, 80f, 80f, -80f)]
        public void PlaceAwayFrom_OneZone_GoesAcrossTheMap(float fromX, float fromZ, float expectedX, float expectedZ)
        {
            var points = new List<Vector2>();

            ExitZonePlacement.PlaceAwayFrom(Side, Street, 1, new Vector2(fromX, fromZ), 16, points);

            Assert.That(points, Has.Count.EqualTo(1));
            Assert.That(points[0].x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(points[0].y, Is.EqualTo(expectedZ).Within(0.001f));
        }

        [Test]
        public void PlaceAwayFrom_TwoZones_TurnSoNeitherIsNextToThePoint()
        {
            var points = new List<Vector2>();

            // From the middle of the south edge, two zones opposite each other keep farthest from it at the middles
            // of the east and west edges, 113 m away.
            ExitZonePlacement.PlaceAwayFrom(Side, Street, 2, new Vector2(0f, -80f), 16, points);

            Assert.That(points, Has.Count.EqualTo(2));
            foreach (Vector2 point in points)
            {
                Assert.That(Vector2.Distance(point, new Vector2(0f, -80f)), Is.GreaterThan(100f));
            }
        }
    }
}
