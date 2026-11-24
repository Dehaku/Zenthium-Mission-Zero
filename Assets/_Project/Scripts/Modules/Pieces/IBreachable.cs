namespace Zenthium.Modules
{
    /// <summary>
    /// Something an explosion can blow open, such as a wall or a locked door (Breaching Charge, modules.md).
    /// Milestone 5's walls and doors implement it; nothing does yet.
    /// </summary>
    public interface IBreachable
    {
        void Breach();
    }
}
