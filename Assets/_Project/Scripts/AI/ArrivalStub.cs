using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.AI
{
    /// <summary>
    /// Stub until Milestone 9: how a spawned entity shows up from its faction's arrival tag (enemies.md, Arrival
    /// tags), such as <c>Arrival:Beam</c>'s beaming down. For now every entity just appears where it's spawned.
    /// </summary>
    public static class ArrivalStub
    {
        public static void Arrive(EntityBehaviour spawned, ArrivalTag arrival)
        {
            // Intentionally empty until Milestone 9 plays the arrival.
        }
    }
}
