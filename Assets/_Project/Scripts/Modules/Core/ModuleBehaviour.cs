using System;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A module's behavior: code linked to a data row by its id (<see cref="ModuleCatalog"/>), built from the shared
    /// parts. Actives and gear actives are used through <see cref="TryActivate"/>, behind the shared
    /// <see cref="ModuleActivation"/> gate. The trigger methods (on hit, on damage taken, on Defiance proc, on
    /// defeat, and the timer tick) are called by <see cref="EntityModules"/> only while the module is enabled: equipped,
    /// in a slot that isn't disabled.
    /// </summary>
    public abstract class ModuleBehaviour
    {
        protected ModuleBehaviour(ModuleData data)
        {
            Data = data ? data : throw new ArgumentNullException(nameof(data));
            Activation = ModuleCategories.IsActivated(data.Category) ? ModuleActivation.FromData(data) : null;
        }

        public ModuleData Data { get; }

        /// <summary>Null for passives.</summary>
        public ModuleActivation Activation { get; }

        public bool IsEnabled { get; private set; }

        /// <summary>Null while detached.</summary>
        protected ModuleContext Context { get; private set; }

        protected Entity Owner => Context.Owner;

        /// <param name="aimDirection">The user's aim on the XZ plane.</param>
        /// <param name="slotUsable">False if its slot is locked, disabled or Silenced. Base actions pass true.</param>
        /// <returns>True if it was used.</returns>
        public virtual bool TryActivate(Vector3 aimDirection, bool slotUsable)
        {
            if (Activation == null || Context == null || !IsEnabled
                || Activation.Check(Owner, slotUsable) != ActivationBlock.None || !CanActivateNow(aimDirection))
            {
                return false;
            }

            Activation.TryBegin(Owner, slotUsable);
            Activate(aimDirection);
            return true;
        }

        /// <summary>Lines for the MoreInfo display: the formula with live numbers, and the module's own values.</summary>
        public virtual void DescribeEffects(StringBuilder text)
        {
        }

        internal void Attach(ModuleContext context)
        {
            Context = context;
        }

        internal void Detach()
        {
            SetEnabled(false);
            Context = null;
        }

        internal void SetEnabled(bool enabled)
        {
            if (enabled == IsEnabled || (enabled && Context == null))
            {
                return;
            }

            IsEnabled = enabled;
            if (enabled)
            {
                OnEnabled();
            }
            else
            {
                OnDisabled();
            }
        }

        /// <summary>Checks that only this module makes, such as needing a target in reach. Nothing is spent yet.</summary>
        protected virtual bool CanActivateNow(Vector3 aimDirection)
        {
            return true;
        }

        /// <summary>The module's effect, after the cost is spent and the cooldown started.</summary>
        protected virtual void Activate(Vector3 aimDirection)
        {
        }

        /// <summary>Always-on effects start here, such as a passive's Armor.</summary>
        protected virtual void OnEnabled()
        {
        }

        /// <summary>Undo everything <see cref="OnEnabled"/> did.</summary>
        protected virtual void OnDisabled()
        {
        }

        /// <summary>The timer and approach triggers.</summary>
        /// <param name="gameDelta">The user's game seconds since the last tick.</param>
        protected internal virtual void Tick(float gameDelta)
        {
        }

        protected internal virtual void OnDamageTaken(DamageEvent damage)
        {
        }

        protected internal virtual void OnHitDealt(DamageEvent hit)
        {
        }

        protected internal virtual void OnDefianceProc()
        {
        }

        protected internal virtual void OnDefeatedTarget(Entity target)
        {
        }
    }
}
