using System;
using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// The text the MoreInfo button shows (modules.md, Module info): each module's formula with live numbers, its
    /// cooldown and cost, and which slots are locked or disabled. Plain text until Milestone 10's HUD.
    /// </summary>
    public static class ModuleInfoText
    {
        private const string NumberFormat = "0.#";

        public static void Build(StringBuilder text, EntityModules modules)
        {
            text.Clear();
            Entity entity = modules.Entity;
            if (entity == null)
            {
                return;
            }

            if (modules.Kick != null)
            {
                text.Append("<b>Kick</b>\n");
                Describe(text, modules.Kick, entity);
            }

            foreach (SlotType type in (SlotType[])Enum.GetValues(typeof(SlotType)))
            {
                for (int index = 0; index < entity.Slots.SlotsPerType; index++)
                {
                    ModuleData data = entity.Slots.Get(type, index);
                    if (data == null)
                    {
                        continue;
                    }

                    text.Append("<b>").Append(type).Append(' ').Append(index + 1).Append(": ").Append(data.Id)
                        .Append("</b>");
                    ModuleBehaviour module = modules.GetSlotted(type, index);
                    if (!entity.Slots.IsUnlocked(type, index))
                    {
                        text.Append(" (locked)\n");
                    }
                    else if (module == null)
                    {
                        text.Append(" (not built yet)\n");
                    }
                    else
                    {
                        text.Append(entity.Slots.IsDisabled(type, index) ? " (disabled)\n" : "\n");
                        Describe(text, module, entity);
                    }
                }
            }
        }

        private static void Describe(StringBuilder text, ModuleBehaviour module, Entity entity)
        {
            module.DescribeEffects(text);
            ModuleActivation activation = module.Activation;
            if (activation == null)
            {
                return;
            }

            if (activation.Cooldown != null)
            {
                text.Append("Cooldown ").Append(Format(activation.Cooldown.Seconds)).Append(" s");
                if (!activation.Cooldown.IsReady)
                {
                    text.Append(" (").Append(Format(activation.Cooldown.Remaining)).Append(" s left)");
                }

                text.Append('\n');
            }

            if (activation.HasCost)
            {
                text.Append("Cost ").Append(Format(activation.Cost)).Append(' ').Append(activation.Resource)
                    .Append(" (have ").Append(Format(entity.Resources.Current(activation.Resource))).Append(")\n");
            }
        }

        private static string Format(float value)
        {
            return value.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
