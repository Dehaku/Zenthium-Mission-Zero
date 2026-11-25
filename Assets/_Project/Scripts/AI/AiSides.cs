using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.AI
{
    /// <summary>
    /// Who the AI fights and notices, by team (enemies.md, AI: Targets). Not the friendly fire rules, which decide who
    /// a hit can hurt.
    /// </summary>
    public static class AiSides
    {
        /// <summary>The team it fights for: a Hypnotized entity fights on its hypnotizer's side (statuses.md).</summary>
        public static Team EffectiveTeam(Entity self)
        {
            if (self.Statuses.TryGetSource(StatusType.Hypnotized, out Entity hypnotizer) && hypnotizer.Team != self.Team)
            {
                return hypnotizer.Team == Team.Player ? Team.Ally : hypnotizer.Team;
            }

            return self.Team;
        }

        /// <summary>
        /// Enemies fight anyone not on their side, civilians and neutrals included. The player's side fights enemies
        /// and neutrals. Neutrals fight anything not neutral, and those that spare civilians (Heavy Turrets) leave
        /// civilians alone. Civilians fight no one.
        /// </summary>
        public static bool IsHostile(Team self, Team other, bool sparesCivilians)
        {
            switch (self)
            {
                case Team.Enemy:
                    return other != Team.Enemy;
                case Team.Player:
                case Team.Ally:
                    return other == Team.Enemy || other == Team.Neutral;
                case Team.Neutral:
                    return other != Team.Neutral && !(other == Team.Civilian && sparesCivilians);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whose sight fills the awareness bar: for enemies, the player and the player's allies only, so they
        /// attack civilians and neutrals only once in combat; for everyone else, anyone it fights.
        /// </summary>
        public static bool Notices(Team self, Team other, bool sparesCivilians)
        {
            return self == Team.Enemy
                ? other == Team.Player || other == Team.Ally
                : IsHostile(self, other, sparesCivilians);
        }

        /// <summary>
        /// Lower is preferred. Enemies prefer the player and allies, then civilians, and fight neutrals only when
        /// nothing better is in view. Everyone else has no preference.
        /// </summary>
        public static int Preference(Team self, Team other)
        {
            if (self != Team.Enemy)
            {
                return 0;
            }

            switch (other)
            {
                case Team.Player:
                case Team.Ally:
                    return 0;
                case Team.Civilian:
                    return 1;
                default:
                    return 2;
            }
        }
    }
}
