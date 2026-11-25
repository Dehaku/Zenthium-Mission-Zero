using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Statuses;

namespace Zenthium.World
{
    /// <summary>
    /// The root of a handmade building, which owns everything in it (world.md, Buildings): its interior zones,
    /// walls, windows and doors (each belongs to exactly one building; neighbors never share walls) and its
    /// sockets. It registers with the scene's <see cref="BuildingRegistry"/>, which gives it an ID and its neighbors.
    /// <para>It burns as a whole (world.md, Fire): an <c>Ignites</c> hit on any of its parts sets it on fire and an
    /// <c>Extinguish</c> hit puts it out. While it burns, each Burn tick gives everyone and every prop inside a Burn,
    /// each part break destroys one standing wall, window or door; its floor is blocked to enemy paths; and, if
    /// spreading is on, it may set unburned neighbors on fire. Burned out, the loose Valuables inside melt.</para>
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private LocationType location = LocationType.House;
        [SerializeField] private BuildingRegistry registry;

        private readonly List<Building> neighbors = new List<Building>();
        private readonly List<EntityBehaviour> zoneEntities = new List<EntityBehaviour>();
        private readonly HashSet<EntityBehaviour> burnedEntities = new HashSet<EntityBehaviour>();
        private readonly List<PropBody> zoneProps = new List<PropBody>();
        private readonly HashSet<PropBody> burnedProps = new HashSet<PropBody>();
        private readonly List<ValuableBag> zoneBags = new List<ValuableBag>();
        private readonly List<bool> standingParts = new List<bool>();
        private BuildingPart[] parts = new BuildingPart[0];
        private Structure[] partStructures = new Structure[0];
        private InteriorZone[] zones = new InteriorZone[0];
        private MapSocket[] sockets = new MapSocket[0];
        private BuildingFire fire;

        /// <summary>Raised when it catches fire, goes out or burns out.</summary>
        public event Action OnFireStateChanged;

        /// <summary>-1 until registered.</summary>
        public int Id { get; private set; } = -1;

        public LocationType Location => location;

        /// <summary>Its walls, windows and door panels.</summary>
        public IReadOnlyList<BuildingPart> Parts => parts;

        public IReadOnlyList<InteriorZone> Zones => zones;
        public IReadOnlyList<MapSocket> Sockets => sockets;

        /// <summary>Buildings within the neighbor radius, set when the registry works them out.</summary>
        public IReadOnlyList<Building> Neighbors => neighbors;

        public BuildingFireState FireState => fire != null ? fire.State : BuildingFireState.Unburned;

        /// <summary>Game seconds since it caught fire; 0 unless burning.</summary>
        public float SecondsBurning => fire != null ? fire.SecondsBurning : 0f;

        /// <summary>For buildings placed in code, such as a map assembled at runtime: set before it's enabled.</summary>
        public void SetRegistry(BuildingRegistry newRegistry)
        {
            registry = newRegistry;
        }

        /// <summary>Adds its interior zones' floor areas to the list.</summary>
        public void AddFootprint(List<Rect> results)
        {
            foreach (InteriorZone zone in zones)
            {
                results.Add(zone.Footprint);
            }
        }

        /// <returns>True if it caught fire: only an unburned building can.</returns>
        public bool Ignite()
        {
            if (!TryGetFire(out BuildingFire buildingFire) || !buildingFire.Ignite())
            {
                return false;
            }

            SetZonesBlockPaths(true);
            OnFireStateChanged?.Invoke();
            return true;
        }

        /// <returns>True if it was burning and is now out.</returns>
        public bool Extinguish()
        {
            if (fire == null || !fire.Extinguish())
            {
                return false;
            }

            SetZonesBlockPaths(false);
            OnFireStateChanged?.Invoke();
            return true;
        }

        internal void SetId(int id)
        {
            Id = id;
        }

        internal void SetNeighbors(List<Building> newNeighbors)
        {
            neighbors.Clear();
            neighbors.AddRange(newNeighbors);
        }

