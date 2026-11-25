using System.Collections.Generic;
using Zenthium.Entities;

namespace Zenthium.AI
{
    /// <summary>An entity the AI can see now, and how far away it is.</summary>
    public readonly struct TargetCandidate
    {
        public TargetCandidate(Entity entity, float distance)
        {
            Entity = entity;
            Distance = distance;
        }

        public Entity Entity { get; }

        /// <summary>Meters on the XZ plane.</summary>
        public float Distance { get; }
    }

    /// <summary>Which entity the AI attacks (enemies.md, AI: Targets).</summary>
    public static class TargetSelector
    {
        /// <summary>
        /// The taunter, if Taunted. Otherwise whoever last hit it, if it's still in view and on another side.
        /// Otherwise the nearest one it fights, by <see cref="AiSides.Preference"/> first. Never one that's
        /// Incapacitated, captured or being dragged (dragged targets can't be hurt).
        /// </summary>
        /// <param name="self">Its team, after Hypnotized (<see cref="AiSides.EffectiveTeam"/>).</param>
        /// <param name="taunter">Who Taunted it; null if it isn't Taunted.</param>
        /// <param name="lastAttacker">Who last hit it; null if no one, or not on another side.</param>
        /// <param name="visible">Those it can see now.</param>
        /// <returns>Null if there's no one to attack.</returns>
        public static Entity Choose(Team self, bool sparesCivilians, Entity taunter, Entity lastAttacker,
            IReadOnlyList<TargetCandidate> visible)
        {
            if (IsTargetable(taunter))
            {
                return taunter;
            }

            Entity best = null;
            int bestPreference = int.MaxValue;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < visible.Count; i++)
            {
                Entity candidate = visible[i].Entity;
                if (!IsTargetable(candidate) || !AiSides.IsHostile(self, candidate.Team, sparesCivilians))
                {
                    continue;
                }

                if (candidate == lastAttacker)
                {
                    return candidate;
                }

                int preference = AiSides.Preference(self, candidate.Team);
                if (preference < bestPreference
                    || (preference == bestPreference && visible[i].Distance < bestDistance))
                {
                    best = candidate;
                    bestPreference = preference;
                    bestDistance = visible[i].Distance;
                }
            }

            return best;
        }

        private static bool IsTargetable(Entity entity)
        {
            return entity != null && !entity.IsIncapacitated && !entity.IsCaptured && !entity.IsBeingDragged;
        }
    }
}
