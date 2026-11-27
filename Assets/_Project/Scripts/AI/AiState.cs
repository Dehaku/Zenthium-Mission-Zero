namespace Zenthium.AI
{
    /// <summary>
    /// What the AI is doing (enemies.md, AI: States).
    /// </summary>
    public enum AiState
    {
        /// <summary>Patrolling with its squad at reduced speed, or, for an ally, following the player.</summary>
        Idle,

        /// <summary>Aware, with a target in view: attacking it.</summary>
        Combat,

        /// <summary>Aware, with no target in view: searching around the last known position, then forgetting.</summary>
        Searching,

        /// <summary>Going for a Valuable it can see, and picking it up (world.md, Picking up).</summary>
        Looting,

        /// <summary>
        /// Taking its Valuables to its Leader or off the map, or a panicked coward running for the map edge
        /// (enemies.md, Other rules; world.md, Goon behavior).
        /// </summary>
        Objective,
    }
}
