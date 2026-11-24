using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Statuses
{
    /// <summary>
    /// An entity's statuses (statuses.md), with the rules they share:
    /// <list type="bullet">
    /// <item>Stacking per status: Refresh, Intensity, Independent or no stacking. A refresh never shortens the time left.</item>
    /// <item>Durations run on the entity's game time, so Time Slow stretches them and Time Stop freezes them.</item>
    /// <item>Crowd control lasts half as long on Leaders and the player; "while inside" crowd control pulses on them.</item>
    /// <item>Invulnerable entities take no new hostile statuses; a Defiance proc cleanses them.</item>
    /// <item>Disables switch off a random slot each; slots with <c>CannotBeDisabled</c> modules are skipped.</item>
    /// </list>
    /// Resists are rolled by whoever applies a debuff (<see cref="ResistRules"/>), before calling here.
    /// </summary>
    public sealed class StatusEffects
    {
        private readonly Entity owner;
        private readonly ModuleConstants constants;
        private readonly IRandomSource random;
        private readonly List<Instance> instances = new List<Instance>();

        internal StatusEffects(Entity owner, ModuleConstants constants, IRandomSource random)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Raised when a status starts, ends, gains a layer or pulses on or off.</summary>
        public event Action OnStatusesChanged;

        /// <summary>False while Stunned, Rooted or Engulfed. Knockback still moves the entity.</summary>
        public bool CanMove => !AnyStops(movement: true);

        /// <summary>False while Stunned or Engulfed: no attacks or powers.</summary>
        public bool CanAct => !AnyStops(movement: false);

        /// <summary>Every active power slot stops working (Silenced).</summary>
        public bool IsSilenced => Has(StatusType.Silenced);

        public bool Has(StatusType type)
        {
            foreach (Instance instance in instances)
            {
                if (instance.Type == type && instance.IsOn)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Layers of an Intensity status, or the number of Independent instances; 0 if absent.</summary>
        public int Layers(StatusType type)
        {
            int layers = 0;
            foreach (Instance instance in instances)
            {
                if (instance.Type == type && instance.IsOn)
                {
                    layers += instance.Layers;
                }
            }

            return layers;
        }

        /// <summary>The longest time left on the status, in game seconds; infinity if it lasts until removed.</summary>
        public float RemainingSeconds(StatusType type)
        {
            float remaining = 0f;
            foreach (Instance instance in instances)
            {
                if (instance.Type == type)
                {
                    remaining = Math.Max(remaining, instance.Remaining);
                }
            }

            return remaining;
        }

        /// <summary>Fills the list with each active status once, in the order they started.</summary>
        public void GetActive(List<StatusType> results)
        {
            results.Clear();
            foreach (Instance instance in instances)
            {
                if (instance.IsOn && !results.Contains(instance.Type))
                {
                    results.Add(instance.Type);
                }
            }
        }

        /// <returns>
        /// False if it didn't take: the entity is captured or Invulnerable (hostile statuses), it doesn't
        /// stack, no slot is left to disable, or there's no time.
        /// </returns>
        public bool TryApply(in StatusApplication application)
        {
            StatusInfo info = StatusCatalog.Get(application.Type);
            if (owner.IsCaptured)
            {
                return false;
            }

            if (info.IsHostile && owner.IsInvulnerable)
            {
                return false;
            }

            float seconds = application.Seconds;
            if (info.IsCrowdControl && owner.ReceivesReducedCrowdControl && !float.IsPositiveInfinity(seconds))
            {
                seconds *= constants.CrowdControlDurationMultiplier;
            }

            if (!(seconds > 0f))
            {
                return false;
            }

            Instance existing = info.Stacking == StatusStacking.Independent ? null : FindTimed(application.Type);
            if (existing != null)
            {
                return Restack(existing, info, application, seconds);
            }

            var instance = new Instance(application, info, seconds);
            if (info.DisablesSlot.HasValue)
            {
                instance.DisabledSlot = owner.Slots.DisableRandom(info.DisablesSlot.Value, random);
                if (instance.DisabledSlot < 0)
                {
                    return false;
                }
            }

            instances.Add(instance);
            ApplyModifiers(instance);
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Keeps a "while inside" status on while the entity is in an area, such as Smoke Bomb's stun. Call it every
        /// tick the entity is inside; repeated calls for the same status and source do nothing.
        /// </summary>
        /// <returns>False if Invulnerable blocks it.</returns>
        public bool SetInsideArea(in StatusApplication application)
        {
            if (owner.IsCaptured)
            {
                return false;
            }

            if (FindArea(application.Type, application.Source) != null)
            {
                return true;
            }

            StatusInfo info = StatusCatalog.Get(application.Type);
            if (info.IsHostile && owner.IsInvulnerable)
            {
                return false;
            }

            var instance = new Instance(application, info, StatusApplication.UntilRemoved)
            {
                IsArea = true,
                IsPulsed = info.IsCrowdControl && owner.ReceivesReducedCrowdControl,
            };
            instances.Add(instance);
            ApplyModifiers(instance);
            RaiseChanged();
            return true;
        }

        /// <summary>Ends a "while inside" status when the entity leaves the area.</summary>
        public void LeaveArea(StatusType type, Entity source)
        {
            Instance area = FindArea(type, source);
            if (area != null)
            {
                RemoveAt(instances.IndexOf(area));
                RaiseChanged();
            }
        }

        /// <summary>
        /// A Defiance proc clears hostile statuses: DoTs and debuffs. Carrying Treasure and hidden markers stay
        /// (stats.md, Defiance).
        /// </summary>
        internal void CleanseForDefiance()
        {
            bool removed = false;
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (instances[i].Info.IsHostile)
                {
                    RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                RaiseChanged();
            }
        }

        /// <summary>Ends every status, such as when the entity is captured.</summary>
        internal void ClearAll()
        {
            if (instances.Count == 0)
            {
                return;
            }

            for (int i = instances.Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }

            RaiseChanged();
        }

        /// <param name="gameDelta">The entity's game seconds since the last tick.</param>
        internal void Tick(float gameDelta)
        {
            bool changed = false;
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                Instance instance = instances[i];
                if (instance.IsArea)
                {
                    changed |= instance.IsPulsed && UpdatePulse(instance, gameDelta);
                    continue;
                }

                if (float.IsPositiveInfinity(instance.Remaining))
                {
                    continue;
                }

                instance.Remaining -= gameDelta;
                if (instance.Remaining <= 0f)
                {
                    RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RaiseChanged();
            }
        }

        private bool Restack(Instance existing, StatusInfo info, in StatusApplication application, float seconds)
        {
            if (info.Stacking == StatusStacking.NoStack)
            {
                return false;
            }

            if (info.Stacking == StatusStacking.Intensity
                && (application.MaxLayers <= 0 || existing.Layers < application.MaxLayers))
            {
                existing.Layers++;
            }

            existing.Source = application.Source;
            existing.Remaining = Math.Max(existing.Remaining, seconds);
            RemoveModifiers(existing);
            existing.Modifiers = application.Modifiers;
            ApplyModifiers(existing);
            RaiseChanged();
            return true;
        }

        private bool UpdatePulse(Instance instance, float gameDelta)
        {
            instance.SecondsInside += gameDelta;
            bool on = CrowdControlPulse.IsOn(
                instance.SecondsInside, constants.CrowdControlPulseOnSeconds, constants.CrowdControlPulseOffSeconds);
            if (on == instance.IsOn)
            {
                return false;
            }

            instance.IsOn = on;
            if (on)
            {
                ApplyModifiers(instance);
            }
            else
            {
                RemoveModifiers(instance);
            }

            return true;
        }

        /// <param name="movement">True to check what stops moving, false for what stops acting.</param>
        private bool AnyStops(bool movement)
        {
            foreach (Instance instance in instances)
            {
                if (instance.IsOn && (movement ? instance.Info.StopsMoving : instance.Info.StopsActing))
                {
                    return true;
                }
            }

            return false;
        }

        private Instance FindTimed(StatusType type)
        {
            foreach (Instance instance in instances)
            {
                if (instance.Type == type && !instance.IsArea)
                {
                    return instance;
                }
            }

            return null;
        }

        private Instance FindArea(StatusType type, Entity source)
        {
            foreach (Instance instance in instances)
            {
                if (instance.IsArea && instance.Type == type && instance.Source == source)
                {
                    return instance;
                }
            }

            return null;
        }

        private void RemoveAt(int index)
        {
            Instance instance = instances[index];
            RemoveModifiers(instance);
            if (instance.DisabledSlot >= 0 && instance.Info.DisablesSlot.HasValue)
            {
                owner.Slots.Enable(instance.Info.DisablesSlot.Value, instance.DisabledSlot);
            }

            instances.RemoveAt(index);
        }

        private void ApplyModifiers(Instance instance)
        {
            if (!instance.IsOn || instance.Modifiers == null)
            {
                return;
            }

            foreach (StatusModifier modifier in instance.Modifiers)
            {
                instance.Handles.Add(AddModifier(modifier, modifier.Value * instance.Layers));
            }
        }

        private void RemoveModifiers(Instance instance)
        {
            // Handles were added in the same order as the modifiers they came from.
            for (int i = 0; i < instance.Handles.Count; i++)
            {
                RemoveModifier(instance.Modifiers[i], instance.Handles[i]);
            }

            instance.Handles.Clear();
        }

        private ModifierHandle AddModifier(StatusModifier modifier, float value)
        {
            switch (modifier.Target)
            {
                case StatusModifierTarget.Slow: return owner.MoveSpeed.AddSlow(value);
                case StatusModifierTarget.MoveSpeedBonus: return owner.MoveSpeed.AddBonus(value);
                case StatusModifierTarget.DamageDealt: return owner.DamageModifiers.AddDealt(value);
                case StatusModifierTarget.DamageTaken: return owner.DamageModifiers.AddTaken(value);
                case StatusModifierTarget.Armor: return owner.Defenses.AddArmor(value);
                case StatusModifierTarget.DamageReduction: return owner.Defenses.AddDamageReduction(value);
                case StatusModifierTarget.StatBoost: return owner.Stats.AddBoost(modifier.Stat, value);
                default: throw new ArgumentOutOfRangeException(nameof(modifier), modifier.Target, null);
            }
        }

        private void RemoveModifier(StatusModifier modifier, ModifierHandle handle)
        {
            switch (modifier.Target)
            {
                case StatusModifierTarget.Slow: owner.MoveSpeed.RemoveSlow(handle); break;
                case StatusModifierTarget.MoveSpeedBonus: owner.MoveSpeed.RemoveBonus(handle); break;
                case StatusModifierTarget.DamageDealt: owner.DamageModifiers.RemoveDealt(handle); break;
                case StatusModifierTarget.DamageTaken: owner.DamageModifiers.RemoveTaken(handle); break;
                case StatusModifierTarget.Armor: owner.Defenses.RemoveArmor(handle); break;
                case StatusModifierTarget.DamageReduction: owner.Defenses.RemoveDamageReduction(handle); break;
                case StatusModifierTarget.StatBoost: owner.Stats.RemoveBoost(modifier.Stat, handle); break;
                default: throw new ArgumentOutOfRangeException(nameof(modifier), modifier.Target, null);
            }
        }

        private void RaiseChanged()
        {
            OnStatusesChanged?.Invoke();
        }

        private sealed class Instance
        {
            public Instance(in StatusApplication application, StatusInfo info, float seconds)
            {
                Type = application.Type;
                Info = info;
                Source = application.Source;
                Remaining = seconds;
                Modifiers = application.Modifiers;
            }

            public StatusType Type { get; }
            public StatusInfo Info { get; }
            public List<ModifierHandle> Handles { get; } = new List<ModifierHandle>();
            public Entity Source { get; set; }
            public float Remaining { get; set; }
            public int Layers { get; set; } = 1;
            public IReadOnlyList<StatusModifier> Modifiers { get; set; }
            public int DisabledSlot { get; set; } = -1;
            public bool IsArea { get; set; }
            public bool IsPulsed { get; set; }
            public float SecondsInside { get; set; }
            public bool IsOn { get; set; } = true;
        }
    }
}
