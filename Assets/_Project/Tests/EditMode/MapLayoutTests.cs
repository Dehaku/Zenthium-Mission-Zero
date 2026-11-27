using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The street grid (maps.md, Generation and Size), with 68 m blocks and 12 m streets: blocks never overlap each
    /// other or the streets, every block faces a street, the map comes out near its size, and the key block holds the
    /// mission's location.
    /// </summary>
    public class MapLayoutTests
    {
        private const float Block = 68f;
        private const float Street = 12f;

        private static MapBlockOption Option(District district, params LocationType[] locations)
        {
            return new MapBlockOption(district, new List<LocationType>(locations));
        }

        private static List<MapBlockOption> Downtown()
        {
            return new List<MapBlockOption>
            {
                Option(District.Downtown, LocationType.Bank),
                Option(District.Downtown, LocationType.Store, LocationType.Club),
                Option(District.Downtown, LocationType.Museum),
            };
        }

        private static MapLayout Create(float size, LocationType key, List<MapBlockOption> options, IRandomSource random)
        {
            bool created = MapLayout.TryCreate(size, Block, Street, key, options, random, out MapLayout layout,
                out string problem);
            Assert.That(created, Is.True, problem);
            return layout;
        }

        [TestCase(150f, 2)]
        [TestCase(250f, 3)]
        [TestCase(400f, 5)]
        [TestCase(10f, 1)]
        public void BlocksPerSideFor_RoundsToTheNearestWholeBlock(float size, int expected)
        {
            Assert.That(MapLayout.BlocksPerSideFor(size, Block, Street), Is.EqualTo(expected));
        }

        [TestCase(150f, 172f, 4)]
        [TestCase(250f, 252f, 9)]
        [TestCase(400f, 412f, 25)]
        public void TryCreate_MapSizeMatches(float size, float expectedSide, int expectedBlocks)
        {
            MapLayout layout = Create(size, LocationType.Bank, Downtown(), new FixedRandom(0.5f));

            Assert.That(layout.Side, Is.EqualTo(expectedSide).Within(0.001f));
            Assert.That(layout.Blocks.Count, Is.EqualTo(expectedBlocks));
            Assert.That(layout.Area.width, Is.EqualTo(expectedSide).Within(0.001f));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TryCreate_NoBlockOverlapsAnotherOrAStreet(int seed)
        {
            MapLayout layout = Create(400f, LocationType.Bank, Downtown(), new SeededRandomSource(seed));

            for (int i = 0; i < layout.Blocks.Count; i++)
            {
                Rect block = layout.Blocks[i].Area;
                for (int j = i + 1; j < layout.Blocks.Count; j++)
                {
                    Assert.That(block.Overlaps(layout.Blocks[j].Area), Is.False, $"Blocks {i} and {j} overlap.");
                }

                foreach (Rect street in layout.Streets)
                {
                    Assert.That(block.Overlaps(street), Is.False, $"Block {i} overlaps the street at {street}.");
                }
            }
        }

        [TestCase(150f)]
        [TestCase(250f)]
        [TestCase(400f)]
        public void TryCreate_EveryBlockIsInsideTheMapAndFacesAStreet(float size)
        {
            MapLayout layout = Create(size, LocationType.Bank, Downtown(), new SeededRandomSource(4));

            foreach (BlockPlacement placement in layout.Blocks)
            {
                Rect block = placement.Area;
                Assert.That(layout.Area.Contains(block.min) && layout.Area.Contains(block.max), Is.True,
                    $"The block at {block} is outside the map.");

                // Grown by a hair, a block that only touches a street overlaps it.
                var grown = new Rect(block.x - 0.01f, block.y - 0.01f, block.width + 0.02f, block.height + 0.02f);
                bool facesStreet = false;
                foreach (Rect street in layout.Streets)
                {
                    facesStreet |= grown.Overlaps(street);
                }

                Assert.That(facesStreet, Is.True, $"The block at {block} can't be reached from a street.");
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TryCreate_KeyBlockHoldsTheLocation(int seed)
        {
            List<MapBlockOption> options = Downtown();

            MapLayout layout = Create(250f, LocationType.Museum, options, new SeededRandomSource(seed));

            Assert.That(layout.Blocks[layout.KeyBlock].Option, Is.EqualTo(2));
        }

        [Test]
        public void TryCreate_UsesOnlyTheLocationsDistrict()
        {
            var options = new List<MapBlockOption>
            {
                Option(District.Industrial, LocationType.Warehouse),
                Option(District.Residential, LocationType.House),
                Option(District.Industrial, LocationType.Lab),
                Option(District.Downtown, LocationType.Bank),
            };

            MapLayout layout = Create(400f, LocationType.Lab, options, new SeededRandomSource(5));

            foreach (BlockPlacement placement in layout.Blocks)
            {
                Assert.That(placement.Option, Is.EqualTo(0).Or.EqualTo(2));
            }
        }

        [Test]
        public void TryCreate_FillsWithEveryDistrictBlockBeforeRepeating()
        {
            // 4 cells: the Bank block as the key, then Store, Museum and one more.
            MapLayout layout = Create(150f, LocationType.Bank, Downtown(), new SeededRandomSource(6));

            var used = new HashSet<int>();
            foreach (BlockPlacement placement in layout.Blocks)
            {
                used.Add(placement.Option);
            }

            Assert.That(used, Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void TryCreate_UniqueBlockIsOnlyTheKey()
        {
            List<MapBlockOption> options = Downtown();
            options.Add(Option(District.Downtown, LocationType.SwiftTower));

            MapLayout withoutTower = Create(400f, LocationType.Bank, options, new SeededRandomSource(7));
            MapLayout withTower = Create(400f, LocationType.SwiftTower, options, new SeededRandomSource(7));

            int towersWithout = 0;
            int towersWith = 0;
            for (int i = 0; i < withoutTower.Blocks.Count; i++)
            {
                towersWithout += withoutTower.Blocks[i].Option == 3 ? 1 : 0;
                towersWith += withTower.Blocks[i].Option == 3 ? 1 : 0;
            }

            Assert.That(towersWithout, Is.EqualTo(0));
            Assert.That(towersWith, Is.EqualTo(1));
        }

        [Test]
        public void TryCreate_NoBlockForTheLocation_Fails()
        {
            bool created = MapLayout.TryCreate(250f, Block, Street, LocationType.Lab, Downtown(), new FixedRandom(0.5f),
                out MapLayout layout, out string problem);

            Assert.That(created, Is.False);
            Assert.That(layout, Is.Null);
            Assert.That(problem, Does.Contain("Lab"));
        }

        [Test]
        public void TryCreate_OnlyAUniqueBlockInTheDistrict_FailsToFill()
        {
            var options = new List<MapBlockOption> { Option(District.Downtown, LocationType.SwiftTower) };

            bool created = MapLayout.TryCreate(250f, Block, Street, LocationType.SwiftTower, options,
                new FixedRandom(0.5f), out _, out string problem);

            Assert.That(created, Is.False);
            Assert.That(problem, Does.Contain("Downtown"));
        }

        [Test]
        public void TryCreate_SameSeed_SameMap()
        {
            MapLayout first = Create(400f, LocationType.Bank, Downtown(), new SeededRandomSource(42));
            MapLayout second = Create(400f, LocationType.Bank, Downtown(), new SeededRandomSource(42));

            Assert.That(second.KeyBlock, Is.EqualTo(first.KeyBlock));
            for (int i = 0; i < first.Blocks.Count; i++)
            {
                Assert.That(second.Blocks[i].Option, Is.EqualTo(first.Blocks[i].Option));
                Assert.That(second.Blocks[i].QuarterTurns, Is.EqualTo(first.Blocks[i].QuarterTurns));
            }
        }
    }
}
