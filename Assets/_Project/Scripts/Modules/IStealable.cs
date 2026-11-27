namespace Zenthium.Modules
{
    /// <summary>
    /// Something a loot destination takes off the map for good once it's towed there, such as a prop or a citizen
    /// a Tow Saucer brings to a crop circle or the map edge (the user's decision, Milestone 7b).
    /// </summary>
    public interface IStealable
    {
        void Steal();
    }
}
