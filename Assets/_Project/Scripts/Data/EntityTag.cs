namespace Zenthium.Data
{
    /// <summary>
    /// Tags on entities rather than modules (modules.md, factions.md). Systems check for a tag,
    /// never for a specific entity. A parameterized tag such as <c>LootDestination:&lt;place&gt;</c> is one value per place.
    /// New values go at the end: scenes and prefabs store tags by index.
    /// </summary>
    public enum EntityTag
    {
        Hackable,
        Coward,

        /// <summary>Never moves: no walking, knockback or being dragged, such as a Heavy Turret (world.md).</summary>
        Immobile,

        /// <summary>Never targets or notices civilians, such as a Heavy Turret (enemies.md, world.md).</summary>
        SparesCivilians,

        /// <summary><c>LootDestination:MapEdge</c>: carries Valuables off the map, Leader or not (world.md, Goon behavior).</summary>
        LootDestinationMapEdge,

        /// <summary>
        /// <c>LootDestination:CropCircle</c>: takes Valuables and dragged citizens to the nearest Repo Agency crop
        /// circle (Factions/GalacticRepoAgency.md).
        /// </summary>
        LootDestinationCropCircle,
    }
}
