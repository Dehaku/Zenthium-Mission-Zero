using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Hits every valid entity within the module's radius tier around the user, such as Megaphone's stun and
    /// Shockwave Stomp. Each target is pushed away from the user.
    /// </summary>
    public sealed class AreaStrikeModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly HitEffects effects;

        public AreaStrikeModule(ModuleData data, HitEffects effects) : base(data)
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
            Context.World.CollectEntities(origin, Data.RadiusTier.Value, reached);
            foreach (EntityBehaviour target in reached)
            {
                if (target != Context.OwnerBehaviour && !target.Entity.IsBeingDragged)
                {
                    effects.ApplyTo(Context, target, MeleeStrikeModule.Away(origin, target, aim.Direction));
                }
            }
        }
    }
}
