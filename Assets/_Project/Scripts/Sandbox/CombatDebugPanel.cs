using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Sandbox_Combat's debug panel: damages, heals and changes the stats of a dummy entity, and shows
    /// the values the combat rules derive from them. Test tool only; never in a build.
    /// </summary>
    public sealed class CombatDebugPanel : MonoBehaviour
    {
        private const int LogLines = 8;
        private const float PercentPerFraction = 100f;

        private static readonly Stat[] AllStats = (Stat[])Enum.GetValues(typeof(Stat));

        [Header("Scene")]
        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;
        [SerializeField] private EntityBehaviour dummy;
        [SerializeField] private EntityBehaviour attacker;

        [Header("Readout")]
        [SerializeField] private TMP_Text readout;
        [SerializeField] private TMP_Text log;

        [Header("Damage")]
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private TMP_Dropdown damageTypeDropdown;
        [SerializeField] private Toggle damageOverTimeToggle;
        [SerializeField] private Toggle fromAttackerToggle;
        [SerializeField] private Button hitButton;
        [SerializeField] private Button kickButton;
        [SerializeField] private Button healButton;

        [Header("Stats")]
        [SerializeField] private TMP_Dropdown statDropdown;
        [SerializeField] private TMP_InputField statInput;
        [SerializeField] private Button setBaseStatButton;
        [SerializeField] private Button addBoostButton;

        [Header("Defenses")]
        [SerializeField] private TMP_InputField reductionInput;
        [SerializeField] private Button addReductionButton;
        [SerializeField] private Button clearModifiersButton;
        [SerializeField] private Button resetButton;

        private readonly StringBuilder text = new StringBuilder();
        private readonly Queue<string> logEntries = new Queue<string>();
        private Entity boundDummy;
        private bool isReadoutDirty;

        private void Awake()
        {
            FillDropdown(damageTypeDropdown, typeof(DamageType));
            FillDropdown(statDropdown, typeof(Stat));
        }

        private void OnEnable()
        {
            hitButton.onClick.AddListener(ApplyHit);
            kickButton.onClick.AddListener(ApplyKick);
            healButton.onClick.AddListener(HealDummy);
            setBaseStatButton.onClick.AddListener(SetBaseStat);
            addBoostButton.onClick.AddListener(AddBoost);
            addReductionButton.onClick.AddListener(AddReduction);
            clearModifiersButton.onClick.AddListener(ClearModifiers);
            resetButton.onClick.AddListener(ResetDummy);

            // The dummy may run Awake before or after this, so bind now and on every (re)creation.
            dummy.OnEntityCreated += Bind;
            if (dummy.Entity != null)
            {
                Bind(dummy.Entity);
            }
        }

        private void OnDisable()
        {
            hitButton.onClick.RemoveListener(ApplyHit);
            kickButton.onClick.RemoveListener(ApplyKick);
            healButton.onClick.RemoveListener(HealDummy);
            setBaseStatButton.onClick.RemoveListener(SetBaseStat);
            addBoostButton.onClick.RemoveListener(AddBoost);
            addReductionButton.onClick.RemoveListener(AddReduction);
            clearModifiersButton.onClick.RemoveListener(ClearModifiers);
            resetButton.onClick.RemoveListener(ResetDummy);

            if (dummy != null)
            {
                dummy.OnEntityCreated -= Bind;
            }

            Unbind();
        }

        private void Update()
        {
            // Invulnerable time counts down without an event, so keep refreshing while it runs.
            if (boundDummy != null && (isReadoutDirty || boundDummy.IsInvulnerable))
            {
                RefreshReadout();
            }
        }

        private void Bind(Entity entity)
        {
            Unbind();
            boundDummy = entity;
            boundDummy.OnDamaged += LogDamage;
            boundDummy.OnDefianceProc += LogDefiance;
            boundDummy.OnIncapacitated += LogIncapacitated;
            boundDummy.Health.OnHealthChanged += MarkHealthDirty;
            boundDummy.Stats.OnStatsChanged += MarkDirty;
            boundDummy.Defenses.OnDefensesChanged += MarkDirty;
            AddLog("Dummy created.");
            MarkDirty();
        }

        private void Unbind()
        {
            if (boundDummy == null)
            {
                return;
            }

            boundDummy.OnDamaged -= LogDamage;
            boundDummy.OnDefianceProc -= LogDefiance;
            boundDummy.OnIncapacitated -= LogIncapacitated;
            boundDummy.Health.OnHealthChanged -= MarkHealthDirty;
            boundDummy.Stats.OnStatsChanged -= MarkDirty;
            boundDummy.Defenses.OnDefensesChanged -= MarkDirty;
            boundDummy = null;
        }

        private void ApplyHit()
        {
            if (boundDummy == null || !TryReadNumber(amountInput, out float amount))
            {
                return;
            }

            Entity source = fromAttackerToggle.isOn ? attacker.Entity : null;
            var type = (DamageType)damageTypeDropdown.value;
            combatRules.Pipeline.Apply(new Hit(source, amount, type, damageOverTimeToggle.isOn), boundDummy);
        }

        /// <summary>A Kick from the attacker: Kick's damage tier with the attacker's Strength.</summary>
        private void ApplyKick()
        {
            if (boundDummy == null || attacker.Entity == null
                || !database.Modules.TryGet(DataIds.Modules.Kick, out ModuleData kick)
                || !kick.TryGetStat(out Stat stat))
            {
                return;
            }

            float damage = database.Formulas.TierDamage(attacker.Entity.Stats.Get(stat), kick.DamageTier);
            combatRules.Pipeline.Apply(new Hit(attacker.Entity, damage, DamageType.Physical), boundDummy);
        }

        private void HealDummy()
        {
            if (boundDummy != null && TryReadNumber(amountInput, out float amount))
            {
                AddLog($"Healed {boundDummy.Heal(amount):0.#}.");
            }
        }

        private void SetBaseStat()
        {
            if (boundDummy != null && TryReadNumber(statInput, out float value))
            {
                boundDummy.Stats.SetBase((Stat)statDropdown.value, value);
            }
        }

        private void AddBoost()
        {
            if (boundDummy != null && TryReadNumber(statInput, out float percent))
            {
                boundDummy.Stats.AddBoost((Stat)statDropdown.value, percent / PercentPerFraction);
            }
        }

        private void AddReduction()
        {
            if (boundDummy != null && TryReadNumber(reductionInput, out float percent))
            {
                boundDummy.Defenses.AddDamageReduction(percent / PercentPerFraction);
            }
        }

        private void ClearModifiers()
        {
            if (boundDummy == null)
            {
                return;
            }

            boundDummy.Stats.ClearBoosts();
            boundDummy.Defenses.ClearModifiers();
        }

        private void ResetDummy()
        {
            dummy.ResetEntity();
        }

        private void LogDamage(DamageEvent damage)
        {
            string kind = damage.IsDamageOverTime ? "DoT" : "hit";
            string from = damage.Source == null ? "environment" : damage.Source.Team.ToString();
            switch (damage.Outcome)
            {
                case DamageOutcome.Dealt:
                    AddLog($"{damage.Type} {kind} from {from}: {damage.Amount:0.#} damage.");
                    break;
                case DamageOutcome.Dodged:
                    AddLog($"{damage.Type} {kind} from {from}: dodged.");
                    break;
                case DamageOutcome.Blocked:
                    AddLog($"{damage.Type} {kind} from {from}: blocked (Invulnerable).");
                    break;
            }
        }

        private void LogDefiance()
        {
            AddLog("Defiance! Left at 1 HP.");
        }

        private void LogIncapacitated()
        {
            AddLog("Incapacitated.");
        }

        private void AddLog(string entry)
        {
            logEntries.Enqueue(entry);
            while (logEntries.Count > LogLines)
            {
                logEntries.Dequeue();
            }

            log.text = string.Join("\n", logEntries);
        }

        private void MarkHealthDirty(float current, float max)
        {
            MarkDirty();
        }

        private void MarkDirty()
        {
            isReadoutDirty = true;
        }

        private void RefreshReadout()
        {
            isReadoutDirty = false;
            Entity entity = boundDummy;
            text.Clear();
            text.Append(dummy.name).Append(" (").Append(entity.Team);
            if (entity.RankTier.HasValue)
            {
                text.Append(", ").Append(entity.RankTier.Value);
            }

            text.Append(")\n");
            text.Append("HP ").Append(entity.Health.Current.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" / ").Append(entity.Health.Max.ToString("0.#", CultureInfo.InvariantCulture));
            if (entity.IsIncapacitated)
            {
                text.Append("  INCAPACITATED");
            }

            if (entity.IsInvulnerable)
            {
                text.Append("  INVULNERABLE ")
                    .Append(entity.Invulnerability.RemainingSeconds.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append('s');
            }

            text.Append("\nArmor ").Append(entity.Defenses.Armor.ToString("0.#", CultureInfo.InvariantCulture))
                .Append("   Damage Reduction ").Append(Percent(entity.Defenses.DamageReduction))
                .Append("   Dodge ").Append(Percent(entity.Defenses.DodgeChance));
            text.Append("\nDefiance ").Append(Percent(entity.Defiance.ProcChance))
                .Append("   Defiance Roll stacks ").Append(entity.Defiance.RollStacks);
            text.Append("\nMove speed ").Append(entity.MoveSpeed.Current.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" m/s");
            foreach (Stat stat in AllStats)
            {
                text.Append('\n').Append(stat).Append(' ')
                    .Append(entity.Stats.Get(stat).ToString("0.#", CultureInfo.InvariantCulture))
                    .Append(" (base ")
                    .Append(entity.Stats.GetBase(stat).ToString("0.#", CultureInfo.InvariantCulture))
                    .Append(')');
            }

            readout.SetText(text);
        }

        private static string Percent(float fraction)
        {
            return (fraction * PercentPerFraction).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static bool TryReadNumber(TMP_InputField input, out float value)
        {
            return float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static void FillDropdown(TMP_Dropdown dropdown, Type enumType)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(Enum.GetNames(enumType)));
        }
    }
}
