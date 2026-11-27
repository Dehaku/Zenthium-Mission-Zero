using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// The scene's Valuables (world.md, Valuables): spawns bags when carriers drop or launch them, lists the loose
    /// bags for the AI to loot, and keeps the lost count for the Valuables secondary goal (Milestone 9). One per scene.
    /// </summary>
    public sealed class ValuableService : MonoBehaviour
    {
        private const float FullCircleDegrees = 360f;

        [SerializeField] private ModuleWorld world;
        [SerializeField] private ValuableBag bagPrefab;

        [Tooltip("Meters around a defeated carrier that its Valuables scatter within.")]
        [SerializeField, Min(0f)] private float dropRadius = 1.5f;

        [Tooltip("A launched bag's distance varies by up to this share either way, at random, so they scatter.")]
        [SerializeField, Range(0f, 1f)] private float launchDistanceSpread = 0.25f;

        [Tooltip("Degrees either side of the aim a launched bag can veer, at random.")]
        [SerializeField, Range(0f, 45f)] private float launchAngleSpread = 15f;

        private readonly List<ValuableBag> bags = new List<ValuableBag>();

        /// <summary>Raised when a Valuable is lost.</summary>
        public event Action OnLostChanged;

        public ModuleWorld World => world;

        /// <summary>Valuables lost so far: melted, or carried off the map.</summary>
        public int LostCount { get; private set; }

        /// <summary>The loose bags in the scene, flying ones included.</summary>
        public IReadOnlyList<ValuableBag> Bags => bags;

        /// <returns>Null if the scene has no bag prefab.</returns>
        public ValuableBag Spawn(Vector3 position)
        {
            if (bagPrefab == null)
            {
                Debug.LogError($"{name}: {nameof(ValuableService)} has no bag prefab.", this);
                return null;
            }

            ValuableBag bag = Instantiate(bagPrefab, position, Quaternion.identity);
            bag.SetService(this);
            return bag;
        }

        /// <summary>Scatters bags at random within the drop radius around a point.</summary>
        public void DropAround(Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = world.Random.Value01() * FullCircleDegrees;
                float distance = Mathf.Sqrt(world.Random.Value01()) * dropRadius;
                Spawn(center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance);
            }
        }

        /// <summary>Launches bags along a throw, each with a small random spread in distance and direction.</summary>
        public void Launch(in ThrowInfo info, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float veer = (world.Random.Value01() * 2f - 1f) * launchAngleSpread;
                float distance = info.Distance * (1f + (world.Random.Value01() * 2f - 1f) * launchDistanceSpread);
                ValuableBag bag = Spawn(info.Origin);
                if (bag != null)
                {
                    Vector3 direction = Quaternion.Euler(0f, veer, 0f) * info.Direction;
                    bag.Launch(info.Thrower, direction, distance, info.KickDamage);
                }
            }
        }

        internal void Register(ValuableBag bag)
        {
            if (!bags.Contains(bag))
            {
                bags.Add(bag);
            }
        }

        internal void Unregister(ValuableBag bag)
        {
            bags.Remove(bag);
        }

        public void CountLost(int count)
        {
            if (count > 0)
            {
                LostCount += count;
                OnLostChanged?.Invoke();
            }
        }

        private void Awake()
        {
            if (world == null || bagPrefab == null)
            {
                Debug.LogError($"{name}: {nameof(ValuableService)} needs the Module World and a bag prefab.", this);
            }
        }
    }
}
