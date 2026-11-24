namespace Zenthium.Statuses
{
    /// <summary>
    /// The statuses of statuses.md's catalog. Its States live elsewhere: Incapacitated in <c>Health</c>, Dragged on
    /// <c>Entity.DraggedBy</c>, Invulnerable in <c>Invulnerability</c>; Defiance Roll is a counter on <c>Defiance</c>;
    /// Intangible comes with Phase Shift.
    /// </summary>
    public enum StatusType
    {
        Stun,
        Root,
        Taunted,
        Hypnotized,
        Engulfed,
        Marked,
        Burn,
        Infected,
        BrokenLimb,
        Gooed,
        CarryingTreasure,
        SlotDisable,
        Hex,
        Silenced,
        NumbingToxin,
        Pied,
        DeathlyRage,
        Spotlight,
        GoldPlatedChrome,
        Notarized,
        GelCoated,
        ReactivePlating,
        Overdrive,
        Mutate,
        SoulShard,
        Charmed,
    }

    /// <summary>The Kind column of statuses.md.</summary>
    public enum StatusKind
    {
        CrowdControl,
        Debuff,
        DamageOverTime,
        Slow,
        Disable,
        ScreenEffect,
        Buff,
        HiddenMarker,
    }

    /// <summary>What reapplying a status does (statuses.md, General rules).</summary>
    public enum StatusStacking
    {
        /// <summary>One instance; reapplying refreshes its timer (the default).</summary>
        Refresh,

        /// <summary>One instance; each application adds a layer and refreshes the timer.</summary>
        Intensity,

        /// <summary>Each application runs separately, with its own timer.</summary>
        Independent,

        /// <summary>Reapplying while it's active does nothing.</summary>
        NoStack,
    }
}
