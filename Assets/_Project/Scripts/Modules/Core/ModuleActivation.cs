using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>Why an activated module can't be used right now.</summary>
    public enum ActivationBlock
    {
        None,
        Incapacitated,

        /// <summary>Stunned or Engulfed.</summary>
        CantAct,

        /// <summary>Its slot is locked, empty, disabled, or Silenced.</summary>
        SlotUnusable,
        Cooldown,
        CantAfford,
    }

    /// <summary>
    /// The gate every activated module shares (modules.md, Schema: Resources and Cooldown): the user can act, the
    /// slot works, the cooldown is ready and the cost is affordable. Using it spends the cost and starts the
    /// cooldown, which runs in real seconds (<c>GameClock.CooldownDelta</c>), ignoring the timescale.
    /// </summary>
    public sealed class ModuleActivation
    {
        public ModuleActivation(float? cooldownSeconds, ResourceType? resource, float cost)
        {
            Cooldown = cooldownSeconds.HasValue ? new ModuleCooldown(cooldownSeconds.Value) : null;
            HasCost = resource.HasValue;
            Resource = resource.GetValueOrDefault();
            Cost = HasCost ? cost : 0f;
        }

        /// <summary>Null if the module has no cooldown.</summary>
        public ModuleCooldown Cooldown { get; }

        public bool HasCost { get; }
        public ResourceType Resource { get; }
        public float Cost { get; }

        public static ModuleActivation FromData(ModuleData data)
        {
            float? cooldown = data.TryGetCooldown(out float seconds) ? seconds : (float?)null;
            ResourceType? resource = data.TryGetResourceCost(out ResourceType spent, out float cost)
                ? spent
                : (ResourceType?)null;
            return new ModuleActivation(cooldown, resource, cost);
        }

        /// <param name="slotUsable">False if its slot is locked, disabled or Silenced. Base actions pass true.</param>
        public ActivationBlock Check(Entity user, bool slotUsable)
        {
            if (user.IsIncapacitated)
            {
                return ActivationBlock.Incapacitated;
            }

            if (!user.Statuses.CanAct)
            {
                return ActivationBlock.CantAct;
            }

            if (!slotUsable)
            {
                return ActivationBlock.SlotUnusable;
            }

            if (Cooldown != null && !Cooldown.IsReady)
            {
                return ActivationBlock.Cooldown;
            }

            if (HasCost && !user.Resources.CanAfford(Resource, Cost))
            {
                return ActivationBlock.CantAfford;
            }

            return ActivationBlock.None;
        }

        /// <summary>Spends the cost and starts the cooldown, if <see cref="Check"/> allows it.</summary>
        public bool TryBegin(Entity user, bool slotUsable)
        {
            if (Check(user, slotUsable) != ActivationBlock.None)
            {
                return false;
            }

            if (HasCost)
            {
                user.Resources.TrySpend(Resource, Cost);
            }

            if (Cooldown != null)
            {
                Cooldown.Start();
            }

            return true;
        }

        /// <param name="cooldownDelta">Real seconds since the last tick, 0 while paused.</param>
        public void Tick(float cooldownDelta)
        {
            if (Cooldown != null)
            {
                Cooldown.Tick(cooldownDelta);
            }
        }
    }
}
