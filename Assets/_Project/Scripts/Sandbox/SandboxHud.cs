using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenthium.Core.Controls;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Player;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A plain-text HUD for sandbox scenes: the player's HP and resources, every action and slot with its button
    /// (keyboard / gamepad, read from the input actions), and whether each is ready, cooling down, locked or
    /// disabled. Test tool only; the real HUD comes in Milestone 10.
    /// </summary>
    public sealed class SandboxHud : MonoBehaviour
    {
        // Control scheme binding groups in InputSystem_Actions.inputactions.
        private const string KeyboardGroup = "Keyboard&Mouse";
        private const string GamepadGroup = "Gamepad";

        private const string NumberFormat = "0.#";
        private const string ReadyColor = "#7CFC7C";
        private const string WaitColor = "#FFD966";
        private const string OffColor = "#999999";
        private const string BlockedColor = "#FF8080";

        // controls.md: four Power buttons and four Gear buttons, one per slot.
        private const int SlotButtonCount = 4;

        [SerializeField] private PlayerController controller;
        [SerializeField] private EntityModules modules;
        [SerializeField] private TMP_Text label;

        [Tooltip("Real seconds between text updates.")]
        [SerializeField, Min(0.02f)] private float refreshSeconds = 0.1f;

        private readonly StringBuilder text = new StringBuilder();
        private readonly string[] powerKeys = new string[SlotButtonCount];
        private readonly string[] gearKeys = new string[SlotButtonCount];
        private InputSystem_Actions actions;
        private string kickKey;
        private string dragKey;
        private string sprintKey;
        private string moreInfoKey;
        private string friendlyFireKey;
        private string checklistKey;
        private float untilRefresh;

        private void Awake()
        {
            if (controller == null || modules == null || label == null)
            {
                Debug.LogError($"{name}: {nameof(SandboxHud)} is missing references.", this);
                enabled = false;
                return;
            }

            // Only read for button names; never enabled, so it doesn't compete with the player's input.
            actions = new InputSystem_Actions();
            InputSystem_Actions.PlayerActions player = actions.Player;
            kickKey = Keys(player.Kick);
            dragKey = Keys(player.Drag);
            sprintKey = Keys(player.Sprint);
            moreInfoKey = Keys(player.MoreInfo);
            friendlyFireKey = Keys(player.FriendlyFire);
            checklistKey = Keys(actions.Sandbox.ToggleChecklist);
            InputAction[] powers = { player.Power1, player.Power2, player.Power3, player.Power4 };
            InputAction[] gears = { player.Gear1, player.Gear2, player.Gear3, player.Gear4 };
            for (int i = 0; i < SlotButtonCount; i++)
            {
                powerKeys[i] = Keys(powers[i]);
                gearKeys[i] = Keys(gears[i]);
            }
        }

        private void OnDestroy()
        {
            if (actions != null)
            {
                actions.Dispose();
            }
        }

        private void Update()
        {
            untilRefresh -= Time.unscaledDeltaTime;
            if (untilRefresh > 0f)
            {
                return;
            }

            untilRefresh = refreshSeconds;
            Build();
            label.SetText(text);
        }

        private void Build()
        {
            text.Clear();
            Entity entity = modules.Entity;
            if (entity == null)
            {
                text.Append("No player entity.");
                return;
            }

            text.Append("HP ").Append(Mathf.CeilToInt(entity.Health.Current)).Append(" / ")
                .Append(Mathf.CeilToInt(entity.Health.Max));
            foreach (ResourceType resource in (ResourceType[])Enum.GetValues(typeof(ResourceType)))
            {
                if (entity.Resources.Has(resource))
                {
                    text.Append("   ").Append(resource).Append(' ')
                        .Append(Format(entity.Resources.Current(resource))).Append(" / ")
                        .Append(Format(entity.Resources.Max(resource))).Append(" (+")
                        .Append(Format(entity.Resources.RegenPerSecond(resource))).Append("/s)");
                }
            }

            text.Append("\n\n");
            Line(kickKey, "Kick");
            AppendStatus(modules.Kick, true, entity);
            Line(dragKey, "Drag").Append("grab a downed body / let go\n");
            Line(sprintKey, "Sprint (toggle)")
                .Append(controller.IsSprintOn ? Colored(ReadyColor, "ON") : Colored(OffColor, "off")).Append('\n');

            text.Append('\n');
            AppendSlots(SlotType.Active, powerKeys, entity);
            AppendSlots(SlotType.Passive, null, entity);
            AppendSlots(SlotType.Gear, gearKeys, entity);

            text.Append("\nHold ").Append(moreInfoKey).Append(": formulas   Hold ").Append(friendlyFireKey)
                .Append(": friendly fire   ").Append(checklistKey).Append(": test checklist");
        }

        private void AppendSlots(SlotType type, string[] keys, Entity entity)
        {
            for (int index = 0; index < entity.Slots.SlotsPerType; index++)
            {
                string key = keys != null && index < keys.Length ? keys[index] : "passive";
                ModuleData data = entity.Slots.Get(type, index);
                if (data == null)
                {
                    Line(key, type + " " + (index + 1)).Append(Colored(OffColor, "(empty)")).Append('\n');
                    continue;
                }

                Line(key, data.Id);
                if (!entity.Slots.IsUnlocked(type, index))
                {
                    text.Append(Colored(OffColor, "locked")).Append('\n');
                    continue;
                }

                AppendStatus(modules.GetSlotted(type, index), modules.IsSlotUsable(type, index), entity);
            }
        }

        private void AppendStatus(ModuleBehaviour module, bool slotUsable, Entity entity)
        {
            if (module == null)
            {
                text.Append(Colored(OffColor, "not built yet")).Append('\n');
                return;
            }

            ModuleActivation activation = module.Activation;
            if (activation == null)
            {
                text.Append(module.IsEnabled ? Colored(ReadyColor, "always on") : Colored(BlockedColor, "disabled"))
                    .Append('\n');
                return;
            }

            switch (activation.Check(entity, slotUsable))
            {
                case ActivationBlock.None:
                    text.Append(Colored(ReadyColor, "READY"));
                    break;
                case ActivationBlock.Cooldown:
                    text.Append(Colored(WaitColor, Format(activation.Cooldown.Remaining) + " s"));
                    break;
                case ActivationBlock.CantAfford:
                    text.Append(Colored(BlockedColor, "not enough " + activation.Resource));
                    break;
                case ActivationBlock.SlotUnusable:
                    text.Append(Colored(BlockedColor, entity.Statuses.IsSilenced ? "silenced" : "disabled"));
                    break;
                case ActivationBlock.CantAct:
                    text.Append(Colored(BlockedColor, "stunned"));
                    break;
                case ActivationBlock.Incapacitated:
                    text.Append(Colored(BlockedColor, "down"));
                    break;
            }

            if (activation.Cooldown != null)
            {
                text.Append("  cd ").Append(Format(activation.Cooldown.Seconds)).Append(" s");
            }

            if (activation.HasCost)
            {
                text.Append("  cost ").Append(Format(activation.Cost)).Append(' ').Append(activation.Resource);
            }

            text.Append('\n');
        }

        private StringBuilder Line(string key, string what)
        {
            return text.Append('[').Append(key).Append("] ").Append(what).Append(": ");
        }

        private static string Keys(InputAction action)
        {
            string keyboard = action.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardGroup));
            string gamepad = action.GetBindingDisplayString(InputBinding.MaskByGroup(GamepadGroup));
            return string.IsNullOrEmpty(gamepad) ? keyboard : keyboard + " / " + gamepad;
        }

        private static string Colored(string color, string value)
        {
            return "<color=" + color + ">" + value + "</color>";
        }

        private static string Format(float value)
        {
            return value.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
