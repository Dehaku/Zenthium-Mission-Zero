namespace Zenthium.Data
{
    // Unity serializes enums as numbers, so add new values at the end of each list.
    // The importer matches CSV words to these names ignoring case, spaces and punctuation
    // ("Gear (active)" is GearActive).

    public enum Stat
    {
        Strength,
        Will,
        Intelligence,
        Finesse,
        Toughness,
    }

    public enum ModuleCategory
    {
        BaseAction,
        Active,
        Passive,
        GearActive,
        GearPassive,
    }

    public enum ResourceType
    {
        Stamina,
        Mana,
        Charge,
        Ammo,
    }

    public enum RankTier
    {
        Goon,
        Champion,
        Leader,
    }

    public enum Rank
    {
        D,
        C,
        B,
        A,
        S,
    }

    public enum PropSize
    {
        Small,
        Medium,
        Large,
    }

    public enum ZennyFamily
    {
        Basic,
        Gem,
        Metallic,
    }

    public enum MissionType
    {
        Rescue,
        Countdown,
        Invasion,
        ArsonRiot,
    }

    public enum TierScale
    {
        Damage,
        Duration,
        Radius,
        Knockback,
        Cooldown,
        Range,
        Utility,
    }

    public enum DamageType
    {
        Physical,
        Magic,
        Energy,
    }

    public enum DataValueKind
    {
        Number,
        Range,
        Tier,
        Module,
    }

    /// <summary>The three slot types of heroes.md (Slots). Base actions are outside the slots.</summary>
    public enum SlotType
    {
        Active,
        Passive,
        Gear,
    }
}
