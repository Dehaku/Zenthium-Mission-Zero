using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// Spawns faction entities from one AI body prefab: it's built inactive, given the scene's references a prefab
    /// can't hold (clock, Module World, map edge, Valuable service), then activated. The entity's arrival tag
    /// (enemies.md, Arrival tags) isn't shown yet: every entity just appears (<see cref="ArrivalStub"/>).
    /// </summary>
    public sealed class FactionEntitySpawner : EntitySpawner
    {
        [SerializeField] private ModuleWorld world;
        [SerializeField] private MapBounds mapBounds;
        [SerializeField] private ValuableService valuables;

        [Tooltip("The AI body for spawned entities. Its entity, team, rank, modules and tags are set when it's spawned.")]
        [SerializeField] private EntityBehaviour prefab;

        private Transform holder;

        public override EntityBehaviour Spawn(EntityData entity, Team team, int difficultyLevel, Vector3 position)
        {
            if (prefab == null || world == null || entity == null)
            {
                Debug.LogError($"{name}: {nameof(FactionEntitySpawner)} needs a prefab, a Module World and an entity.",
                    this);
                return null;
            }

            EntityBehaviour spawned = Instantiate(prefab, position, Quaternion.identity, Holder());
            spawned.name = entity.Id;
            spawned.ConfigureFactionEntity(world.Database, world.Clock, entity, team, difficultyLevel);
            spawned.GetComponent<EntityModules>().SetWorld(world);
            if (spawned.TryGetComponent(out AiBrain brain))
            {
                brain.Configure(mapBounds);
            }

            if (spawned.TryGetComponent(out ValuableCarrier carrier))
            {
                carrier.SetService(valuables);
            }

            spawned.transform.SetParent(null, true);
            if (entity.Faction != null && entity.Faction.TryGetArrival(out ArrivalTag arrival))
            {
                ArrivalStub.Arrive(spawned, arrival);
            }

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
