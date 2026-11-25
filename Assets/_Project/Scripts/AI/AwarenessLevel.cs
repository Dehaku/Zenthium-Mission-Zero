namespace Zenthium.AI
{
    /// <summary>How far along an entity is in noticing someone (enemies.md, Becoming aware).</summary>
    public enum AwarenessLevel
    {
        /// <summary>The bar fills while it sees someone it notices.</summary>
        Unaware,

        /// <summary>The "!" is up: it waits before acting, and its alert goes out when the wait ends.</summary>
        Noticing,

        Aware,
    }

    /// <summary>What one <see cref="Awareness.Tick"/> changed.</summary>
    public enum AwarenessChange
    {
        None,

        /// <summary>The bar filled: the "!" and the wait start.</summary>
        Noticed,

        /// <summary>The wait ended: allies in the alert radius become aware, and it acts.</summary>
        AlertSent,

        /// <summary>The bar drained after searching: unaware again.</summary>
        Forgotten,
    }
}
