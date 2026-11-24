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
    /// The scene services module behaviors use: game data, combat rules, the clock, entity queries, spawning
    /// projectiles, and taking entities off the map (Evac Balloon). One per scene; entities reference it.
    /// </summary>
    public sealed class ModuleWorld : MonoBehaviour
    {
        private const int MaxOverlapHits = 64;

        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;
        [SerializeField] private GameClockDriver clock;
        [SerializeField] private Projectile projectilePrefab;

        [Tooltip("Meters per second an extracted entity floats up before it leaves the map.")]
        [SerializeField, Min(0.1f)] private float extractionRiseSpeed = 3f;

        [Tooltip("Seconds an extracted entity floats before it leaves the map.")]
        [SerializeField, Min(0f)] private float extractionSeconds = 1.5f;

        [Tooltip("The orthographic overhead camera can't show height, so an extracted body grows to this multiple of its "
            + "size as it rises toward the camera.")]
        [SerializeField, Range(1f, 5f)] private float extractionEndScale = 2f;

        private readonly Collider[] overlapHits = new Collider[MaxOverlapHits];
        private readonly IRandomSource random = new UnityRandomSource();

        /// <summary>
        /// An entity was rescued or captured. Milestone 9's mission goals count these; until then nothing listens.
        /// </summary>
        public event Action<EntityBehaviour, ExtractionKind> OnExtracted;

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

        /// <returns>Null if the scene has no projectile prefab set.</returns>
        public Projectile SpawnProjectile(Vector3 position, Vector3 direction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleWorld)} has no projectile prefab.", this);
                return null;
            }

            return Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction, Vector3.up));
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

            OnExtracted?.Invoke(target, kind);
            StartCoroutine(FloatAway(target.gameObject));
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

        private void Awake()
        {
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
