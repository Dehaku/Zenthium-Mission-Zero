using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Timing;

namespace Zenthium.Entities
{
    /// <summary>
    /// The one entity type: heroes, enemies and civilians differ only in their <see cref="EntitySetup"/>.
    /// Holds the rules every entity shares (stats, HP, defenses, Defiance, resources) as plain C#;
    /// <see cref="EntityBehaviour"/> wraps it in a scene. Presentation subscribes to the events here.
    /// </summary>
    public sealed class Entity
    {
        // stats.md: Defiance leaves the entity at 1 HP.
        private const float DefianceHp = 1f;

        private readonly EntityRules rules;
        private readonly IRandomSource random;
        private readonly HashSet<EntityTag> tags;
        private readonly HashSet<ModuleTag> moduleTags;
        private readonly List<ModuleData> modules;
        private readonly bool isLeader;
        private readonly float hpDamageBonus;
        private IDefianceCleanse defianceCleanse;

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
            moduleTags = new HashSet<ModuleTag>(setup.ModuleTags);
            modules = new List<ModuleData>(setup.Modules);
            isLeader = setup.RankTier == Zenthium.Data.RankTier.Leader;
            hpDamageBonus = rules.RankScaling.HpDamageBonus(setup.DifficultyLevel);

            float innateBoost = setup.RankTier.HasValue ? rules.RankScaling.InnateBoost(setup.RankTier.Value) : 0f;
            Stats = new EntityStats(setup.BaseStats, innateBoost + rules.RankScaling.StatBonus(setup.DifficultyLevel));
            Health = new Health(CalculateMaxHp());
            Defenses = new Defenses(Stats, rules.Formulas);
            MoveSpeed = new MoveSpeed(rules.EntityConstants);
            Defiance = new Defiance(Stats, rules.EntityConstants, Team == Team.Player);
            Invulnerability = new Invulnerability();
            Resources = new ResourcePools(rules.EntityConstants);
            foreach (ModuleData module in modules)
            {
                Resources.GrantForModule(module);
            }

            Stats.OnStatsChanged += UpdateMaxHp;
        }

        /// <summary>Every hit or DoT tick that lands, is dodged or is blocked.</summary>
        public event Action<DamageEvent> OnDamaged;

        /// <summary>HP reached 0 and Defiance didn't proc.</summary>
        public event Action OnIncapacitated;

        /// <summary>Defiance saved the entity at 1 HP.</summary>
        public event Action OnDefianceProc;

        public Team Team { get; }

        /// <summary>Goon, Champion or Leader; null for heroes and civilians.</summary>
        public RankTier? RankTier { get; }

        public EntityStats Stats { get; }
        public Health Health { get; }
        public Defenses Defenses { get; }
        public MoveSpeed MoveSpeed { get; }
        public Defiance Defiance { get; }
        public Invulnerability Invulnerability { get; }
        public ResourcePools Resources { get; }

        /// <summary>A stand-in for Milestone 4's module slots.</summary>
        public IReadOnlyList<ModuleData> Modules => modules;

        public bool IsIncapacitated => Health.IsIncapacitated;
        public bool IsInvulnerable => Invulnerability.IsActive;

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

        public bool HasModuleTag(ModuleTag tag)
        {
            return moduleTags.Contains(tag);
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

        /// <summary>Hooks up the status system's cleanse. Without one, a Defiance proc cleanses nothing.</summary>
        public void SetDefianceCleanse(IDefianceCleanse cleanse)
        {
            defianceCleanse = cleanse;
        }

        /// <param name="realDelta">Unscaled seconds since the last tick.</param>
        public void Tick(GameClock clock, float realDelta)
        {
            Invulnerability.Tick(clock.GameDelta(realDelta, IsExemptFromTimePowers));
            Resources.Tick(clock.CooldownDelta(realDelta));
        }

        /// <returns>The HP actually restored. Healing doesn't revive an Incapacitated entity.</returns>
        public float Heal(float amount)
        {
            return Health.Heal(amount);
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
                if (defianceCleanse != null)
                {
                    defianceCleanse.CleanseForDefiance();
                }

                Invulnerability.Grant(Defiance.InvulnerableSeconds);
                OnDefianceProc?.Invoke();
                return;
            }

            Health.Lose(Health.Current);
            OnIncapacitated?.Invoke();
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
