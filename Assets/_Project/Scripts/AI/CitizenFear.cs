namespace Zenthium.AI
{
    /// <summary>
    /// When a citizen flees (missions.md, Citizens): a Scared citizen panics while an enemy it can see is within its
    /// fear range. It drops its Valuables once, the first time it starts to flee (a decision for 6b; world.md).
    /// </summary>
    public sealed class CitizenFear
    {
        private readonly float fearRange;
        private bool hasDropped;

        /// <param name="fearRange">Meters.</param>
        public CitizenFear(float fearRange)
        {
            this.fearRange = fearRange;
        }

        public bool IsFleeing { get; private set; }

        /// <param name="nearestEnemyDistance">Meters to the nearest enemy it can see; positive infinity if none.</param>
        /// <param name="dropsValuables">True only the first time it starts to flee.</param>
        /// <returns>Whether it flees now.</returns>
        public bool Update(CitizenKind kind, float nearestEnemyDistance, out bool dropsValuables)
        {
            IsFleeing = kind == CitizenKind.Scared && nearestEnemyDistance <= fearRange;
            dropsValuables = IsFleeing && !hasDropped;
            hasDropped |= IsFleeing;
            return IsFleeing;
        }
    }
}
