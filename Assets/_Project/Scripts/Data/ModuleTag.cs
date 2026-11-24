namespace Zenthium.Data
{
    /// <summary>
    /// The module tags of modules.md that take no parameter, from modules.csv's <c>tags</c> column. Rules check a
    /// tag, never a specific module. <c>Resist:&lt;Stat&gt;</c> and <c>NoRegen:&lt;Resource&gt;</c> are read
    /// separately, as <see cref="ModuleData.ResistStats"/> and <see cref="ModuleData.NoRegenResources"/>.
    /// Unity serializes enums as numbers, so add new values at the end.
    /// </summary>
    public enum ModuleTag
    {
        Melee,
        Ranged,
        Time,
        Buff,
        Debuff,
        Summon,
        KnockbackResistant,
        CannotBeDisabled,
        Telegraph,
        Ignites,
        Extinguish,
        NotRandom,

        /// <summary>Its hits deal extra damage to walls, doors and windows (Breaching Charge; a local design decision).</summary>
        Demolition,
    }
}
