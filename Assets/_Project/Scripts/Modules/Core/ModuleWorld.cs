using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;
using Zenthium.Timing;

namespace Zenthium.Modules
{
    /// <summary>How an extracted entity counts (missions.md): a citizen is rescued, an enemy captured.</summary>
    public enum ExtractionKind
    {
        Rescued,
        Captured,
    }

    /// <summary>
    /// The scene services module behaviors use: game data, combat rules, the clock, entity and structure queries,
    /// spawning projectiles, placed objects and summons, shields, and taking entities off the map (Evac Balloon).
    /// One per scene; entities reference it.
    /// </summary>
    public sealed class ModuleWorld : MonoBehaviour
    {
        private const int MaxOverlapHits = 64;

        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;
        [SerializeField] private GameClockDriver clock;
        [SerializeField] private Projectile projectilePrefab;

        [Tooltip("The body for summons (Ink Beast). Its entity, team and modules are set when it's spawned.")]
        [SerializeField] private EntityBehaviour summonPrefab;

        [Tooltip("Spawns faction entities with their AI (Call for Backup). Empty: modules that need it do nothing.")]
        [SerializeField] private EntitySpawner entitySpawner;

        [SerializeField] private PlacedGlyph glyphPrefab;
        [SerializeField] private SmokeCloud smokePrefab;
        [SerializeField] private TimedCharge chargePrefab;
        [SerializeField] private ShieldZone shieldPrefab;

        [Tooltip("A thin bar shown briefly along a beam (Repulsor Blast). Presentation only.")]
        [SerializeField] private GameObject beamVisualPrefab;

        [Tooltip("Seconds a beam's bar stays visible.")]
        [SerializeField, Min(0.01f)] private float beamVisualSeconds = 0.15f;

        [Tooltip("Meters per second an extracted entity floats up before it leaves the map.")]
        [SerializeField, Min(0.1f)] private float extractionRiseSpeed = 3f;

        [Tooltip("Seconds an extracted entity floats before it leaves the map.")]
        [SerializeField, Min(0f)] private float extractionSeconds = 1.5f;

        [Tooltip("The orthographic overhead camera can't show height, so an extracted body grows to this multiple of its "
            + "size as it rises toward the camera.")]
        [SerializeField, Range(1f, 5f)] private float extractionEndScale = 2f;

        private readonly Collider[] overlapHits = new Collider[MaxOverlapHits];
        private readonly IRandomSource random = new UnityRandomSource();
        private readonly Dictionary<Entity, EntityBehaviour> behaviours = new Dictionary<Entity, EntityBehaviour>();
        private readonly List<ShieldZone> shields = new List<ShieldZone>();
        private readonly List<SmokeCloud> smokes = new List<SmokeCloud>();
        private readonly List<ILootDestination> lootDestinations = new List<ILootDestination>();
        private Transform spawnHolder;

        /// <summary>
        /// An entity was rescued or captured, floated away or captured where it lies. Milestone 9's mission goals
        /// count these; until then nothing listens.
        /// </summary>
        public event Action<EntityBehaviour, ExtractionKind> OnRescuedOrCaptured;

        public GameDatabase Database => database;
        public CombatRulesBehaviour CombatRules => combatRules;
        public GameClockDriver Clock => clock;
        public IRandomSource Random => random;

        /// <summary>Fills the list with each entity whose collider is within the radius, once each.</summary>
        public void CollectEntities(Vector3 center, float radius, List<EntityBehaviour> results)
        {
            results.Clear();
            int count = Physics.OverlapSphereNonAlloc(
                center, radius, overlapHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (TryGetEntity(overlapHits[i], out EntityBehaviour entity) && entity.Entity != null
                    && !results.Contains(entity))
                {
                    results.Add(entity);
                }
            }
        }

        /// <summary>Fills the list with each wall, door, window or prop within the radius that can be hit, once each.</summary>
        public void CollectStructures(Vector3 center, float radius, List<IDamageableStructure> results)
        {
            results.Clear();
            int count = Physics.OverlapSphereNonAlloc(
                center, radius, overlapHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (TryGetStructure(overlapHits[i], out IDamageableStructure structure) && structure.CanBeHit
                    && !results.Contains(structure))
                {
                    results.Add(structure);
                }
            }
        }