        private void Awake()
        {
            parts = GetComponentsInChildren<BuildingPart>(true);
            zones = GetComponentsInChildren<InteriorZone>(true);
            sockets = GetComponentsInChildren<MapSocket>(true);

            // Looked up here, not through the part, which may not have woken yet.
            partStructures = new Structure[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Building = this;
                partStructures[i] = parts[i].GetComponent<Structure>();
            }

            if (zones.Length == 0)
            {
                Debug.LogError($"{name}: a {nameof(Building)} needs at least one {nameof(InteriorZone)}.", this);
            }
        }

        private void OnEnable()
        {
            foreach (Structure part in partStructures)
            {
                part.OnHit += ReactToHit;
            }

            if (registry != null)
            {
                registry.Register(this);
            }
            else
            {
                Debug.LogError($"{name}: {nameof(Building)} needs the scene's {nameof(BuildingRegistry)}.", this);
            }
        }

        private void OnDisable()
        {
            foreach (Structure part in partStructures)
            {
                part.OnHit -= ReactToHit;
            }

            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        private void Update()
        {
            if (fire == null || fire.State != BuildingFireState.Burning || registry.World == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            FireStep step = fire.Advance(registry.World.Clock.Clock.GameDelta(realDelta, false));
            for (int i = 0; i < step.BurnTicks; i++)
            {
                BurnTick();
            }

            for (int i = 0; i < step.PartBreaks; i++)
            {
                BreakRandomPart();
            }

            for (int i = 0; i < step.SpreadRolls && registry.IsSpreadOn; i++)
            {
                RollSpread();
            }

            if (step.BurnedOut)
            {
                BurnOut();
            }
        }

        private bool TryGetFire(out BuildingFire buildingFire)
        {
            if (fire == null && registry != null && registry.World != null)
            {
                fire = new BuildingFire(registry.World.Database.FireConstants);
            }

            buildingFire = fire;
            return fire != null;
        }

        private void ReactToHit(StructureHit hit)
        {
            if (hit.Extinguishes)
            {
                Extinguish();
            }
            else if (hit.Ignites)
            {
                Ignite();
            }
        }

        /// <summary>Burn on everyone and every prop inside, once each.</summary>
        private void BurnTick()
        {
            FireConstants constants = registry.World.Database.FireConstants;
            var burn = new StatusApplication(StatusType.Burn, null, constants.BurnSeconds,
                damage: constants.BurnDamage, damageType: DamageType.Physical);
            burnedEntities.Clear();
            burnedProps.Clear();
            foreach (InteriorZone zone in zones)
            {
                zone.CollectEntities(zoneEntities);
                foreach (EntityBehaviour entity in zoneEntities)
                {
                    if (entity.Entity != null && burnedEntities.Add(entity))
                    {
                        entity.Entity.Statuses.TryApply(burn);
                    }
                }

                zone.CollectProps(zoneProps);
                foreach (PropBody prop in zoneProps)
                {
                    if (burnedProps.Add(prop) && prop.TryGetComponent(out PropBurning burning))
                    {
                        burning.Ignite(constants.BurnDamage, constants.BurnSeconds);
                    }
                }
            }
        }

        /// <summary>One standing wall, window or door destroyed, at random.</summary>
        private void BreakRandomPart()
        {
            standingParts.Clear();
            foreach (BuildingPart part in parts)
            {
                standingParts.Add(!part.IsBroken);
            }

            int index = FireTargets.PickStanding(standingParts, registry.World.Random.Value01());
            if (index >= 0)
            {
                partStructures[index].Break();
            }
        }

        private void RollSpread()
        {
            float chance = registry.World.Database.FireConstants.SpreadChance;
            foreach (Building neighbor in neighbors)
            {
                if (neighbor.FireState == BuildingFireState.Unburned && registry.World.Random.Value01() < chance)
                {
                    neighbor.Ignite();
                }
            }
        }

        private void BurnOut()
        {
            SetZonesBlockPaths(false);
            foreach (InteriorZone zone in zones)
            {
                zone.CollectBags(zoneBags);
                foreach (ValuableBag bag in zoneBags)
                {
                    bag.Melt();
                }
            }

            OnFireStateChanged?.Invoke();
        }

        private void SetZonesBlockPaths(bool blocks)
        {
            foreach (InteriorZone zone in zones)
            {
                zone.SetBlocksPaths(blocks);
            }
        }
    }
}
