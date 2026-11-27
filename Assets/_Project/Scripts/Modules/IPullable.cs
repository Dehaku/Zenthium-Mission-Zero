using UnityEngine;

namespace Zenthium.Modules
{
    /// <summary>
    /// Something a pull moves that isn't an entity, such as a prop caught in Tractor Beam. A pull only moves it: it
    /// deals no damage where it stops and doesn't break (the user's decision, Milestone 7b).
    /// </summary>
    public interface IPullable
    {
        /// <summary>Meters along the direction; an obstacle stops it sooner.</summary>
        void Pull(Vector3 direction, float distance);
    }
}
