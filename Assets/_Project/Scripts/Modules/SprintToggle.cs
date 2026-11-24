using System;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Sprint (modules.md): a toggle that adds a percentage of the base move speed. No resource cost.
    /// Only player heroes sprint.
    /// </summary>
    public sealed class SprintToggle
    {
        // Key of Sprint's values column in modules.csv.
        public const string SpeedBonusKey = "speed_bonus";

        // Key of a module that gives a bigger Sprint bonus, such as Sprinter.
        public const string SprinterBonusKey = "sprint_bonus";

        private readonly MoveSpeed moveSpeed;
        private readonly float bonus;
        private ModifierHandle handle;

        /// <param name="bonus">The bonus as a fraction of the base speed: 0.5 is +50%.</param>
        public SprintToggle(MoveSpeed moveSpeed, float bonus)
        {
            this.moveSpeed = moveSpeed ?? throw new ArgumentNullException(nameof(moveSpeed));
            this.bonus = bonus;
        }

        public bool IsOn { get; private set; }

        /// <returns>False if Sprint's data has no speed bonus.</returns>
        public static bool TryGetBonus(ModuleData sprint, out float bonus)
        {
            bonus = 0f;
            return sprint != null && sprint.Values.TryGetNumber(SpeedBonusKey, out bonus);
        }

        /// <summary>
        /// Sprint's bonus for an entity: the largest of Sprint's own and any equipped module's <c>sprint_bonus</c>,
        /// which replaces it rather than adding (Sprinter: "a bigger Sprint bonus").
        /// </summary>
        public static float BonusFor(float sprintBonus, ModuleSlots slots)
        {
            float bonus = sprintBonus;
            foreach (SlotType type in (SlotType[])Enum.GetValues(typeof(SlotType)))
            {
                for (int index = 0; index < slots.SlotsPerType; index++)
                {
                    if (slots.IsEquipped(type, index)
                        && slots.Get(type, index).Values.TryGetNumber(SprinterBonusKey, out float moduleBonus))
                    {
                        bonus = Math.Max(bonus, moduleBonus);
                    }
                }
            }

            return bonus;
        }

        public void Toggle()
        {
            SetOn(!IsOn);
        }

        public void SetOn(bool on)
        {
            if (on == IsOn)
            {
                return;
            }

            IsOn = on;
            if (on)
            {
                handle = moveSpeed.AddBonus(bonus);
            }
            else
            {
                moveSpeed.RemoveBonus(handle);
            }
        }
    }
}
