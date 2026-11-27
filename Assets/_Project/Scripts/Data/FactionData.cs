using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// One faction name from the <c>faction</c> column of entities.csv, including the "Summons" and "Neutral" groups,
    /// with its rules from factions.csv. A faction with no factions.csv row has no tags, arrival or weights.
    /// </summary>
    public sealed class FactionData : DataEntry
    {
        /// <summary>A mission type or location the weights don't list (factions.md, Weights).</summary>
        public const float UnlistedWeight = 1f;

        [SerializeField] private List<EntityTag> tags = new List<EntityTag>();
        [SerializeField] private bool hasArrival;
        [SerializeField] private ArrivalTag arrival;
        [SerializeField] private DataValueList missionWeights = new DataValueList();
        [SerializeField] private DataValueList locationWeights = new DataValueList();
        [SerializeField] private DataValueList values = new DataValueList();

        /// <summary>Tags every entity in the faction has (factions.md, Shared rules).</summary>
        public IReadOnlyList<EntityTag> Tags => tags;

        /// <summary>Mission type keys (<c>rescue</c>, <c>arson_riot</c>) to relative weights; Milestone 9 picks by them.</summary>
        public DataValueList MissionWeights => missionWeights;

        /// <summary>Location keys (<c>bank</c>, <c>park</c>) to relative weights.</summary>
        public DataValueList LocationWeights => locationWeights;

        /// <summary>Faction-only numbers, such as the Repo Agency's crop circles.</summary>
        public DataValueList Values => values;

        public bool TryGetArrival(out ArrivalTag tag)
        {
            tag = arrival;
            return hasArrival;
        }

        internal void Populate(List<EntityTag> newTags, ArrivalTag? newArrival, DataValueList newMissionWeights,
            DataValueList newLocationWeights, DataValueList newValues)
        {
            tags = newTags;
            hasArrival = newArrival.HasValue;
            arrival = newArrival.GetValueOrDefault();
            missionWeights = newMissionWeights;
            locationWeights = newLocationWeights;
            values = newValues;
        }
    }
}
