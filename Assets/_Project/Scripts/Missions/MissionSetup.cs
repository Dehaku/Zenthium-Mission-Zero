using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>
    /// One mission's rolled setup, handed from HQ (or the debug launcher) to the Mission scene (CLAUDE.md, Scenes and
    /// flow). Milestone 9c adds the secondary goal; hazards come with mission modifiers, from rank C.
    /// </summary>
    public sealed class MissionSetup
    {
        /// <param name="difficultyLevel">Mission ranks above D: 0 at D, 4 at S, 5 at S2.</param>
        /// <param name="seed">Builds the same map and the same spawns again.</param>
        public MissionSetup(MissionType type, FactionData faction, LocationType location, int difficultyLevel,
            MapSize size, bool hasLeader, int seed)
        {
            Type = type;
            Faction = faction;
            Location = location;
            DifficultyLevel = difficultyLevel;
            Size = size;
            HasLeader = hasLeader;
            Seed = seed;
        }

        public MissionType Type { get; }
        public FactionData Faction { get; }

        /// <summary>The key building's location, which also picks the map's district.</summary>
        public LocationType Location { get; }

        public int DifficultyLevel { get; }
        public MapSize Size { get; }

        /// <summary>Whether the faction's Leader makes an entrance (difficulty.md, Leader-led missions).</summary>
        public bool HasLeader { get; }

        public int Seed { get; }

        public override string ToString()
        {
            string rank = DifficultyLevel <= (int)Rank.S ? ((Rank)DifficultyLevel).ToString()
                : "S" + (DifficultyLevel - (int)Rank.S + 1);
            string faction = Faction != null ? Faction.Id : "no faction";
            return $"{Type} at a {Location}, {faction}, rank {rank}, {Size} map"
                + (HasLeader ? ", Leader-led" : string.Empty) + $", seed {Seed}";
        }
    }
}
