using Zenthium.Core;

namespace Zenthium.AI
{
    /// <summary>
    /// Cowardice (enemies.md, Other rules): an entity with the <c>Coward</c> tag rolls once, the first time its HP
    /// drops below a fraction of its max, and on a success panics and runs for the map edge until it escapes.
    /// </summary>
    public sealed class Cowardice
    {
        private readonly float hpFraction;
        private readonly float chance;

        /// <param name="hpFraction">It rolls once HP is below this fraction of max HP.</param>
        /// <param name="chance">The chance the roll makes it panic.</param>
        public Cowardice(float hpFraction, float chance)
        {
            this.hpFraction = hpFraction;
            this.chance = chance;
        }

        public bool HasRolled { get; private set; }

        public bool IsPanicking { get; private set; }

        /// <summary>Rolls if this is the first time its HP is below the threshold. Incapacitated, it doesn't roll.</summary>
        /// <returns>True if this check made it panic.</returns>
        public bool Check(float currentHp, float maxHp, IRandomSource random)
        {
            // The cast rounds the fraction to float. Mono can keep it at higher precision, where 20 / 100 is exactly
            // 0.2 but the float 0.2f is a hair above it, so exactly 20% HP would count as below the threshold.
            if (HasRolled || maxHp <= 0f || currentHp <= 0f || (float)(currentHp / maxHp) >= hpFraction)
            {
                return false;
            }

            HasRolled = true;
            IsPanicking = random.Value01() < chance;
            return IsPanicking;
        }
    }
}
