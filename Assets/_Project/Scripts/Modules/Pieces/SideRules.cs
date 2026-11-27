using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Who a support module reaches on its user's side: Notarized's allies and Audit's alerted enemies are entities on
    /// the user's team, not the user itself (the user's decisions, Milestone 7b), standing and still on the map.
    /// </summary>
    public static class SideRules
    {
        public static bool IsOnUsersSide(Entity user, Entity other)
        {
            return user != null && other != null && other != user && other.Team == user.Team
                && !other.IsIncapacitated && !other.IsCaptured && !other.IsEscaped && !other.IsAbducted;
        }
    }
}
