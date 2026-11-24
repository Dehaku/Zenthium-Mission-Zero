using System;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// What a module's behavior works with: the entity using it, its body in the scene and the scene's services.
    /// One per entity, shared by all its modules.
    /// </summary>
    public sealed class ModuleContext
    {
        public ModuleContext(EntityBehaviour ownerBehaviour, Entity owner, ModuleWorld world, Rigidbody body,
            EntityMotor motor, float meleeArcHalfAngle)
        {
            OwnerBehaviour = ownerBehaviour;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            World = world;
            Body = body;
            Motor = motor;
            MeleeArcHalfAngle = meleeArcHalfAngle;
        }

        public EntityBehaviour OwnerBehaviour { get; }
        public Entity Owner { get; }
        public ModuleWorld World { get; }
        public Rigidbody Body { get; }

        /// <summary>Null if the entity can't move by itself.</summary>
        public EntityMotor Motor { get; }

        /// <summary>Degrees either side of the aim that melee modules reach (a feel value).</summary>
        public float MeleeArcHalfAngle { get; }

        public Vector3 Position => Body.position;
        public IRandomSource Random => World.Random;
    }
}
