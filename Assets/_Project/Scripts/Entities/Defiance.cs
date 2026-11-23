using System;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// Defiance (stats.md): the chance, from Will, to survive at 1 HP. A chance of 100% or more is a guaranteed
    /// proc; each proc adds a Defiance Roll stack that removes 100% from the chance for the rest of the mission.
    /// Will itself is untouched.
    /// </summary>
    public sealed class Defiance
    {
        private readonly EntityStats stats;
        private readonly EntityConstants constants;
        private readonly bool isPlayer;

        internal Defiance(EntityStats stats, EntityConstants constants, bool isPlayer)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
            this.isPlayer = isPlayer;
        }

        /// <summary>Defiance Roll stacks: one per proc this mission.</summary>
        public int RollStacks { get; private set; }

        /// <summary>The proc chance after Defiance Roll stacks, as a fraction. Can exceed 1; never below 0.</summary>
        public float ProcChance =>
            Math.Max(0f, stats.Get(Stat.Will) * constants.DefianceChancePerWill - RollStacks * constants.DefianceRollCost);

        /// <summary>How long a proc makes the entity Invulnerable: longer for the player.</summary>
        public float InvulnerableSeconds =>
            isPlayer ? constants.DefianceInvulnerablePlayer : constants.DefianceInvulnerableOther;

        /// <summary>Rolls only when the chance is between 0 and 100%. A proc adds a Defiance Roll stack.</summary>
        internal bool TryProc(IRandomSource random)
        {
            float chance = ProcChance;
            bool procs = chance >= 1f || (chance > 0f && random.Value01() < chance);
            if (procs)
            {
                RollStacks++;
            }

            return procs;
        }
    }
}
