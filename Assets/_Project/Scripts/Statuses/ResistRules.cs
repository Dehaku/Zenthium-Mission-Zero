using System;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Statuses
{
    /// <summary>
    /// Resisting debuffs (statuses.md, Resists): a successful resist ignores the debuff entirely.
    /// </summary>
    public static class ResistRules
    {
        /// <summary>Defender's resist stat / (defender's resist stat + attacker's stat). Equal stats give 0.5.</summary>
        public static float Chance(float defenderStat, float attackerStat)
        {
            float total = defenderStat + attackerStat;
            return total > 0f ? Math.Max(0f, defenderStat) / total : 0f;
        }

        /// <summary>The stat the module scales with; if it has none, the attacker's value of the resist stat.</summary>
        public static Stat AttackerStat(ModuleData module, Stat resistStat)
        {
            return module != null && module.TryGetStat(out Stat stat) ? stat : resistStat;
        }

        /// <param name="attacker">Null for the environment, which can't be resisted.</param>
        public static bool Resists(
            ModuleData module, Stat resistStat, Entity attacker, Entity defender, IRandomSource random)
        {
            if (attacker == null || defender == null)
            {
                return false;
            }

            float chance = Chance(
                defender.Stats.Get(resistStat), attacker.Stats.Get(AttackerStat(module, resistStat)));
            return chance > 0f && random.Value01() < chance;
        }
    }
}
