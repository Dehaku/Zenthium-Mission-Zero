using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Dash (modules.md): a quick burst along the aim, as far as its range tier, Invulnerable for exactly as long
    /// as the dash lasts. It stops at walls like a knockback. Rooted or Stunned users can't dash.
    /// </summary>
    public sealed class DashModule : ModuleBehaviour
    {
        private EntityMotor dashingMotor;
        private Entity dashingUser;

        public DashModule(ModuleData data) : base(data)
        {
        }

        protected override bool CanActivateNow(Vector3 aimDirection)
        {
            return Context.Motor != null && !Context.Motor.IsDashing && Owner.Statuses.CanMove
                && aimDirection.sqrMagnitude > 0f;
        }

        protected override void Activate(Vector3 aimDirection)
        {
            if (!Context.Motor.TryDash(aimDirection, Data.RangeTier.Value))
            {
                return;
            }

            dashingMotor = Context.Motor;
            dashingUser = Owner;
            dashingUser.AddInvulnerableHold();
            dashingMotor.OnDashEnded += EndInvulnerable;
        }

        protected override void OnDisabled()
        {
            EndInvulnerable();
        }

        private void EndInvulnerable()
        {
            if (dashingMotor == null)
            {
                return;
            }

            dashingMotor.OnDashEnded -= EndInvulnerable;
            dashingUser.RemoveInvulnerableHold();
            dashingMotor = null;
            dashingUser = null;
        }
    }
}
