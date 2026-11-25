using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;
using Zenthium.Statuses;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// One entity's AI (enemies.md, AI), for enemies, neutrals and the player's allies alike, on the entity's game
    /// time. It looks around a few times a second: who it can see, its awareness, its target and which module to
    /// use. Idle, it patrols with its squad (an ally follows the player). Aware with a target in view, it fights;
    /// with none, it searches the last known position, then forgets. Presentation reads its state and events.
    /// </summary>
    [RequireComponent(typeof(EntityModules), typeof(EntityMotor), typeof(AiNavigator))]
    public sealed class AiBrain : MonoBehaviour, IAwarenessReset
    {
        private const int MaxSightHits = 16;
        private static readonly SlotType[] ActivatedSlotTypes = { SlotType.Active, SlotType.Gear };

        [Tooltip("The squad it patrols with while Idle. Empty: it stands still, or an ally follows the player.")]
        [SerializeField] private AiSquad squad;

        [Tooltip("Game seconds between looks around: who it sees, its target and its module.")]
        [SerializeField, Min(0.02f)] private float thinkSeconds = 0.2f;

        [Tooltip("Meters around the last known position it wanders while searching.")]
        [SerializeField, Min(0f)] private float searchRadius = 6f;

        [Tooltip("It stops this fraction of its attack range away, so small moves don't take the target out of range.")]
        [SerializeField, Range(0.1f, 1f)] private float rangeMargin = 0.85f;

        [Tooltip("Meters inside the follow distance an ally walks to before it stops.")]
        [SerializeField, Min(0f)] private float followSlack = 3f;

        [Tooltip("Meters from a point that count as being there.")]
        [SerializeField, Min(0.1f)] private float arriveDistance = 1f;

        [Tooltip("Meters a ranged attacker steps away each time it backs off.")]
        [SerializeField, Min(0.5f)] private float backoffStep = 2f;

        private readonly List<EntityBehaviour> everyone = new List<EntityBehaviour>();
        private readonly List<EntityBehaviour> nearby = new List<EntityBehaviour>();
        private readonly List<TargetCandidate> visible = new List<TargetCandidate>();
        private readonly List<ModuleOption> options = new List<ModuleOption>();
        private readonly List<ModuleBehaviour> optionModules = new List<ModuleBehaviour>();
        private readonly List<SlotType> optionTypes = new List<SlotType>();
        private readonly List<int> optionIndexes = new List<int>();
        private readonly RaycastHit[] sightHits = new RaycastHit[MaxSightHits];

        private EntityModules modules;
        private EntityBehaviour self;
        private AiNavigator navigator;
        private Entity entity;
        private Awareness awareness;
        private AiConstants constants;
        private float visionRange;
        private float untilThink;
        private float sinceThink;
        private float fillRate;
        private Entity lastAttacker;
        private Vector3 lastKnownPosition;
        private bool hasLastKnown;
        private float searchLeft;
        private bool hasReachedSearchPoint;
        private Vector3 searchPoint;
        private Vector3 wanderPoint;
        private bool isFollowing;
        private float attackReach;
        private bool isRanged;
        private bool isTelegraphing;
        private float telegraphLeft;
        private SlotType telegraphType;
        private int telegraphIndex;
        private ModuleAim telegraphAim;

        /// <summary>It noticed someone, was hit or was alerted: the "!" and the alert wait start.</summary>
        public event Action OnNoticed;

        /// <summary>A Telegraph module started its wind-up (enemies.md: flash red before it hits).</summary>
        public event Action OnTelegraphStarted;

        public AiState State { get; private set; }

        /// <summary>Who it's attacking; null if no one.</summary>
        public EntityBehaviour Target { get; private set; }

        public AwarenessLevel AwarenessLevel => awareness != null ? awareness.Level : AwarenessLevel.Unaware;

        /// <summary>From 0 to 1.</summary>
        public float AwarenessBar => awareness != null ? awareness.Bar : 0f;

        /// <summary>Meters its alert reaches.</summary>
        public float AlertRadius => constants != null ? constants.AlertRadius : 0f;

        public bool IsTelegraphing => isTelegraphing;

        /// <summary>Idle and unaware on its feet: patrolling, so its squad waits for it.</summary>
        internal bool IsPatrolling => entity != null && !entity.IsIncapacitated && State == AiState.Idle
            && AwarenessLevel == AwarenessLevel.Unaware;

        /// <summary>Smoke Bomb: an unaware entity's bar empties (modules.md).</summary>
        public void ResetAwarenessBar()
        {
            if (awareness != null)
            {
                awareness.ResetBar();
            }
        }

        internal bool IsNear(Vector3 point, float distance)
        {
            return navigator.IsNear(point, distance);
        }

        private void Awake()
        {
            modules = GetComponent<EntityModules>();
            self = GetComponent<EntityBehaviour>();
            navigator = GetComponent<AiNavigator>();
        }

        private void OnEnable()
        {
            // The entity may be created before or after this, and again on reset, as in EntityModules.
            self.OnEntityCreated += Bind;
            if (self.Entity != null)
            {
                Bind(self.Entity);
            }

            if (squad != null)
            {
                squad.Join(this);
            }
        }

        private void OnDisable()
        {
            self.OnEntityCreated -= Bind;
            Unbind();
            if (squad != null)
            {
                squad.Leave(this);
            }
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            if (modules.World == null)
            {
                Debug.LogError($"{name}: {nameof(AiBrain)} needs its {nameof(EntityModules)} to have a Module World.", this);
                return;
            }

            GameDatabase database = modules.World.Database;
            entity = newEntity;
            constants = database.AiConstants;
            visionRange = database.FormulaConstants.VisionRange;
            awareness = new Awareness(database.Formulas, constants);
            entity.OnDamaged += HandleDamaged;
            State = AiState.Idle;
            untilThink = 0f;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnDamaged -= HandleDamaged;
            }

            entity = null;
            Target = null;
            lastAttacker = null;
            hasLastKnown = false;
            isTelegraphing = false;
            navigator.Stop();
        }

        private void Update()
        {
            if (entity == null || self.ClockDriver == null)
            {
                return;
            }

            if (entity.IsIncapacitated || entity.IsCaptured)
            {
                Fall();
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            float gameDelta = self.ClockDriver.Clock.GameDelta(realDelta, entity.IsExemptFromTimePowers);
            sinceThink += gameDelta;
            untilThink -= gameDelta;
            bool thinks = untilThink <= 0f;
            float elapsed = sinceThink;
            if (thinks)
            {
                untilThink = thinkSeconds;
                sinceThink = 0f;
                Look();
            }

            HandleAwarenessChange(awareness.Tick(gameDelta, fillRate));
            if (awareness.Level == AwarenessLevel.Noticing || !entity.Statuses.CanAct)
            {
                // Waiting after the "!", or stunned: no moving or attacking, and a wind-up is lost.
                isTelegraphing = false;
                navigator.Stop();
                return;
            }

            if (isTelegraphing)
            {
                TickTelegraph(gameDelta);
                return;
            }

            if (thinks)
            {
                Decide(elapsed);
            }
        }

        /// <summary>Defeated: an alert still waiting never goes out (enemies.md), and it forgets everything.</summary>
        private void Fall()
        {
            awareness.Reset();
            State = AiState.Idle;
            Target = null;
            lastAttacker = null;
            hasLastKnown = false;
            isTelegraphing = false;
            navigator.Stop();
        }

        /// <summary>Who it can see, and how fast its awareness bar fills from them.</summary>
        private void Look()
        {
            visible.Clear();
            fillRate = 0f;
            Team team = AiSides.EffectiveTeam(entity);
            bool sparesCivilians = entity.HasTag(EntityTag.SparesCivilians);
            Vector3 eye = transform.position;
            modules.World.CollectAll(everyone);
            foreach (EntityBehaviour body in everyone)
            {
                Entity other = body.Entity;
                if (other == null || other == entity || other.IsIncapacitated || other.IsCaptured
                    || !AiSides.IsHostile(team, other.Team, sparesCivilians))
                {
                    continue;
                }

                Vector3 position = body.transform.position;
                float distance = FlatDistance(eye, position);
                if (distance > visionRange || !modules.World.CanSee(eye, position, sightHits))
                {
                    continue;
                }

                visible.Add(new TargetCandidate(other, distance));
                if (AiSides.Notices(team, other.Team, sparesCivilians))
                {
                    fillRate = Mathf.Max(fillRate, awareness.FillRate(distance, other.Stats.Detectability));
                }
            }
        }

        private void HandleAwarenessChange(AwarenessChange change)
        {
            switch (change)
            {
                case AwarenessChange.Noticed:
                    StartNoticing();
                    break;
                case AwarenessChange.AlertSent:
                    SendAlert();
                    break;
                case AwarenessChange.Forgotten:
                    State = AiState.Idle;
                    Target = null;
                    lastAttacker = null;
                    hasLastKnown = false;
                    break;
            }
        }

        private void Decide(float elapsed)
        {
            Entity taunter = entity.Statuses.TryGetSource(StatusType.Taunted, out Entity source) ? source : null;
            if (awareness.Level == AwarenessLevel.Unaware)
            {
                if (taunter != null)
                {
                    NoticeNow();
                    return;
                }

                State = AiState.Idle;
                ActIdle();
                return;
            }

            if (lastAttacker != null && !IsVisible(lastAttacker))
            {
                lastAttacker = null;
            }

            Entity chosen = TargetSelector.Choose(AiSides.EffectiveTeam(entity),
                entity.HasTag(EntityTag.SparesCivilians), taunter, lastAttacker, visible);
            if (chosen != null && modules.World.TryGetBehaviour(chosen, out EntityBehaviour chosenBody))
            {
                State = AiState.Combat;
                Target = chosenBody;
                awareness.KeepAware();
                lastKnownPosition = chosenBody.transform.position;
                hasLastKnown = true;
                Fight(chosenBody);
                return;
            }

            Target = null;
            if (State != AiState.Searching)
            {
                BeginSearch();
            }

            Search(elapsed);
        }

        private void ActIdle()
        {
            if (entity.HasTag(EntityTag.Immobile))
            {
                navigator.Stop();
                return;
            }

            if ((entity.Team == Team.Ally) && TryFindPlayer(out EntityBehaviour player))
            {
                Follow(player);
                return;
            }

            if (squad != null)
            {
                navigator.MoveTo(squad.PatrolPoint(this), constants.PatrolSpeedFraction);
                return;
            }

            navigator.Stop();
        }

        /// <summary>Allies outside combat stay within about the follow distance of the player (enemies.md).</summary>
        private void Follow(EntityBehaviour player)
        {
            float distance = FlatDistance(transform.position, player.transform.position);
            if (distance > constants.AllyFollowDistance)
            {
                isFollowing = true;
            }
            else if (distance <= constants.AllyFollowDistance - followSlack)
            {
                isFollowing = false;
            }

            if (isFollowing)
            {
                navigator.MoveTo(player.transform.position, 1f);
            }
            else
            {
                navigator.Stop();
            }
        }

        /// <summary>
        /// Melee: chase until in reach. Ranged: close in until in range, stop, and back off a little from targets
        /// closer than the back-off distance (enemies.md, Movement). A locked door in the way is attacked first.
        /// </summary>
        private void Fight(EntityBehaviour targetBody)
        {
            Vector3 toTarget = targetBody.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            var aim = new ModuleAim(toTarget, distance);
            modules.SetAim(aim);
            BuildOptions();

            if (navigator.BlockingDoor != null && TryAttackDoor(navigator.BlockingDoor))
            {
                return;
            }

            if (TryUseModule(aim, distance) && isTelegraphing)
            {
                return;
            }

            if (entity.HasTag(EntityTag.Immobile))
            {
                navigator.Stop();
                return;
            }

            float stopDistance = (float.IsPositiveInfinity(attackReach) || attackReach <= 0f
                ? arriveDistance
                : attackReach) * rangeMargin;
            if (distance > stopDistance)
            {
                navigator.MoveTo(targetBody.transform.position, 1f);
            }
            else if (isRanged && distance < constants.RangedBackoffDistance && distance > 0f)
            {
                navigator.MoveTo(transform.position - toTarget / distance * backoffStep, 1f);
            }
            else
            {
                navigator.Stop();
            }
        }

        private bool TryAttackDoor(Door door)
        {
            Vector3 doorPoint = door.Panel.transform.position;
            Vector3 toDoor = doorPoint - transform.position;
            toDoor.y = 0f;
            var aim = new ModuleAim(toDoor, toDoor.magnitude);
            modules.SetAim(aim);
            return TryUseModule(aim, toDoor.magnitude);
        }

        /// <summary>
        /// Uses the first module in list order that's ready with the target in reach (enemies.md, Using modules).
        /// One that still refuses, for a reason of its own, is skipped for the next. A Telegraph module winds up
        /// first.
        /// </summary>
        private bool TryUseModule(ModuleAim aim, float distance)
        {
            for (int attempt = 0; attempt < options.Count; attempt++)
            {
                int i = ModuleChooser.FirstUsable(options, distance);
                if (i < 0)
                {
                    return false;
                }

                if (optionModules[i].Data.HasTag(ModuleTag.Telegraph))
                {
                    BeginTelegraph(optionTypes[i], optionIndexes[i], aim);
                    return true;
                }

                if (modules.TryActivateSlot(optionTypes[i], optionIndexes[i], aim))
                {
                    return true;
                }

                options[i] = new ModuleOption(false, options[i].Reach);
            }

            return false;
        }

        /// <summary>
        /// Its activated modules in list order (actives, then gear), with whether each is ready now. Also works out
        /// its attack range: the longest reach among its Melee and Ranged modules, and whether it fights at range.
        /// </summary>
        private void BuildOptions()
        {
            options.Clear();
            optionModules.Clear();
            optionTypes.Clear();
            optionIndexes.Clear();
            attackReach = 0f;
            isRanged = false;
            float anyReach = 0f;
            foreach (SlotType type in ActivatedSlotTypes)
            {
                for (int index = 0; index < entity.Slots.SlotsPerType; index++)
                {
                    ModuleBehaviour module = modules.GetSlotted(type, index);
                    if (module == null || module.Activation == null || !module.IsEnabled)
                    {
                        continue;
                    }

                    bool ready = module.Activation.Check(entity, modules.IsSlotUsable(type, index))
                        == ActivationBlock.None;
                    float reach = ModuleChooser.Reach(module.Data);
                    options.Add(new ModuleOption(ready, reach));
                    optionModules.Add(module);
                    optionTypes.Add(type);
                    optionIndexes.Add(index);

                    if (float.IsPositiveInfinity(reach))
                    {
                        continue;
                    }

                    anyReach = Mathf.Max(anyReach, reach);
                    bool ranged = module.Data.HasTag(ModuleTag.Ranged);
                    if (ranged || module.Data.HasTag(ModuleTag.Melee))
                    {
                        attackReach = Mathf.Max(attackReach, reach);
                        isRanged |= ranged;
                    }
                }
            }

            if (attackReach <= 0f)
            {
                attackReach = anyReach;
            }
        }

        private void BeginTelegraph(SlotType type, int index, ModuleAim aim)
        {
            isTelegraphing = true;
            telegraphLeft = constants.TelegraphSeconds;
            telegraphType = type;
            telegraphIndex = index;
            telegraphAim = aim;
            navigator.Stop();
            OnTelegraphStarted?.Invoke();
        }

        /// <summary>The aim is kept from the wind-up's start, so a target that moves can dodge it.</summary>
        private void TickTelegraph(float gameDelta)
        {
            telegraphLeft -= gameDelta;
            if (telegraphLeft > 0f)
            {
                return;
            }

            isTelegraphing = false;
            modules.TryActivateSlot(telegraphType, telegraphIndex, telegraphAim);
        }

        private void BeginSearch()
        {
            State = AiState.Searching;
            searchLeft = constants.SearchSeconds;
            hasReachedSearchPoint = false;
            searchPoint = hasLastKnown ? lastKnownPosition : transform.position;
            wanderPoint = searchPoint;
        }

        /// <summary>
        /// Goes to the last known position and wanders around it until the search time is up, then forgets
        /// (enemies.md, Losing the target). The time counts from the start, so an unreachable spot can't stall it.
        /// </summary>
        private void Search(float elapsed)
        {
            if (awareness.IsForgetting || entity.HasTag(EntityTag.Immobile))
            {
                navigator.Stop();
                if (!awareness.IsForgetting)
                {
                    awareness.BeginForgetting();
                }

                return;
            }

            searchLeft -= elapsed;
            if (searchLeft <= 0f)
            {
                navigator.Stop();
                awareness.BeginForgetting();
                return;
            }

            // Out of sight of its target, it still breaks a locked door on its way to the last known position.
            if (navigator.BlockingDoor != null)
            {
                BuildOptions();
                TryAttackDoor(navigator.BlockingDoor);
            }

            if (!hasReachedSearchPoint)
            {
                if (!navigator.IsNear(searchPoint, arriveDistance))
                {
                    navigator.MoveTo(searchPoint, 1f);
                    return;
                }

                hasReachedSearchPoint = true;
            }

            if (navigator.HasArrived || navigator.IsNear(wanderPoint, arriveDistance))
            {
                wanderPoint = RandomPointAround(searchPoint);
            }

            navigator.MoveTo(wanderPoint, constants.PatrolSpeedFraction);
        }

        private void HandleDamaged(DamageEvent damage)
        {
            Entity source = damage.Source;
            if (source == null || source == entity || entity.IsIncapacitated
                || !AiSides.IsHostile(AiSides.EffectiveTeam(entity), source.Team,
                    entity.HasTag(EntityTag.SparesCivilians)))
            {
                return;
            }

            // An entity that just took damage turns on whoever hit it (enemies.md, Targets).
            lastAttacker = source;
            if (modules.World.TryGetBehaviour(source, out EntityBehaviour attacker))
            {
                lastKnownPosition = attacker.transform.position;
                hasLastKnown = true;
            }

            NoticeNow();
        }

        /// <summary>Hit, alerted or Taunted: aware at once, through the usual wait.</summary>
        private void NoticeNow()
        {
            if (awareness.Notice())
            {
                StartNoticing();
            }
            else if (State == AiState.Searching)
            {
                State = AiState.Combat;
            }
        }

        private void StartNoticing()
        {
            State = AiState.Combat;
            isTelegraphing = false;
            navigator.Stop();
            OnNoticed?.Invoke();
        }

        /// <summary>The wait is over: unaware allies on its team within the alert radius notice too.</summary>
        private void SendAlert()
        {
            modules.World.CollectEntities(transform.position, constants.AlertRadius, nearby);
            foreach (EntityBehaviour body in nearby)
            {
                if (body == self || !body.TryGetComponent(out AiBrain other) || other.awareness == null)
                {
                    continue;
                }

                float distance = FlatDistance(transform.position, body.transform.position);
                if (AlertRules.Wakes(entity, body.Entity, distance, constants.AlertRadius, other.AwarenessLevel))
                {
                    other.ReceiveAlert(lastKnownPosition, hasLastKnown);
                }
            }
        }

        /// <param name="threat">Where the alerter last saw its target, so this one searches there.</param>
        private void ReceiveAlert(Vector3 threat, bool hasThreat)
        {
            if (hasThreat)
            {
                lastKnownPosition = threat;
                hasLastKnown = true;
            }

            NoticeNow();
        }

        private bool IsVisible(Entity other)
        {
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i].Entity == other)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindPlayer(out EntityBehaviour player)
        {
            foreach (EntityBehaviour body in everyone)
            {
                if (body.Entity != null && body.Entity.Team == Team.Player)
                {
                    player = body;
                    return true;
                }
            }

            player = null;
            return false;
        }

        private Vector3 RandomPointAround(Vector3 center)
        {
            float angle = modules.World.Random.Value01() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(modules.World.Random.Value01()) * searchRadius;
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
