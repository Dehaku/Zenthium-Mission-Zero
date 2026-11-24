using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md (Buildings): neighbors are buildings within 20 m, measured edge to edge between interior zones, so an
    /// L-shaped building's far wing counts.
    /// </summary>
    public class BuildingNeighborsTests
    {
        private const float Radius = 20f;
        private const float Tolerance = 0.0001f;

        private static List<List<int>> Compute(params Rect[][] buildings)
        {
            var footprints = new List<IReadOnlyList<Rect>>();
            foreach (Rect[] zones in buildings)
            {
                footprints.Add(zones);
            }

            var results = new List<List<int>>();
            BuildingNeighbors.Compute(footprints, Radius, results);
            return results;
        }

        [TestCase(0f, 0f, 10f, 10f, 0f, TestName = "Distance_Overlapping_IsZero")]
        [TestCase(30f, 0f, 40f, 10f, 20f, TestName = "Distance_SideBySide_IsTheGap")]
        [TestCase(13f, 14f, 20f, 20f, 5f, TestName = "Distance_Diagonal_IsCornerToCorner")]
        public void Distance_BetweenRectangles(float xMin, float zMin, float xMax, float zMax, float expected)
        {
            Rect a = Rect.MinMaxRect(0f, 0f, 10f, 10f);

            Assert.That(BuildingNeighbors.Distance(a, Rect.MinMaxRect(xMin, zMin, xMax, zMax)),
                Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Compute_GapOf19_AreNeighborsBothWays()
        {
            // Centers 29 m apart, but the walls only 19 m.
            List<List<int>> neighbors = Compute(
                new[] { Rect.MinMaxRect(0f, 0f, 10f, 10f) },
                new[] { Rect.MinMaxRect(29f, 0f, 39f, 10f) });

            Assert.That(neighbors[0], Is.EqualTo(new[] { 1 }));
            Assert.That(neighbors[1], Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Compute_GapOf21_AreNotNeighbors()
        {
            List<List<int>> neighbors = Compute(
                new[] { Rect.MinMaxRect(0f, 0f, 10f, 10f) },
                new[] { Rect.MinMaxRect(31f, 0f, 41f, 10f) });

            Assert.That(neighbors[0], Is.Empty);
            Assert.That(neighbors[1], Is.Empty);
        }

        [Test]
        public void Compute_LShapedBuilding_ItsFarWingCounts()
        {
            // The front wing is 25 m away; the back wing reaches to 15 m.
            List<List<int>> neighbors = Compute(
                new[] { Rect.MinMaxRect(0f, 0f, 10f, 10f) },
                new[] { Rect.MinMaxRect(35f, 0f, 45f, 10f), Rect.MinMaxRect(25f, 10f, 35f, 20f) });

            Assert.That(neighbors[0], Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Compute_NeverListsItself()
        {
            List<List<int>> neighbors = Compute(
                new[] { Rect.MinMaxRect(0f, 0f, 10f, 10f) },
                new[] { Rect.MinMaxRect(12f, 0f, 22f, 10f) },
                new[] { Rect.MinMaxRect(24f, 0f, 34f, 10f) });

            Assert.That(neighbors[1], Is.EqualTo(new[] { 0, 2 }));
        }
    }
}
