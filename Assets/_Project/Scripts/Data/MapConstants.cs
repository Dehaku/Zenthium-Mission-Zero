using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>constants.csv's <c>map</c> group (maps.md): map sizes, the street grid and exit zones.</summary>
    [Serializable]
    public sealed class MapConstants
    {
        [SerializeField] private float smallSize;
        [SerializeField] private float mediumSize;
        [SerializeField] private float largeSize;
        [SerializeField] private float blockSize;
        [SerializeField] private float streetWidth;
        [SerializeField] private float exitZoneSize;

        // Unity's serializer needs a parameterless constructor.
        public MapConstants()
        {
        }

        public MapConstants(
            float smallSize,
            float mediumSize,
            float largeSize,
            float blockSize,
            float streetWidth,
            float exitZoneSize)
        {
            this.smallSize = smallSize;
            this.mediumSize = mediumSize;
            this.largeSize = largeSize;
            this.blockSize = blockSize;
            this.streetWidth = streetWidth;
            this.exitZoneSize = exitZoneSize;
        }

        /// <summary>Meters across one square city block, not counting its streets.</summary>
        public float BlockSize => blockSize;

        /// <summary>Meters across the streets between and around blocks.</summary>
        public float StreetWidth => streetWidth;

        /// <summary>Meters across a square exit zone.</summary>
        public float ExitZoneSize => exitZoneSize;

        /// <summary>Meters across a map of this size, about: the street grid rounds it to whole blocks.</summary>
        public float Size(MapSize size)
        {
            switch (size)
            {
                case MapSize.Small:
                    return smallSize;
                case MapSize.Medium:
                    return mediumSize;
                default:
                    return largeSize;
            }
        }
    }
}
