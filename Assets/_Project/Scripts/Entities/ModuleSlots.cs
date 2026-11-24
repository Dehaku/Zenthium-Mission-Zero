using System;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>Why <see cref="ModuleSlots.Add"/> refused a module.</summary>
    public enum SlotAddResult
    {
        Added,

        /// <summary>A base action, which is always available outside the slots.</summary>
        NotSlottable,

        /// <summary>Every slot of its type is already filled.</summary>
        NoFreeSlot,

        /// <summary>A hero can't equip two copies of the same module (heroes.md, Slots).</summary>
        Duplicate,
    }

    /// <summary>
    /// An entity's active, passive and gear slots (heroes.md, Slots). A slot is locked or unlocked; a module in a
    /// locked slot is part of the kit but not equipped. Disable statuses (statuses.md) switch single slots off;
    /// modules with <see cref="ModuleTag.CannotBeDisabled"/> are immune.
    /// </summary>
    public sealed class ModuleSlots
    {
        private static readonly int TypeCount = Enum.GetValues(typeof(SlotType)).Length;

        private readonly ModuleData[] modules;
        private readonly bool[] unlocked;
        private readonly bool[] disabled;
        private readonly int[] candidates;

        public ModuleSlots(int slotsPerType)
        {
            if (slotsPerType < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotsPerType), slotsPerType, "Can't be negative.");
            }

            SlotsPerType = slotsPerType;
            modules = new ModuleData[TypeCount * slotsPerType];
            unlocked = new bool[modules.Length];
            disabled = new bool[modules.Length];
            candidates = new int[slotsPerType];
        }

        /// <summary>Raised when a slot is disabled or enabled again.</summary>
        public event Action OnSlotsChanged;

        public int SlotsPerType { get; }

        /// <summary>The module in the slot, or null if it's empty.</summary>
        public ModuleData Get(SlotType type, int index)
        {
            return modules[Index(type, index)];
        }

        public bool IsUnlocked(SlotType type, int index)
        {
            return unlocked[Index(type, index)];
        }

        public bool IsDisabled(SlotType type, int index)
        {
            return disabled[Index(type, index)];
        }

        /// <summary>Holds a module in an unlocked slot. Disabled slots still count as equipped.</summary>
        public bool IsEquipped(SlotType type, int index)
        {
            int i = Index(type, index);
            return modules[i] != null && unlocked[i];
        }

        /// <summary>Equipped and not disabled, so it works right now.</summary>
        public bool IsUsable(SlotType type, int index)
        {
            return IsEquipped(type, index) && !IsDisabled(type, index);
        }

        /// <summary>Puts the module in the first empty slot of its type.</summary>
        public SlotAddResult Add(ModuleData module, bool slotUnlocked)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (!module.TryGetSlotType(out SlotType type))
            {
                return SlotAddResult.NotSlottable;
            }

            if (Contains(module))
            {
                return SlotAddResult.Duplicate;
            }

            for (int index = 0; index < SlotsPerType; index++)
            {
                int i = Index(type, index);
                if (modules[i] == null)
                {
                    modules[i] = module;
                    unlocked[i] = slotUnlocked;
                    return SlotAddResult.Added;
                }
            }

            return SlotAddResult.NoFreeSlot;
        }

        public bool Contains(ModuleData module)
        {
            return Array.IndexOf(modules, module) >= 0;
        }

        /// <summary>Whether any equipped module has the tag. Disabled slots keep their tags.</summary>
        public bool HasTag(ModuleTag tag)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null && unlocked[i] && modules[i].HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Disables a random equipped slot of the type that isn't disabled yet, skipping modules that can't be
        /// disabled, so each hit of an Independent disable takes another slot.
        /// </summary>
        /// <returns>The slot index, or -1 if none could be disabled.</returns>
        internal int DisableRandom(SlotType type, IRandomSource random)
        {
            int count = 0;
            for (int index = 0; index < SlotsPerType; index++)
            {
                int i = Index(type, index);
                if (IsEquipped(type, index) && !disabled[i] && !modules[i].HasTag(ModuleTag.CannotBeDisabled))
                {
                    candidates[count++] = index;
                }
            }

            if (count == 0)
            {
                return -1;
            }

            // Value01 is below 1, so the pick stays in range; the clamp guards a source that returns 1.
            int pick = candidates[Math.Min(count - 1, (int)(random.Value01() * count))];
            disabled[Index(type, pick)] = true;
            OnSlotsChanged?.Invoke();
            return pick;
        }

        internal void Enable(SlotType type, int index)
        {
            int i = Index(type, index);
            if (disabled[i])
            {
                disabled[i] = false;
                OnSlotsChanged?.Invoke();
            }
        }

        private int Index(SlotType type, int index)
        {
            if (index < 0 || index >= SlotsPerType)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Slots go from 0 to {SlotsPerType - 1}.");
            }

            return (int)type * SlotsPerType + index;
        }
    }
}
