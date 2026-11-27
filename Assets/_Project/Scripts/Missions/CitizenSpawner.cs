using UnityEngine;
using Zenthium.AI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>
    /// Spawns citizens from one body prefab (missions.md, Citizens): built inactive, given the scene's references
    /// and its kind and Valuables, then activated, as <see cref="FactionEntitySpawner"/> does for enemies.
    /// </summary>
    public sealed class CitizenSpawner : MonoBehaviour
    {
        [SerializeField] private ModuleWorld world;
        [SerializeField] private ValuableService valuables;

        [Tooltip("The citizen body: a civilian entity with a Citizen Brain and an AI Navigator.")]
        [SerializeField] private EntityBehaviour prefab;

        [Tooltip("The entity data citizens are built from. entities.csv has no citizen row yet, so the sandboxes' " +
            "stand-in is used.")]
        [SerializeField] private EntityData citizenEntity;

        private Transform holder;

        /// <returns>Null if it can't spawn one.</returns>
        public EntityBehaviour Spawn(Vector3 position, CitizenKind kind, int valuablesHeld)
        {
            if (prefab == null || world == null || citizenEntity == null)
            {
                Debug.LogError($"{name}: {nameof(CitizenSpawner)} needs a prefab, a Module World and an entity.", this);
                return null;
            }

            EntityBehaviour spawned = Instantiate(prefab, position, Quaternion.identity, Holder());
            spawned.name = $"Citizen ({kind})";
            spawned.ConfigureFactionEntity(world.Database, world.Clock, citizenEntity, Team.Civilian, 0);
            spawned.GetComponent<EntityModules>().SetWorld(world);
            if (spawned.TryGetComponent(out CitizenBrain brain))
            {
                brain.SetStartingKind(kind);
            }

            if (spawned.TryGetComponent(out ValuableCarrier carrier))
            {
                carrier.SetService(valuables);
                carrier.SetStartingValuables(valuablesHeld);
            }

            spawned.transform.SetParent(null, true);
            return spawned;
        }

        /// <summary>An inactive parent, so a spawned body doesn't wake until it's configured.</summary>
        private Transform Holder()
        {
            if (holder == null)
            {
                var holderObject = new GameObject("Spawn Holder");
                holderObject.SetActive(false);
                holder = holderObject.transform;
                holder.SetParent(transform, false);
            }

            return holder;
        }
    }
}