        /// <summary>The scene object of an entity whose modules are running, such as an attacker to knock back.</summary>
        public bool TryGetBehaviour(Entity entity, out EntityBehaviour behaviour)
        {
            behaviour = null;
            return entity != null && behaviours.TryGetValue(entity, out behaviour) && behaviour != null;
        }

        /// <returns>Null if the scene has no prefab for it. The same for the other Spawn methods.</returns>
        public Projectile SpawnProjectile(Vector3 position, Vector3 direction)
        {
            return Spawn(projectilePrefab, position, Quaternion.LookRotation(direction, Vector3.up));
        }

        public PlacedGlyph SpawnGlyph(Vector3 position)
        {
            return Spawn(glyphPrefab, position, Quaternion.identity);
        }

        public SmokeCloud SpawnSmoke(Vector3 position)
        {
            return Spawn(smokePrefab, position, Quaternion.identity);
        }

        public TimedCharge SpawnCharge(Vector3 position)
        {
            return Spawn(chargePrefab, position, Quaternion.identity);
        }

        public ShieldZone SpawnShield(Vector3 position)
        {
            return Spawn(shieldPrefab, position, Quaternion.identity);
        }

        /// <summary>
        /// Spawns a summon on the summoner's side: a player's summons are allies. It's built inactive, configured,
        /// then activated, since a prefab can't hold the scene's references.
        /// </summary>
        public EntityBehaviour SpawnSummon(EntityData entity, Entity summoner, IReadOnlyList<ModuleData> modules,
            Vector3 position)
        {
            if (summonPrefab == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleWorld)} has no summon prefab.", this);
                return null;
            }

