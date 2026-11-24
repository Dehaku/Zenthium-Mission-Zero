using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Power Throw (modules.md): throws everything the user is dragging along the aim, as far as the range tier, as
    /// a swept knockback that stops at walls. Thrown things deal no damage until props exist (Milestone 5, with
    /// their Strength limits).
    /// </summary>
    public sealed class PowerThrowModule : ModuleBehaviour
    {
        // Thrown bodies start this far in front of the user, so the user's own body doesn't stop them.
        private const float ThrowStartDistance = 1.2f;

        private readonly List<IDragTarget> thrown = new List<IDragTarget>();
        private DragAction drag;

        public PowerThrowModule(ModuleData data) : base(data)
        {
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Throws what you're dragging ").Append(Data.RangeTier.Value).Append(" m\n");
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return aim.Direction.sqrMagnitude > 0f && Context.OwnerBehaviour.TryGetComponent(out drag)
                && drag.CarriedCount > 0;
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 direction = aim.Direction.normalized;
            Vector3 start = Context.Position + direction * ThrowStartDistance;
            drag.ReleaseAll(thrown);
            foreach (IDragTarget target in thrown)
            {
                if (target.Body == null || !target.Body.TryGetComponent(out EntityMotor motor))
                {
                    continue;
                }

                target.Body.position = new Vector3(start.x, target.Body.position.y, start.z);
                motor.ApplyKnockback(direction, Data.RangeTier.Value);
            }

            thrown.Clear();
        }
    }
}
