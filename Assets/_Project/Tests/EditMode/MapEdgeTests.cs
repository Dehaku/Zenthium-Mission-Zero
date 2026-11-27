using NUnit.Framework;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>Fleeing enemies leave from the nearest edge (missions.md: any edge), on a 60 m map centered on 0.</summary>
    public class MapEdgeTests
    {
        private static readonly Rect Map = new Rect(-30f, -30f, 60f, 60f);

        [TestCase(25f, 3f, 30f, 3f)]
        [TestCase(-26f, -4f, -30f, -4f)]
        [TestCase(5f, 28f, 5f, 30f)]
        [TestCase(-2f, -29f, -2f, -30f)]
        [TestCase(40f, 50f, 30f, 30f)]
        public void NearestPoint_IsOnTheClosestEdge(float x, float z, float expectedX, float expectedZ)
        {
            Vector2 point = MapEdge.NearestPoint(Map, new Vector2(x, z));

            Assert.That(point.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(expectedZ).Within(0.001f));
        }

        [TestCase(0f, 0f, 30f)]
        [TestCase(27.5f, 0f, 2.5f)]
        [TestCase(10f, -29f, 1f)]
        [TestCase(35f, 0f, 0f)]
        public void DistanceToEdge_IsToTheNearestEdge(float x, float z, float expected)
        {
            Assert.That(MapEdge.DistanceToEdge(Map, new Vector2(x, z)), Is.EqualTo(expected).Within(0.001f));
        }
    }
}
