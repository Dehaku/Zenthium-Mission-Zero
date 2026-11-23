using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's Stamina, Mana, Charge and Ammo (combat.md). Every module that spends a resource grants enough
    /// of it to be used once, and grants from several modules add to the same pool. Pools start full and regain
    /// a baseline amount per real second, unless a <c>NoRegen:&lt;Resource&gt;</c> module stops it.
    /// </summary>
    public sealed class ResourcePools
    {
        private static readonly int ResourceCount = Enum.GetValues(typeof(ResourceType)).Length;

        private readonly EntityConstants constants;
        private readonly float[] current = new float[ResourceCount];
        private readonly float[] max = new float[ResourceCount];
        private readonly int[] noRegenSources = new int[ResourceCount];

        internal ResourcePools(EntityConstants constants)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        }

        /// <summary>The resource, its current amount and its max, after any change.</summary>
        public event Action<ResourceType, float, float> OnResourceChanged;

        public float Current(ResourceType resource)
        {
            return current[(int)resource];
        }

        public float Max(ResourceType resource)
        {
            return max[(int)resource];
        }

        /// <summary>Whether the entity has this resource at all.</summary>
        public bool Has(ResourceType resource)
        {
            return max[(int)resource] > 0f;
        }

        public bool Regenerates(ResourceType resource)
        {
            return noRegenSources[(int)resource] == 0;
        }

        /// <summary>Adds the cost of a module that spends a resource. Modules that spend nothing grant nothing.</summary>
        public void GrantForModule(ModuleData module)
        {
            if (module != null && module.TryGetResourceCost(out ResourceType resource, out float cost))
            {
                Grant(resource, cost);
            }
        }

        /// <summary>Raises the pool's max and fills the new space, so a fresh grant is usable at once.</summary>
        public void Grant(ResourceType resource, float amount)
        {
            int index = (int)resource;
            max[index] += amount;
            current[index] += amount;
            RaiseResourceChanged(index);
        }

        /// <summary>From a module with <c>NoRegen:&lt;Resource&gt;</c>: stops all regen of it, the baseline included.</summary>
        public void AddNoRegen(ResourceType resource)
        {
            noRegenSources[(int)resource]++;
        }

        public void RemoveNoRegen(ResourceType resource)
        {
            int index = (int)resource;
            noRegenSources[index] = Math.Max(0, noRegenSources[index] - 1);
        }

        public bool CanAfford(ResourceType resource, float cost)
        {
            return current[(int)resource] >= cost;
        }

        /// <returns>False, spending nothing, if there isn't enough.</returns>
        public bool TrySpend(ResourceType resource, float cost)
        {
            if (!CanAfford(resource, cost))
            {
                return false;
            }

            int index = (int)resource;
            current[index] -= cost;
            RaiseResourceChanged(index);
            return true;
        }

        /// <param name="cooldownDelta">Real seconds, stopped while paused (<c>GameClock.CooldownDelta</c>).</param>
        internal void Tick(float cooldownDelta)
        {
            for (int i = 0; i < ResourceCount; i++)
            {
                if (noRegenSources[i] > 0 || current[i] >= max[i])
                {
                    continue;
                }

                current[i] = Math.Min(max[i], current[i] + constants.ResourceRegenPerSecond * cooldownDelta);
                RaiseResourceChanged(i);
            }
        }

        private void RaiseResourceChanged(int index)
        {
            OnResourceChanged?.Invoke((ResourceType)index, current[index], max[index]);
        }
    }
}
