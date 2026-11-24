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
        private readonly float[] extraRegen = new float[ResourceCount];

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

        /// <summary>
        /// Everything one equipped module grants (combat.md, Resources): enough of the resource it spends to use it
        /// once; the extra capacity and regen of resource passives (<c>&lt;resource&gt;</c> and
        /// <c>&lt;resource&gt;_regen</c> in its values), on top of the baseline; and its <c>NoRegen</c> tags.
        /// </summary>
        public void GrantForModule(ModuleData module)
        {
            if (module == null)
            {
                return;
            }

            if (module.TryGetResourceCost(out ResourceType spent, out float cost))
            {
                Grant(spent, cost);
            }

            for (int i = 0; i < ResourceCount; i++)
            {
                var resource = (ResourceType)i;
                if (module.Values.TryGetNumber(CapacityKey(resource), out float capacity) && capacity > 0f)
                {
                    Grant(resource, capacity);
                }

                if (module.Values.TryGetNumber(RegenKey(resource), out float regen))
                {
                    extraRegen[i] += regen;
                }
            }

            foreach (ResourceType resource in module.NoRegenResources)
            {
                AddNoRegen(resource);
            }
        }

        /// <summary>Regen per real second: the baseline plus resource passives. 0 when a NoRegen module stops it.</summary>
        public float RegenPerSecond(ResourceType resource)
        {
            int index = (int)resource;
            return noRegenSources[index] > 0 ? 0f : constants.ResourceRegenPerSecond + extraRegen[index];
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

                current[i] = Math.Min(max[i], current[i] + RegenPerSecond((ResourceType)i) * cooldownDelta);
                RaiseResourceChanged(i);
            }
        }

        // Keys of resource passives' values in modules.csv, such as Sprinter's stamina=100; stamina_regen=10.
        private static string CapacityKey(ResourceType resource)
        {
            switch (resource)
            {
                case ResourceType.Stamina: return "stamina";
                case ResourceType.Mana: return "mana";
                case ResourceType.Charge: return "charge";
                case ResourceType.Ammo: return "ammo";
                default: throw new ArgumentOutOfRangeException(nameof(resource), resource, null);
            }
        }

        private static string RegenKey(ResourceType resource)
        {
            switch (resource)
            {
                case ResourceType.Stamina: return "stamina_regen";
                case ResourceType.Mana: return "mana_regen";
                case ResourceType.Charge: return "charge_regen";
                case ResourceType.Ammo: return "ammo_regen";
                default: throw new ArgumentOutOfRangeException(nameof(resource), resource, null);
            }
        }

        private void RaiseResourceChanged(int index)
        {
            OnResourceChanged?.Invoke((ResourceType)index, current[index], max[index]);
        }
    }
}
