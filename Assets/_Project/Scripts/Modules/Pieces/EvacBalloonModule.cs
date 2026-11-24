using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Evac Balloon (modules.md): attaches to the nearest Incapacitated citizen or enemy within its range and floats
    /// them away, counting as rescued or captured. With nothing in reach it isn't used, so the cooldown doesn't
    /// start. Standing citizens come with Milestone 6, and the mission counts with Milestone 9.
    /// </summary>
    public sealed class EvacBalloonModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private EntityBehaviour target;

        public EvacBalloonModule(ModuleData data) : base(data)
        {
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Floats away a downed citizen (rescued) or enemy (captured) in reach\n");
        }

        protected override bool CanActivateNow(Vector3 aimDirection)
        {
            target = FindTarget();
            return target != null;
        }

        protected override void Activate(Vector3 aimDirection)
        {
            ExtractionKind kind = target.Entity.Team == Team.Civilian ? ExtractionKind.Rescued : ExtractionKind.Captured;
            Context.World.Extract(target, kind);
            target = null;
        }

        private EntityBehaviour FindTarget()
        {
            Vector3 origin = Context.Position;
            Context.World.CollectEntities(origin, Data.RangeTier.Value, reached);
            EntityBehaviour nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (EntityBehaviour candidate in reached)
            {
                Entity entity = candidate.Entity;
                bool evacuable = entity.IsIncapacitated && (entity.Team == Team.Civilian || entity.Team == Team.Enemy);
                if (!evacuable || candidate == Context.OwnerBehaviour)
                {
                    continue;
                }

                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
