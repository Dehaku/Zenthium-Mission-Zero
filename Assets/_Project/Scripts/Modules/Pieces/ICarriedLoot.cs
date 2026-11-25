namespace Zenthium.Modules
{
    /// <summary>
    /// The Valuables an entity carries, which Kick can launch instead of kicking (world.md, Carrying). The World
    /// assembly implements it.
    /// </summary>
    public interface ICarriedLoot
    {
        /// <summary>Launches every carried Valuable along the throw, each dealing the throw's Kick damage.</summary>
        /// <returns>False if there's nothing to launch, or this carrier can't launch (only the player can).</returns>
        bool TryLaunchAll(in ThrowInfo info);
    }
}
