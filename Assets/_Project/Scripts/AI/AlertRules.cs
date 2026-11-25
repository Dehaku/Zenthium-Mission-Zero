using Zenthium.Entities;

namespace Zenthium.AI
{
    /// <summary>Who an alert reaches (enemies.md, Becoming aware: Alert).</summary>
    public static class AlertRules
    {
        /// <summary>
        /// An alert wakes allies on the alerter's team within the radius that are on their feet and still unaware.
        /// Each one woken starts its own wait and alert, so alerts chain.
        /// </summary>
        /// <param name="distance">Meters between the two, on the XZ plane.</param>
        public static bool Wakes(Entity alerter, Entity candidate, float distance, float radius,
            AwarenessLevel candidateLevel)
        {
            return alerter != null && candidate != null && candidate != alerter
                && candidate.Team == alerter.Team && candidate.Team != Team.Civilian
                && !candidate.IsIncapacitated && distance <= radius
                && candidateLevel == AwarenessLevel.Unaware;
        }
    }
}
