using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Statuses;
using Zenthium.Timing;

namespace Zenthium.Entities
{
    /// <summary>
    /// The one entity type: heroes, enemies and civilians differ only in their <see cref="EntitySetup"/>.
    /// Holds the rules every entity shares (stats, HP, defenses, Defiance, resources, slots, statuses) as plain C#;
    /// <see cref="EntityBehaviour"/> wraps it in a scene. Presentation subscribes to the events here.
    /// </summary>
    public sealed class Entity
    {
        // stats.md: Defiance leaves the entity at 1 HP.
        private const float DefianceHp = 1f;

        private readonly EntityRules rules;
        private readonly IRandomSource random;
        private readonly HashSet<EntityTag> tags;
        private readonly List<IIncomingHitHandler> incomingHitHandlers = new List<IIncomingHitHandler>();
        private readonly bool isLeader;
        private readonly float hpDamageBonus;

        /// <exception cref="ArgumentException">A module can't be slotted: no free slot, or a duplicate.</exception>
        public Entity(EntitySetup setup, EntityRules rules, IRandomSource random)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            Team = setup.Team;
            RankTier = setup.RankTier;
            tags = new HashSet<EntityTag>(setup.Tags);
            isLeader = setup.RankTier == Zenthium.Data.RankTier.Leader;
            hpDamageBonus = rules.RankScaling.HpDamageBonus(setup.DifficultyLevel);

            float innateBoost = setup.RankTier.HasValue ? rules.RankScaling.InnateBoost(setup.RankTier.Value) : 0f;
            Stats = new EntityStats(setup.BaseStats, innateBoost + rules.RankScaling.StatBonus(setup.DifficultyLevel));
            Health = new Health(CalculateMaxHp());
            Defenses = new Defenses(Stats, rules.Formulas);
            MoveSpeed = new MoveSpeed(rules.EntityConstants);
            DamageModifiers = new DamageModifiers();
            Defiance = new Defiance(Stats, rules.EntityConstants, Team == Team.Player);
            Invulnerability = new Invulnerability();
            Resources = new ResourcePools(rules.EntityConstants);
            Slots = CreateSlots(setup, rules.ModuleConstants.SlotsPerType);
            Statuses = new StatusEffects(this, rules.ModuleConstants, random);

            Stats.OnStatsChanged += UpdateMaxHp;
        }

        /// <summary>Every hit or DoT tick that lands, is dodged or is blocked.</summary>
        public event Action<DamageEvent> OnDamaged;

        /// <summary>A hit or DoT tick from this entity landed on someone (the "on hit" trigger).</summary>
        public event Action<DamageEvent> OnHitDealt;

        /// <summary>A hit from this entity Incapacitated the given entity.</summary>
        public event Action<Entity> OnDefeatedTarget;

        /// <summary>HP reached 0 and Defiance didn't proc.</summary>
        public event Action OnIncapacitated;

        /// <summary>Back on its feet after being Incapacitated.</summary>
        public event Action OnRevived;

        /// <summary>Defiance saved the entity at 1 HP.</summary>
        public event Action OnDefianceProc;

        public Team Team { get; }

        /// <summary>Goon, Champion or Leader; null for heroes and civilians.</summary>
        public RankTier? RankTier { get; }

        public EntityStats Stats { get; }
        public Health Health { get; }
        public Defenses Defenses { get; }
        public MoveSpeed MoveSpeed { get; }
        public DamageModifiers DamageModifiers { get; }
        public Defiance Defiance { get; }
        public Invulnerability Invulnerability { get; }
        public ResourcePools Resources { get; }
        public ModuleSlots Slots { get; }
        public StatusEffects Statuses { get; }

        public bool IsIncapacitated => Health.IsIncapacitated;
        public bool IsInvulnerable => Invulnerability.IsActive;

        /// <summary>Leaders and the player take half-length crowd control, and pulsed "while inside" crowd control.</summary>
        public bool ReceivesReducedCrowdControl => isLeader || Team == Team.Player;

        /// <summary>The entity dragging this one, or null. Dragged targets can't be hurt (modules.md, Drag).</summary>
        public Entity DraggedBy { get; private set; }

        public bool IsBeingDragged => DraggedBy != null;

        /// <summary>Time Slow and Time Stop don't affect the player (time.md).</summary>
        public bool IsExemptFromTimePowers => Team == Team.Player;

        /// <summary>The mission's rank damage bonus, applied to everything this entity deals.</summary>
        public float OutgoingDamageMultiplier => 1f + hpDamageBonus;

        public bool HasTag(EntityTag tag)
        {
            return tags.Contains(tag);
        }

        /// <summary>Whether an equipped module carries the tag, such as Immovable's KnockbackResistant.</summary>
        public bool HasModuleTag(ModuleTag tag)
        {
            return Slots.HasTag(tag);
        }

