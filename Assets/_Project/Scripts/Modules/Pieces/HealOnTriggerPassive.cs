using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>When a <see cref="HealOnTriggerPassive"/> heals.</summary>
    public enum HealTrigger
    {
        /// <summary>Second Wind: when Defiance procs.</summary>
        DefianceProc,

        /// <summary>Fresh Ink: each enemy or neutral the user's own hits defeat.</summary>
        DefeatedOpponent,
    }

    /// <summary>
    /// Heals the user a fraction of their max HP on a trigger: Second Wind (<c>heal</c>, on Defiance) and Fresh Ink
    /// (<c>heal_per_defeat</c>, per defeat), modules.md.
    /// </summary>
    public sealed class HealOnTriggerPassive : ModuleBehaviour
    {
        public const string DefianceHealKey = "heal";
        public const string DefeatHealKey = "heal_per_defeat";

        private readonly HealTrigger trigger;
        private readonly float fractionOfMax;

        public HealOnTriggerPassive(ModuleData data, HealTrigger trigger, float fractionOfMax) : base(data)
        {
            this.trigger = trigger;
            this.fractionOfMax = fractionOfMax;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Heals ").Append((fractionOfMax * 100f).ToString("0.#", CultureInfo.InvariantCulture))
                .Append(trigger == HealTrigger.DefianceProc ? "% of max HP when Defiance procs\n"
                    : "% of max HP per enemy you defeat\n");
        }

        protected internal override void OnDefianceProc()
        {
            if (trigger == HealTrigger.DefianceProc)
            {
                Heal();
            }
        }

        protected internal override void OnDefeatedTarget(Entity target)
        {
            if (trigger == HealTrigger.DefeatedOpponent && Opponents.Are(Owner, target))
            {
                Heal();
            }
        }

        private void Heal()
        {
            Owner.Heal(Owner.Health.Max * fractionOfMax);
        }
    }
}
