using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Kick (modules.md): a melee strike, except:
    /// <list type="bullet">
    /// <item>while dragging a prop, it throws the prop instead, as far as Kick's knockback tier (world.md, Props);
    /// one prop per Kick, oldest grab first;</item>
    /// <item>otherwise, while the player carries Valuables, it launches them all (world.md, Carrying).</item>
    /// </list>
    /// Kick's cooldown runs whichever it does.
    /// </summary>
    public sealed class KickModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reachedEntities = new List<EntityBehaviour>();
        private readonly List<IDamageableStructure> reachedStructures = new List<IDamageableStructure>();
        private readonly HitEffects effects;
        private DragAction drag;

        public KickModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        /// <summary>The thrower's Kick damage, which thrown props multiply. 0 if the data has no Kick damage.</summary>
        public static float KickDamage(Entity thrower, GameDatabase database)
        {
            return database.Modules.TryGet(DataIds.Modules.Kick, out ModuleData kick)
                && StatFormula.TryCreate(kick, out StatFormula formula)
                    ? formula.Damage(thrower, database.Formulas)
                    : 0f;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
            text.Append("While dragging a prop: throws it ").Append(Data.KnockbackTier.Value).Append(" m\n");
            text.Append("While carrying Valuables: launches them\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            if (aim.Direction.sqrMagnitude > 0f)
            {
                var info = new ThrowInfo(Owner, Context.Position, aim.Direction.normalized, Data.KnockbackTier.Value,
                    KickDamage(Owner, Context.World.Database));
                if (Context.OwnerBehaviour.TryGetComponent(out drag)
                    && drag.TryReleaseFirstThrowable(out IThrowable throwable))
                {
                    throwable.Throw(info);
                    return;
                }

                if (Context.OwnerBehaviour.TryGetComponent(out ICarriedLoot loot) && loot.TryLaunchAll(info))
                {
                    return;
                }
            }

            MeleeStrikeModule.Strike(Context, aim, effects, Data.RangeTier.Value, reachedEntities, reachedStructures);
        }
    }
}
