namespace Zenthium.AI
{
    /// <summary>
    /// What the AI is doing (enemies.md, AI: States). Looting and Objective come with Milestone 6b.
    /// </summary>
    public enum AiState
    {
        /// <summary>Patrolling with its squad at reduced speed, or, for an ally, following the player.</summary>
        Idle,

        /// <summary>Aware, with a target in view: attacking it.</summary>
        Combat,

        /// <summary>Aware, with no target in view: searching around the last known position, then forgetting.</summary>
        Searching,
    }
}
