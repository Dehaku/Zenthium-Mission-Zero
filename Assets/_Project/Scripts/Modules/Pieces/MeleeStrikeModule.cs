using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A melee hit on every valid entity within the module's range tier and the melee arc around the aim, such as
    /// Kick, Speed Punch and Uppercut. Each target is pushed away from the user. Dragged targets can't be hurt or
    /// moved, so it passes over them.
    /// </summary>
    public sealed class MeleeStrikeModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly HitEffects effects;

        public MeleeStrikeModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 origin = Context.Position;
            Context.World.CollectEntities(origin, Data.RangeTier.Value, reached);
            foreach (EntityBehaviour target in reached)
            {
                if (target == Context.OwnerBehaviour || target.Entity.IsBeingDragged
                    || !MeleeArc.Contains(origin, aim.Direction, target.transform.position, Context.MeleeArcHalfAngle))
                {
                    continue;
                }

                effects.ApplyTo(Context, target, Away(origin, target, aim.Direction));
            }
        }

        internal static Vector3 Away(Vector3 origin, EntityBehaviour target, Vector3 fallback)
        {
            Vector3 away = target.transform.position - origin;
            away.y = 0f;
            return away.sqrMagnitude > 0f ? away : fallback;
        }
    }
}
