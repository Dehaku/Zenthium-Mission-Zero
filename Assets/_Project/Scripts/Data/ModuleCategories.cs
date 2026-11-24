namespace Zenthium.Data
{
    /// <summary>
    /// Which slot type holds each module category (heroes.md, Slots).
    /// </summary>
    public static class ModuleCategories
    {
        /// <returns>False for base actions, which are always available outside the slots.</returns>
        public static bool TryGetSlotType(ModuleCategory category, out SlotType slotType)
        {
            switch (category)
            {
                case ModuleCategory.Active:
                    slotType = SlotType.Active;
                    return true;
                case ModuleCategory.Passive:
                    slotType = SlotType.Passive;
                    return true;
                case ModuleCategory.GearActive:
                case ModuleCategory.GearPassive:
                    slotType = SlotType.Gear;
                    return true;
                default:
                    slotType = default;
                    return false;
            }
        }

        /// <summary>Actives and gear actives are used with a button; passives and gear passives aren't.</summary>
        public static bool IsActivated(ModuleCategory category)
        {
            return category == ModuleCategory.BaseAction || category == ModuleCategory.Active
                || category == ModuleCategory.GearActive;
        }
    }
}
