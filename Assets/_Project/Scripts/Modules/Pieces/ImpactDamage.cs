using System;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Damage when a knocked-back entity slams into a wall or prop (world.md, Walls): the wall or prop takes the
    /// structure tier and the entity the entity tier, both from the stat of whoever knocked it.
    /// </summary>
    public sealed class ImpactDamage
    {
        private readonly BalanceFormulas formulas;
        private readonly WorldConstants constants;

        public ImpactDamage(BalanceFormulas formulas, WorldConstants constants)
        {
            this.formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        }

        /// <summary>
        /// Who knocked the target back and their stat: the module's stat, or Strength for a module without one.
        /// </summary>
        /// <param name="statOwner">Whose stat counts, if not the source's (a summon using its summoner's).</param>
        public static KnockbackCause Cause(Entity source, ModuleData module, Entity statOwner = null)
        {
            if (source == null)
            {
                return default;
            }

            Stat stat = module != null && module.TryGetStat(out Stat moduleStat) ? moduleStat : Stat.Strength;
            return new KnockbackCause(source, (statOwner ?? source).Stats.Get(stat));
        }

        /// <summary>Before the structure's defenses and building multiplier.</summary>
        public float ToStructure(float statValue)
        {
            return formulas.TierDamage(statValue, constants.ImpactStructureDamageTier);
        }

        /// <summary>Before the entity's defenses.</summary>
        public float ToEntity(float statValue)
        {
            return formulas.TierDamage(statValue, constants.ImpactEntityDamageTier);
        }
    }
}