            EntityBehaviour summon = Instantiate(summonPrefab, position, Quaternion.identity, spawnHolder);
            Team team = summoner.Team == Team.Player ? Team.Ally : summoner.Team;
            summon.ConfigureSummon(database, clock, entity, team, summoner, modules);
            summon.GetComponent<EntityModules>().SetWorld(this);
            summon.transform.SetParent(null, true);
            return summon;
        }

        /// <summary>Spawns a faction entity on a team, such as backup (<see cref="EntitySpawner"/>).</summary>
        /// <returns>Null if the scene has no spawner, or it couldn't spawn one.</returns>
        public EntityBehaviour SpawnFactionEntity(EntityData entity, Team team, int difficultyLevel, Vector3 position)
        {
            if (entitySpawner == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleWorld)} has no {nameof(EntitySpawner)}.", this);
                return null;
            }

            return entitySpawner.Spawn(entity, team, difficultyLevel, position);
        }

        /// <summary>Shows a beam's path briefly (presentation only).</summary>
        public void ShowBeam(Vector3 from, Vector3 to)
        {
            Vector3 path = to - from;
            if (beamVisualPrefab == null || path.sqrMagnitude <= 0f)
            {
                return;
            }

            GameObject beam = Instantiate(beamVisualPrefab, (from + to) * 0.5f, Quaternion.LookRotation(path, Vector3.up));
            Vector3 scale = beam.transform.localScale;
            beam.transform.localScale = new Vector3(scale.x, scale.y, path.magnitude);
            Destroy(beam, beamVisualSeconds);
        }

        /// <summary>Whether an active shield stops a projectile from this shooter at this point.</summary>
        public bool IsBlockedByShield(Vector3 point, Entity shooter)
        {
            foreach (ShieldZone shield in shields)
            {
                if (shield != null && shield.Blocks(point, shooter))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether one point can see another: <see cref="LineOfSight"/>, and no smoke cloud between them
        /// (modules.md, Smoke Bomb: the cloud blocks line of sight).
        /// </summary>
        /// <param name="buffer">Reused for the raycast, so a check allocates nothing.</param>
        public bool CanSee(Vector3 from, Vector3 to, RaycastHit[] buffer)
        {
            foreach (SmokeCloud smoke in smokes)
            {
                if (smoke != null && smoke.BlocksSight(from, to))
                {
                    return false;
                }
            }

            return LineOfSight.IsClear(from, to, buffer);
        }

        /// <summary>Fills the list with every entity whose modules are running, anywhere on the map.</summary>
        public void CollectAll(List<EntityBehaviour> results)
        {
            results.Clear();
            foreach (KeyValuePair<Entity, EntityBehaviour> pair in behaviours)
            {
                if (pair.Value != null)
                {
                    results.Add(pair.Value);
                }
            }
        }

        /// <summary>The nearest loot destination for the tag, measured on the XZ plane.</summary>
        /// <returns>False if the map has none for it.</returns>
        public bool TryFindLootDestination(EntityTag tag, Vector3 from, out ILootDestination nearest)
        {
            nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (ILootDestination destination in lootDestinations)
            {
                if (destination.Tag != tag)
                {
                    continue;
                }

                Vector3 offset = destination.Position - from;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = destination;
                    nearestDistance = distance;
                }
            }

            return nearest != null;
        }

        public void RegisterLootDestination(ILootDestination destination)
        {
            if (!lootDestinations.Contains(destination))
            {
                lootDestinations.Add(destination);
            }
        }

        public void UnregisterLootDestination(ILootDestination destination)
        {
            lootDestinations.Remove(destination);
        }

        internal void Register(Entity entity, EntityBehaviour behaviour)
        {
            behaviours[entity] = behaviour;
        }

        internal void Unregister(Entity entity)
        {
            behaviours.Remove(entity);
        }

        internal void RegisterShield(ShieldZone shield)
        {
            shields.Add(shield);
        }

        internal void UnregisterShield(ShieldZone shield)
        {
            shields.Remove(shield);
        }

        internal void RegisterSmoke(SmokeCloud smoke)
        {
            smokes.Add(smoke);
        }

        internal void UnregisterSmoke(SmokeCloud smoke)
        {
            smokes.Remove(smoke);
        }

        private T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleWorld)} has no {typeof(T).Name} prefab.", this);
                return null;
            }

            return Instantiate(prefab, position, rotation);
        }

        /// <summary>Floats the entity away and takes it off the map, counting it as rescued or captured.</summary>
        public void Extract(EntityBehaviour target, ExtractionKind kind)
        {
            if (target == null || !target.isActiveAndEnabled)
            {
                return;
            }

            if (target.TryGetComponent(out EntityDragTarget dragTarget))
            {
                // Disabling the drag target makes any carrier let go.
                dragTarget.enabled = false;
            }

            if (target.TryGetComponent(out EntityMotor motor))
            {
                motor.enabled = false;
            }

            if (target.TryGetComponent(out Rigidbody body))
            {
                body.isKinematic = true;
            }

            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            OnRescuedOrCaptured?.Invoke(target, kind);
            StartCoroutine(FloatAway(target.gameObject));
        }

        /// <summary>Captures an Incapacitated entity where it lies (Stasis Cuffs); it stays on the map.</summary>
        public void Capture(EntityBehaviour target)
        {
            if (target != null && target.Entity != null && target.Entity.Capture())
            {
                OnRescuedOrCaptured?.Invoke(target, ExtractionKind.Captured);
            }
        }

        /// <summary>Finds the entity a collider belongs to, through its rigidbody if it has one.</summary>
        public static bool TryGetEntity(Collider hit, out EntityBehaviour entity)
        {
            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out entity);
            }

            return hit.TryGetComponent(out entity);
        }

        /// <summary>Finds the wall, door, window or prop a collider belongs to, through its rigidbody if it has one.</summary>
        public static bool TryGetStructure(Collider hit, out IDamageableStructure structure)
        {
            structure = null;
            if (hit == null)
            {
                return false;
            }

            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out structure);
            }

            return hit.TryGetComponent(out structure);
        }

        private void Awake()
        {
            // Spawned under an inactive parent, a summon doesn't wake until it's configured.
            var holder = new GameObject("Spawn Holder");
            holder.SetActive(false);
            spawnHolder = holder.transform;
            spawnHolder.SetParent(transform, false);

            if (database == null || combatRules == null || clock == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleWorld)} needs a Game Database, Combat Rules and a Game Clock Driver.", this);
                enabled = false;
            }
        }

        private IEnumerator FloatAway(GameObject target)
        {
            float elapsed = 0f;
            Vector3 startScale = target.transform.localScale;
            while (elapsed < extractionSeconds && target != null)
            {
                target.transform.position += Vector3.up * (extractionRiseSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                float progress = extractionSeconds > 0f ? Mathf.Clamp01(elapsed / extractionSeconds) : 1f;
                target.transform.localScale = startScale * Mathf.Lerp(1f, extractionEndScale, progress);
                yield return null;
            }

            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }
}
