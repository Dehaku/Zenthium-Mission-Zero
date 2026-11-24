using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Which entities a module treats as "enemies" when it targets by side, such as Taunt or Glyph Trap, rather than
    /// by who it may hurt (friendly fire). The player and allies on one side; enemies and neutrals on the other;
    /// civilians on neither.
    /// </summary>
    public static class Opponents
    {
        public static bool Are(Entity a, Entity b)
        {
            if (a == null || b == null || a == b || a.Team == Team.Civilian || b.Team == Team.Civilian)
            {
                return false;
            }

            return IsPlayerSide(a.Team) != IsPlayerSide(b.Team);
        }

        private static bool IsPlayerSide(Team team)
        {
            return team == Team.Player || team == Team.Ally;
        }
    }
}
