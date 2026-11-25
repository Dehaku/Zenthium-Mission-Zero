namespace Zenthium.Modules
{
    /// <summary>
    /// An entity whose awareness bar a module can empty, such as Smoke Bomb's cloud (modules.md). The AI implements
    /// it, so Modules never references the AI.
    /// </summary>
    public interface IAwarenessReset
    {
        /// <summary>Empties the bar of an entity that isn't aware yet; one already aware is unaffected.</summary>
        void ResetAwarenessBar();
    }
}
