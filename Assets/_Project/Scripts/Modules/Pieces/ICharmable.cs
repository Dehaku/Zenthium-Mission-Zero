using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>What Charming is trying to win over, which picks the chance it rolls (modules.md, Charming).</summary>
    public enum CharmTarget
    {
        /// <summary>An enemy goon: <c>goon_convert</c>, and it joins the charmer's side.</summary>
        Goon,

        /// <summary>A Stubborn citizen: <c>stubborn_to_scared</c>, and it turns Scared.</summary>
        StubbornCitizen,
    }

    /// <summary>
    /// An entity Charming can win over. The AI implements it: goons (<c>CharmConversion</c>) and citizens
    /// (<c>CitizenBrain</c>).
    /// </summary>
    public interface ICharmable
    {
        /// <returns>False if Charming can't win it over now.</returns>
        bool TryGetCharmTarget(out CharmTarget target);

        /// <summary>Charming's roll succeeded.</summary>
        void Charm(Entity charmer);
    }
}
