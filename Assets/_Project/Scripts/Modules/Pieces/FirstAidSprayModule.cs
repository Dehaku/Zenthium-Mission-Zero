using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// First Aid Spray (modules.md): heals the user for its <c>heal</c> fraction of max HP. Used next to an
    /// Incapacitated citizen, it gets them up instead, at the same fraction of their max HP, at no cost and with no
    /// cooldown. Getting up as Scared comes with citizens in Milestone 6.
    /// </summary>
    public sealed class FirstAidSprayModule : ModuleBehaviour
    {
        public const string HealKey = "heal";

        private const string PercentFormat = "0.#";

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly float healFraction;

        public FirstAidSprayModule(ModuleData data, float healFraction) : base(data)
        {
            this.healFraction = healFraction;
        }

        public override bool TryActivate(ModuleAim aim, bool slotUsable)
        {
            if (Context != null && IsEnabled && slotUsable && !Owner.IsIncapacitated && Owner.Statuses.CanAct)
            {
                Entity citizen = FindDownedCitizen();
                if (citizen != null)
                {
                    return citizen.Revive(citizen.Health.Max * healFraction);
                }
            }

            return base.TryActivate(aim, slotUsable);
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Heal ").Append((healFraction * 100f).ToString(PercentFormat, CultureInfo.InvariantCulture))
                .Append("% of max HP = ").Append(Mathf.Round(Owner.Health.Max * healFraction)).Append('\n')
                .Append("Next to a downed citizen: revives them, free, no cooldown\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            Owner.Heal(Owner.Health.Max * healFraction);
        }

        private Entity FindDownedCitizen()
        {
            Vector3 origin = Context.Position;
            Context.World.CollectEntities(origin, Data.RangeTier.Value, reached);
            Entity nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (EntityBehaviour candidate in reached)
            {
                Entity entity = candidate.Entity;
                if (entity.Team != Team.Civilian || !entity.IsIncapacitated)
                {
                    continue;
                }

                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = entity;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
