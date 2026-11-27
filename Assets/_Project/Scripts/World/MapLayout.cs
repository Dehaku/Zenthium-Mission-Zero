using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>What the layout needs to know about one block: its district and the locations in it.</summary>
    public readonly struct MapBlockOption
    {
        public MapBlockOption(District district, IReadOnlyList<LocationType> locations)
        {
            District = district;
            Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        }

        public District District { get; }
        public IReadOnlyList<LocationType> Locations { get; }

        /// <summary>It holds a unique location, so it's only ever placed as the key block.</summary>
        public bool IsUnique
        {
            get
            {
                foreach (LocationType location in Locations)
                {
                    if (LocationDistricts.IsUnique(location))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool Has(LocationType location)
        {
            foreach (LocationType held in Locations)
            {
                if (held == location)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>One block on the street grid.</summary>
    public readonly struct BlockPlacement
    {
        public BlockPlacement(int option, Rect area, int quarterTurns)
        {
            Option = option;
            Area = area;
            QuarterTurns = quarterTurns;
        }

        /// <summary>Its index in the block options the layout was made from.</summary>
        public int Option { get; }

        /// <summary>Its ground on the XZ plane, relative to the map's center: x is X, y is Z.</summary>
        public Rect Area { get; }

        /// <summary>0 to 3 turns of 90° about Y. Blocks are square, so turning one never changes its area.</summary>
        public int QuarterTurns { get; }
    }

    /// <summary>
    /// A random Blazithion map as plain geometry (maps.md, Generation): a square grid of equal blocks with streets
    /// between and around them, so every block faces a street on all four sides. One block holds the mission's
    /// location (the key block); the rest are random blocks of the same district, each turned at random.
    /// Coordinates are on the XZ plane relative to the map's center.
    /// </summary>
    public sealed class MapLayout
    {
        private const int QuarterTurnsPerTurn = 4;

        private readonly List<BlockPlacement> blocks;
        private readonly List<Rect> streets;

        private MapLayout(float side, int blocksPerSide, List<BlockPlacement> blocks, int keyBlock, List<Rect> streets)
        {
            Side = side;
            BlocksPerSide = blocksPerSide;
            this.blocks = blocks;
            KeyBlock = keyBlock;
            this.streets = streets;
        }

        /// <summary>Meters across the whole map, streets included.</summary>
        public float Side { get; }

        public int BlocksPerSide { get; }

        /// <summary>Row by row from the map's -X, -Z corner.</summary>
        public IReadOnlyList<BlockPlacement> Blocks => blocks;

        /// <summary>The index in <see cref="Blocks"/> of the block holding the mission's location.</summary>
        public int KeyBlock { get; }

        /// <summary>
        /// The street strips: full-length ones along Z, then full-length ones along X. They cross at intersections.
        /// The first and last of each run along the map's edges.
        /// </summary>
        public IReadOnlyList<Rect> Streets => streets;

        /// <summary>The whole map, relative to its center.</summary>
        public Rect Area => new Rect(-Side * 0.5f, -Side * 0.5f, Side, Side);

        /// <summary>The whole number of blocks along a side that comes nearest the target size; at least 1.</summary>
        public static int BlocksPerSideFor(float targetSize, float blockSize, float streetWidth)
        {
            float cell = blockSize + streetWidth;
            return Math.Max(1, (int)Math.Round((targetSize - streetWidth) / cell, MidpointRounding.AwayFromZero));
        }

        /// <summary>Meters across a map with this many blocks along a side, its outer streets included.</summary>
        public static float SideFor(int blocksPerSide, float blockSize, float streetWidth)
        {
            return blocksPerSide * (blockSize + streetWidth) + streetWidth;
        }

        /// <param name="targetSize">Meters across the map, about (maps.md, Size).</param>
        /// <param name="keyLocation">The mission's location: one block must hold it, and its district picks the rest.</param>
        /// <param name="options">Every block there is to choose from.</param>
        /// <param name="problem">Why it couldn't, such as a district with no block for the location.</param>
        public static bool TryCreate(float targetSize, float blockSize, float streetWidth, LocationType keyLocation,
            IReadOnlyList<MapBlockOption> options, IRandomSource random, out MapLayout layout, out string problem)
        {
            layout = null;
            if (targetSize <= 0f || blockSize <= 0f || streetWidth < 0f)
            {
                problem = $"The map size ({targetSize}) and block size ({blockSize}) must be above 0, and the street "
                    + $"width ({streetWidth}) 0 or more.";
                return false;
            }

            District district = LocationDistricts.Of(keyLocation);
            var keyOptions = new List<int>();
            var fillOptions = new List<int>();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].District != district)
                {
                    continue;
                }

                if (options[i].Has(keyLocation))
                {
                    keyOptions.Add(i);
                }

                if (!options[i].IsUnique)
                {
                    fillOptions.Add(i);
                }
            }

            int blocksPerSide = BlocksPerSideFor(targetSize, blockSize, streetWidth);
            int cellCount = blocksPerSide * blocksPerSide;
            if (keyOptions.Count == 0)
            {
                problem = $"No {district} block holds a {keyLocation}.";
                return false;
            }

            if (cellCount > 1 && fillOptions.Count == 0)
            {
                problem = $"The {district} district has no blocks to fill the map with.";
                return false;
            }

            int keyOption = keyOptions[Index(random, keyOptions.Count)];
            int keyCell = Index(random, cellCount);
            float side = SideFor(blocksPerSide, blockSize, streetWidth);
            float cell = blockSize + streetWidth;
            float min = -side * 0.5f;

            // The first bag leaves out the key block when the district has others, so it repeats as late as it can.
            var bag = new List<int>(fillOptions);
            if (bag.Count > 1)
            {
                bag.Remove(keyOption);
            }

            var blocks = new List<BlockPlacement>(cellCount);
            for (int row = 0; row < blocksPerSide; row++)
            {
                for (int column = 0; column < blocksPerSide; column++)
                {
                    int index = row * blocksPerSide + column;
                    int option = index == keyCell ? keyOption : DrawFill(fillOptions, bag, random);
                    var area = new Rect(min + streetWidth + column * cell, min + streetWidth + row * cell, blockSize,
                        blockSize);
                    blocks.Add(new BlockPlacement(option, area, Index(random, QuarterTurnsPerTurn)));
                }
            }

            var streets = new List<Rect>(2 * (blocksPerSide + 1));
            for (int i = 0; i <= blocksPerSide; i++)
            {
                streets.Add(new Rect(min + i * cell, min, streetWidth, side));
            }

            for (int i = 0; i <= blocksPerSide; i++)
            {
                streets.Add(new Rect(min, min + i * cell, side, streetWidth));
            }

            layout = new MapLayout(side, blocksPerSide, blocks, keyCell, streets);
            problem = null;
            return true;
        }

        /// <summary>
        /// The next block from a shuffled bag of the district's blocks, refilled when empty, so blocks repeat as little
        /// as the district allows.
        /// </summary>
        private static int DrawFill(List<int> fillOptions, List<int> bag, IRandomSource random)
        {
            if (bag.Count == 0)
            {
                bag.AddRange(fillOptions);
            }

            int index = Index(random, bag.Count);
            int option = bag[index];
            bag.RemoveAt(index);
            return option;
        }

        private static int Index(IRandomSource random, int count)
        {
            return Math.Min((int)(random.Value01() * count), count - 1);
        }
    }
}