        /// <param name="carrier">The entity dragging this one; null when it lets go.</param>
        public void SetDraggedBy(Entity carrier)
        {
            if (carrier == this)
            {
                throw new ArgumentException("An entity can't drag itself.", nameof(carrier));
            }

            DraggedBy = carrier;
        }

        /// <summary>Invulnerable until the matching <see cref="RemoveInvulnerableHold"/>, such as during a dash.</summary>
        public void AddInvulnerableHold()
        {
            Invulnerability.AddHold();
        }

        public void RemoveInvulnerableHold()
        {
            Invulnerability.RemoveHold();
        }

        public void AddIncomingHitHandler(IIncomingHitHandler handler)
        {
            if (handler != null && !incomingHitHandlers.Contains(handler))
            {
                incomingHitHandlers.Add(handler);
            }
        }

        public void RemoveIncomingHitHandler(IIncomingHitHandler handler)
        {
            incomingHitHandlers.Remove(handler);
        }

        /// <param name="realDelta">Unscaled seconds since the last tick.</param>
        public void Tick(GameClock clock, float realDelta)
        {
            float gameDelta = clock.GameDelta(realDelta, IsExemptFromTimePowers);
            Invulnerability.Tick(gameDelta);
            Statuses.Tick(gameDelta);
            Resources.Tick(clock.CooldownDelta(realDelta));
        }

        /// <returns>The HP actually restored. Healing doesn't revive an Incapacitated entity.</returns>
        public float Heal(float amount)
        {
            return Health.Heal(amount);
        }

        /// <summary>Gets an Incapacitated entity back up with the given HP (at least 1).</summary>
        /// <returns>False if it wasn't Incapacitated.</returns>
        public bool Revive(float hp)
        {
            if (!Health.Revive(hp))
            {
                return false;
            }

            OnRevived?.Invoke();
            return true;
        }

        /// <summary>Called by the damage pipeline only, for a direct hit that's about to be mitigated.</summary>
        internal void HandleIncomingHit(ref IncomingHit hit)
        {
            // Backwards by index, so a handler may remove itself.
            for (int i = incomingHitHandlers.Count - 1; i >= 0; i--)
            {
                incomingHitHandlers[i].OnIncomingHit(ref hit);
            }
        }

        /// <summary>Called by the damage pipeline only, after it has decided the outcome and amount.</summary>
        internal void ReceiveDamage(DamageEvent damage)
        {
            OnDamaged?.Invoke(damage);
            if (damage.Outcome != DamageOutcome.Dealt)
            {
                return;
            }

            if (damage.Amount < Health.Current)
            {
                Health.Lose(damage.Amount);
                return;
            }

            if (Defiance.TryProc(random))
            {
                Health.SetCurrent(DefianceHp);
                Statuses.CleanseForDefiance();
                Invulnerability.Grant(Defiance.InvulnerableSeconds);
                OnDefianceProc?.Invoke();
                return;
            }

            Health.Lose(Health.Current);
            OnIncapacitated?.Invoke();
        }

        /// <summary>Called by the damage pipeline on the source of a hit that landed.</summary>
        internal void NotifyHitDealt(DamageEvent hit)
        {
            OnHitDealt?.Invoke(hit);
            if (hit.Target != null && hit.Target.IsIncapacitated)
            {
                OnDefeatedTarget?.Invoke(hit.Target);
            }
        }

        private ModuleSlots CreateSlots(EntitySetup setup, int slotsPerType)
        {
            var slots = new ModuleSlots(slotsPerType);
            var slottedPerType = new int[Enum.GetValues(typeof(SlotType)).Length];
            foreach (ModuleData module in setup.Modules)
            {
                if (module == null || !module.TryGetSlotType(out SlotType type))
                {
                    continue;
                }

                // Slots fill in order, so the first ones of each type are the unlocked ones.
                bool unlocked = slottedPerType[(int)type] < setup.UnlockedSlotsPerType;
                SlotAddResult result = slots.Add(module, unlocked);
                if (result != SlotAddResult.Added)
                {
                    throw new ArgumentException($"Can't slot {module.Id}: {result}.", nameof(setup));
                }

                slottedPerType[(int)type]++;
                if (unlocked)
                {
                    Resources.GrantForModule(module);
                }
            }

            return slots;
        }

        private float CalculateMaxHp()
        {
            return rules.Formulas.MaxHp(Stats.Get(Stat.Toughness), isLeader) * (1f + hpDamageBonus);
        }

        private void UpdateMaxHp()
        {
            float max = CalculateMaxHp();
            if (max != Health.Max)
            {
                Health.SetMax(max);
            }
        }
    }
}
