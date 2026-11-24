using System;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Spell tomes' <c>magic_hit_damage</c> (Force Spell Tome): flat damage added to the user's Magic hits and to
    /// every hit by the user's summons, whatever its type. It's added into the same hit.
    /// </summary>
    public static class SpellDamageBonus
    {
        public const string MagicHitDamageKey = "magic_hit_damage";

        public static float For(Entity attacker, DamageType type)
        {
            if (attacker == null)
            {
                return 0f;
            }

            bool isSummon = attacker.Summoner != null;
            if (!isSummon && type != DamageType.Magic)
            {
                return 0f;
            }

            return Sum((isSummon ? attacker.Summoner : attacker).Slots);
        }

        /// <summary>The total of the equipped modules' <c>magic_hit_damage</c>.</summary>
        public static float Sum(ModuleSlots slots)
        {
            float total = 0f;
            foreach (SlotType type in (SlotType[])Enum.GetValues(typeof(SlotType)))
            {
                for (int index = 0; index < slots.SlotsPerType; index++)
                {
                    if (slots.IsEquipped(type, index)
                        && slots.Get(type, index).Values.TryGetNumber(MagicHitDamageKey, out float bonus))
                    {
                        total += bonus;
                    }
                }
            }

            return total;
        }
    }
}
