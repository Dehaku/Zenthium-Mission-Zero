using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.AI
{
    /// <summary>One module in an entity's list, as the AI sees it when choosing.</summary>
    public readonly struct ModuleOption
    {
        /// <param name="isReady">Off cooldown, affordable, and its slot and user can act.</param>
        /// <param name="reach">Meters it reaches (<see cref="ModuleChooser.Reach"/>).</param>
        public ModuleOption(bool isReady, float reach)
        {
            IsReady = isReady;
            Reach = reach;
        }

        public bool IsReady { get; }
        public float Reach { get; }
    }

    /// <summary>Which module the AI uses (enemies.md, AI: Using modules).</summary>
    public static class ModuleChooser
    {
        /// <summary>The first module in list order that's ready and has the target in reach.</summary>
        /// <returns>Its index, or -1 if none.</returns>
        public static int FirstUsable(IReadOnlyList<ModuleOption> options, float targetDistance)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].IsReady && targetDistance <= options[i].Reach)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// How far a module reaches: its range; for one with only a radius (an area around the user), its radius;
        /// infinite for one with neither, such as a self-buff.
        /// </summary>
        public static float Reach(ModuleData module)
        {
            if (module.RangeTier.IsSet)
            {
                return module.RangeTier.Value;
            }

            return module.RadiusTier.IsSet ? module.RadiusTier.Value : float.PositiveInfinity;
        }
    }
}
