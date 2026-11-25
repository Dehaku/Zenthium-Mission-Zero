namespace Zenthium.Entities
{
    /// <summary>
    /// Tags on entities rather than modules (modules.md, factions.md). Systems check for a tag,
    /// never for a specific entity. Parameterized tags such as <c>LootDestination:&lt;place&gt;</c> come with their systems.
    /// </summary>
    public enum EntityTag
    {
        Hackable,
        Coward,

        /// <summary>Never moves: no walking, knockback or being dragged, such as a Heavy Turret (world.md).</summary>
        Immobile,

        /// <summary>Never targets or notices civilians, such as a Heavy Turret (enemies.md, world.md).</summary>
        SparesCivilians,
    }
}
