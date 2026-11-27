using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// A citizen (missions.md, Citizens), on its game time:
    /// <list type="bullet">
    /// <item>Scared: flees straight away from the nearest enemy it can see within its fear range, and drops its
    /// Valuables the first time it starts to flee.</item>
    /// <item>Incapacitated: starts the mission down, with its Valuables dropped beside it. Revived (First Aid
    /// Spray), it gets up Scared.</item>
    /// <item>Stubborn: never moves, and slows whoever drags it. Charming can turn it Scared.</item>
    /// </list>
    /// Any citizen can be dragged, standing or not (<see cref="EntityDragTarget"/>); it doesn't move on its own
    /// while dragged.
    /// </summary>
    [RequireComponent(typeof(EntityModules), typeof(AiNavigator), typeof(EntityDragTarget))]
    public sealed class CitizenBrain : MonoBehaviour, ICharmable
    {
        private const int MaxSightHits = 16;

        [Tooltip("The kind of citizen it starts the mission as.")]
        [SerializeField] private CitizenKind startingKind = CitizenKind.Scared;

        [Tooltip("Game seconds between looks around for enemies.")]
        [SerializeField, Min(0.02f)] private float thinkSeconds = 0.2f;

        [Tooltip("Meters ahead it aims each time it picks where to run.")]
        [SerializeField, Min(1f)] private float fleeStep = 6f;

        private readonly List<EntityBehaviour> everyone = new List<EntityBehaviour>();
        private readonly RaycastHit[] sightHits = new RaycastHit[MaxSightHits];

        private EntityBehaviour self;
        private EntityModules modules;
        private AiNavigator navigator;
        private EntityDragTarget dragTarget;
        private ValuableCarrier carrier;
        private Entity entity;
        private AiConstants constants;
        private CitizenFear fear;
        private float untilThink;
        private bool hasStarted;

        public CitizenKind Kind { get; private set; }

        /// <summary>The kind it starts the mission as, for citizens spawned in code: set before the object activates.</summary>
        public void SetStartingKind(CitizenKind kind)
        {
            startingKind = kind;
        }

        public bool IsFleeing => fear != null && fear.IsFleeing;

        public bool TryGetCharmTarget(out CharmTarget target)
        {
            target = CharmTarget.StubbornCitizen;
            return entity != null && Kind == CitizenKind.Stubborn && !entity.IsIncapacitated;
        }

        /// <summary>Charming won a Stubborn citizen over: it turns Scared (modules.md).</summary>
        public void Charm(Entity charmer)
        {
            SetKind(CitizenKind.Scared);
        }

        private void Awake()
        {
            self = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
            navigator = GetComponent<AiNavigator>();
            dragTarget = GetComponent<EntityDragTarget>();
            TryGetComponent(out carrier);
            Kind = startingKind;
        }

        private void OnEnable()
        {
            // The entity may be created before or after this, and again on reset, as in AiBrain.
            self.OnEntityCreated += Bind;
            if (self.Entity != null)
            {
                Bind(self.Entity);
            }
        }

        private void OnDisable()
        {
            self.OnEntityCreated -= Bind;
            Unbind();
        }

        private void Start()
        {
            // After every component has bound to the entity, so the Valuable carrier hears it go down and drops
            // what it holds beside it.
            hasStarted = true;
            if (entity != null)
            {
                ApplyStartingKind();
            }
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            if (modules.World == null)
            {
                Debug.LogError($"{name}: {nameof(CitizenBrain)} needs its {nameof(EntityModules)} to have a Module World.", this);
                return;
            }

            entity = newEntity;
            constants = modules.World.Database.AiConstants;
            fear = new CitizenFear(constants.CitizenFearRange);
            entity.OnRevived += HandleRevived;
            untilThink = 0f;
            if (hasStarted)
            {
                ApplyStartingKind();
            }
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnRevived -= HandleRevived;
                entity = null;
            }

            navigator.Stop();
        }

        private void ApplyStartingKind()
        {
            SetKind(startingKind);
            if (startingKind == CitizenKind.Incapacitated)
            {
                entity.Incapacitate();
            }
        }

        private void Update()
        {
            if (entity == null || self.ClockDriver == null)
            {
                return;
            }

            if (entity.IsIncapacitated || entity.IsCaptured || entity.IsBeingDragged || !entity.Statuses.CanAct)
            {
                navigator.Stop();
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            untilThink -= self.ClockDriver.Clock.GameDelta(realDelta, entity.IsExemptFromTimePowers);
            if (untilThink > 0f)
            {
                return;
            }

            untilThink = thinkSeconds;
            float distance = NearestEnemy(out Vector3 enemyPosition);
            bool flees = fear.Update(Kind, distance, out bool dropsValuables);
            if (dropsValuables && carrier != null)
            {
                carrier.DropAll();
            }

            if (!flees)
            {
                navigator.Stop();
                return;
            }

            Vector3 away = transform.position - enemyPosition;
            away.y = 0f;
            Vector3 direction = away.sqrMagnitude > 0f ? away.normalized : transform.forward;
            navigator.MoveTo(transform.position + direction * fleeStep, 1f);
        }

        /// <returns>Meters to the nearest standing enemy it can see within its fear range; positive infinity if none.</returns>
        private float NearestEnemy(out Vector3 position)
        {
            position = transform.position;
            float nearest = float.PositiveInfinity;
            Vector3 eye = transform.position;
            modules.World.CollectAll(everyone);
            foreach (EntityBehaviour body in everyone)
            {
                Entity other = body.Entity;
                if (other == null || other.Team != Team.Enemy || other.IsIncapacitated || other.IsCaptured)
                {
                    continue;
                }

                Vector3 otherPosition = body.transform.position;
                float x = otherPosition.x - eye.x;
                float z = otherPosition.z - eye.z;
                float distance = Mathf.Sqrt(x * x + z * z);
                if (distance > constants.CitizenFearRange || distance >= nearest
                    || !modules.World.CanSee(eye, otherPosition, sightHits))
                {
                    continue;
                }

                nearest = distance;
                position = otherPosition;
            }

            return nearest;
        }

        private void HandleRevived()
        {
            // First Aid Spray gets a downed citizen up Scared (modules.md).
            SetKind(CitizenKind.Scared);
        }

        private void SetKind(CitizenKind kind)
        {
            Kind = kind;
            dragTarget.SetCarrierSlow(kind == CitizenKind.Stubborn ? constants.StubbornDragSlow : 0f);
        }
    }
}
