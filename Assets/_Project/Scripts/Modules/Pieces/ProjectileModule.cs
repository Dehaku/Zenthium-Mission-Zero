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

        protected override bool CanActivateNow(Vector3 aimDirection)
        {
            return aimDirection.sqrMagnitude > 0f;
        }

        protected override void Activate(Vector3 aimDirection)
        {
            Projectile projectile = Context.World.SpawnProjectile(Context.Position, aimDirection);
            if (projectile != null)
            {
                projectile.Launch(Context, effects, aimDirection, Data.RangeTier.Value);
            }
        }
    }
}
