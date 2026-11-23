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
    }
}
