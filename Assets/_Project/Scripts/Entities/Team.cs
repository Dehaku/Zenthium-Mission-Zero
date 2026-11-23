namespace Zenthium.Entities
{
    /// <summary>
    /// Which side an entity is on. Friendly fire rules work from it.
    /// </summary>
    public enum Team
    {
        Player,
        Ally,
        Enemy,
        Neutral,
        Civilian,
    }
}
