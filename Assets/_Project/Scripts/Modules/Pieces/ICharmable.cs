using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// An entity Charming can win over. Milestone 6's goon AI implements it: the goon switches sides and hands the
    /// charmer its Valuables (modules.md, Charming). Nothing implements it yet.
    /// </summary>
    public interface ICharmable
    {
        void Convert(Entity charmer);
    }
}
