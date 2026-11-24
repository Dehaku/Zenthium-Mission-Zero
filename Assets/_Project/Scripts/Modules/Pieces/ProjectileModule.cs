using System.Text;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Fires one projectile along the aim, flying as far as the module's range tier, such as Energy Blaster.
    /// </summary>
    public sealed class ProjectileModule : ModuleBehaviour
    {
        private readonly HitEffects effects;

        public ProjectileModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return aim.Direction.sqrMagnitude > 0f;
        }

        protected override void Activate(ModuleAim aim)
        {
            Projectile projectile = Context.World.SpawnProjectile(Context.Position, aim.Direction);
            if (projectile != null)
            {
                projectile.Launch(Context, effects, aim.Direction, Data.RangeTier.Value);
            }
        }
    }
}
