using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// A Repo Agency crop circle (Factions/GalacticRepoAgency.md, Faction rules), and their loot destination
    /// (<c>LootDestination:CropCircle</c>). Each physics step, for every entity whose center is inside it:
    /// <list type="bullet">
    /// <item>it takes all its Valuables, which count as lost, the player's included;</item>
    /// <item>a citizen is abducted, dragged or not, and disappears for good. Heroes and enemies aren't.</item>
    /// </list>
    /// Its radius comes from the faction's data. The map builder places them from Milestone 8.
    /// </summary>
    public sealed class CropCircle : MonoBehaviour, ILootDestination
    {
        [SerializeField] private ModuleWorld world;

        [Tooltip("The faction whose crop circle this is, for its radius in factions.csv.")]
        [SerializeField] private FactionData faction;

        private readonly List<EntityBehaviour> inside = new List<EntityBehaviour>();
        private float radius;

        public EntityTag Tag => EntityTag.LootDestinationCropCircle;
        public Vector3 Position => transform.position;

        /// <summary>Meters; 0 until it's set up.</summary>
        public float Radius => radius;

        /// <summary>For a crop circle placed in code, such as by the map builder: call it before it wakes.</summary>
        public void Configure(ModuleWorld newWorld, FactionData newFaction)
        {
            world = newWorld;
            faction = newFaction;
        }

        public bool Contains(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        private void Awake()
        {
            if (world == null || !CropCircleRules.TryCreate(faction, out CropCircleRules rules, out _))
            {
                Debug.LogError($"{name}: {nameof(CropCircle)} needs a Module World and a faction with crop circle "
                    + "values in factions.csv.", this);
                enabled = false;
                return;
            }

            radius = rules.Radius;
        }

        private void OnEnable()
        {
            if (world != null)
            {
                world.RegisterLootDestination(this);
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.UnregisterLootDestination(this);
            }
        }

        private void FixedUpdate()
        {
            Vector3 center = transform.position;
            world.CollectEntities(center, radius, inside);
            foreach (EntityBehaviour body in inside)
            {
                Entity entity = body.Entity;
                if (entity == null || !Contains(body.transform.position))
                {
                    continue;
                }

                if (body.TryGetComponent(out ValuableCarrier carrier))
                {
                    carrier.LoseAll();
                }

                if (entity.Team == Team.Civilian && entity.Abduct())
                {
                    body.gameObject.SetActive(false);
                }
            }
        }
    }
}
