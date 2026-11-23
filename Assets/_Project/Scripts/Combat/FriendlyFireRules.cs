using Zenthium.Entities;

namespace Zenthium.Combat
{
    /// <summary>
    /// Who can hurt whom (combat.md, enemies.md). Environmental damage hurts everyone.
    /// <list type="bullet">
    /// <item>Player: enemies and neutrals; allies and civilians only while friendly fire is held; never itself.</item>
    /// <item>Ally: enemies and neutrals only.</item>
    /// <item>Enemy: everyone, other enemies included unless the dev toggle turns that off.</item>
    /// <item>Neutral: everyone.</item>
    /// <item>Civilian: no one. Civilians don't attack.</item>
    /// </list>
    /// </summary>
    public sealed class FriendlyFireRules
    {
        /// <summary>True while the player holds the friendly fire button.</summary>
        public bool IsPlayerFriendlyFireHeld { get; set; }

        /// <summary>Dev option: enemy attacks can hurt other enemies (enemies.md).</summary>
        public bool IsEnemyFriendlyFireEnabled { get; set; } = true;

        /// <param name="attacker">Null for environmental damage.</param>
        public bool CanHurt(Entity attacker, Entity target)
        {
            if (attacker == null)
            {
                return true;
            }

            switch (attacker.Team)
            {
                case Team.Player:
                    return CanPlayerHurt(target.Team);
                case Team.Ally:
                    return IsHostileToPlayer(target.Team);
                case Team.Enemy:
                    return target.Team != Team.Enemy || IsEnemyFriendlyFireEnabled;
                case Team.Neutral:
                    return true;
                default:
                    return false;
            }
        }

        private bool CanPlayerHurt(Team target)
        {
            if (IsHostileToPlayer(target))
            {
                return true;
            }

            return target != Team.Player && IsPlayerFriendlyFireHeld;
        }

        private static bool IsHostileToPlayer(Team target)
        {
            return target == Team.Enemy || target == Team.Neutral;
        }
    }
}
