using System;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// What makes a wall, door, window or prop damageable (world.md): HP, Armor and Damage Reduction, hit through the
    /// damage pipeline's structure rules plus the building multipliers. Not an entity: no stats, dodge, Defiance or
    /// statuses, and friendly fire doesn't protect it. A sibling component (<see cref="BuildingPart"/>,
    /// <see cref="PropBody"/>) sets its numbers and shows it broken.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Structure : MonoBehaviour, IDamageableStructure
    {
        [SerializeField] private ModuleWorld world;

        private Collider[] colliders = new Collider[0];
        private StructureHealth health;
        private float armor;
        private float damageReduction;
        private bool isBuildingPart;

        /// <summary>Raised whenever its HP changes.</summary>
        public event Action OnHealthChanged;

        /// <summary>Raised once, when its HP reaches 0.</summary>
        public event Action OnBroken;

        /// <summary>Null if the scene has none; nothing can damage it then.</summary>
        public ModuleWorld World => world;

        public bool IsConfigured => health != null;
        public bool IsBroken => health != null && health.IsBroken;
        public float Hp => health != null ? health.Current : 0f;
        public float MaxHp => health != null ? health.Max : 0f;
        public float Armor => armor;
        public float DamageReduction => damageReduction;

        /// <summary>Can't be damaged while true, such as while it's dragged (modules.md, Drag row).</summary>
        public bool IsProtected { get; set; }

        public bool CanBeHit => health != null && !health.IsBroken && !IsProtected;

        /// <summary>For structures placed in code, such as a map assembled at runtime: set before it's hit.</summary>
        public void SetWorld(ModuleWorld newWorld)
        {
            world = newWorld;
        }

        /// <summary>Sets its numbers and full HP. Its sibling component calls this once, when it wakes.</summary>
        /// <param name="isPartOfBuilding">A wall, door or window, which take the building multipliers.</param>
        public void Configure(float maxHp, float newArmor, float newDamageReduction, bool isPartOfBuilding)
        {
            if (health != null)
            {
                health.OnChanged -= RaiseHealthChanged;
                health.OnBroken -= RaiseBroken;
            }

            health = new StructureHealth(maxHp);
            health.OnChanged += RaiseHealthChanged;
            health.OnBroken += RaiseBroken;
            armor = newArmor;
            damageReduction = newDamageReduction;
            isBuildingPart = isPartOfBuilding;
        }

        public Vector3 ClosestPoint(Vector3 position)
        {
            Vector3 best = transform.position;
            float bestDistance = float.PositiveInfinity;
            foreach (Collider part in colliders)
            {
                if (part == null || !part.enabled)
                {
                    continue;
                }

                Vector3 point = part.ClosestPoint(position);
                float distance = (point - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            return best;
        }

        public void TakeHit(in StructureHit hit)
        {
            if (!CanBeHit || world == null || world.CombatRules.Pipeline == null)
            {
                return;
            }

            float amount = world.CombatRules.Pipeline.ApplyToStructure(hit.Hit, armor, damageReduction)
                * StructureDamageRules.Multiplier(hit.Kind, isBuildingPart, world.Database.WorldConstants);
            health.TakeDamage(amount);
        }

        /// <summary>Breaks it whatever its HP, such as a Small prop that's been thrown.</summary>
        public void Break()
        {
            if (health != null)
            {
                health.Break();
            }
        }

        private void Awake()
        {
            colliders = GetComponents<Collider>();
            if (world == null)
            {
                Debug.LogError($"{name}: {nameof(Structure)} needs the scene's Module World.", this);
            }
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke();
        }

        private void RaiseBroken()
        {
            OnBroken?.Invoke();
        }
    }
}
