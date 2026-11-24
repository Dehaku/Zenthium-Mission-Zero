using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A melee hit on every valid entity and structure within the module's range tier and the melee arc around the
    /// aim, such as Speed Punch and Uppercut. Each entity is pushed away from the user. Dragged targets can't be
    /// hurt or moved, so it passes over them.
    /// </summary>
    public sealed class MeleeStrikeModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reachedEntities = new List<EntityBehaviour>();
        private readonly List<IDamageableStructure> reachedStructures = new List<IDamageableStructure>();
        private readonly HitEffects effects;

        public MeleeStrikeModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
        }

        /// <summary>The strike itself, shared with Kick.</summary>
        /// <param name="reachedEntities">Reused, so a strike allocates nothing.</param>
        /// <param name="reachedStructures">Reused, so a strike allocates nothing.</param>
        internal static void Strike(ModuleContext context, ModuleAim aim, HitEffects effects, float range,
            List<EntityBehaviour> reachedEntities, List<IDamageableStructure> reachedStructures)
        {
            Vector3 origin = context.Position;
            float halfAngle = context.MeleeArcHalfAngle;
            context.World.CollectEntities(origin, range, reachedEntities);
            foreach (EntityBehaviour target in reachedEntities)
            {
                if (target == context.OwnerBehaviour || target.Entity.IsBeingDragged
                    || !MeleeArc.Contains(origin, aim.Direction, target.transform.position, halfAngle))
                {
                    continue;
                }

                effects.ApplyTo(context, target, Away(origin, target, aim.Direction));
            }

            context.World.CollectStructures(origin, range, reachedStructures);
            foreach (IDamageableStructure structure in reachedStructures)
            {
                if (MeleeArc.Contains(origin, aim.Direction, structure.ClosestPoint(origin), halfAngle))
                {
                    effects.ApplyToStructure(context, structure);
                }
            }
        }

        internal static Vector3 Away(Vector3 origin, EntityBehaviour target, Vector3 fallback)
        {
            Vector3 away = target.transform.position - origin;
            away.y = 0f;
            return away.sqrMagnitude > 0f ? away : fallback;
        }

        protected override void Activate(ModuleAim aim)
        {
            Strike(Context, aim, effects, Data.RangeTier.Value, reachedEntities, reachedStructures);
        }
    }
}
