using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>What a mission put on its map, for its goals (Milestone 9b) and the start sequence (9c).</summary>
    public sealed class MissionMap
    {
        /// <summary>
        /// Where the goal is, on the ground: the Countdown target's socket, or the middle of the mission's location.
        /// </summary>
        public Vector3 ObjectivePoint { get; internal set; }

        /// <summary>Where the Countdown target goes; null for other mission types.</summary>
        public MapSocket CountdownSocket { get; internal set; }

        /// <summary>Rescue's target citizens, in the key building. Empty for other types.</summary>
        public List<EntityBehaviour> TargetCitizens { get; } = new List<EntityBehaviour>();

        /// <summary>Every citizen spawned, the targets included.</summary>
        public List<EntityBehaviour> Citizens { get; } = new List<EntityBehaviour>();

        /// <summary>Doors locked for the mission.</summary>
        public List<Door> LockedDoors { get; } = new List<Door>();

        /// <summary>Valuables placed in buildings and on civilians, for the Valuables secondary goal.</summary>
        public int ValuablesPlaced { get; internal set; }

        /// <summary>The threat budget the starting force was bought with.</summary>
        public float StartingBudget { get; internal set; }

        /// <summary>The threat budget left for Invasion's waves; 0 for other types.</summary>
        public float WaveBudget { get; internal set; }
    }
}
