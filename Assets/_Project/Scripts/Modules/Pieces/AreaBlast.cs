using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// An environmental blast, such as a vehicle's explosion, a Tow Saucer's crash or a Mothership Strike: the hit
    /// goes to every entity in the radius, of every side, citizens included, and to every wall, door, window and prop
    /// there. With no attacker, Armor and Damage Reduction apply and dodging doesn't.
    /// </summary>
    public static class AreaBlast
    {
        /// <param name="exempt">An entity the blast leaves out, such as the crashing saucer; null for none.</param>
        /// <param name="entities">Reused for the entities reached.</param>
        /// <param name="structures">Reused for the structures reached; the caller can read them afterwards.</param>
        public static void Detonate(ModuleWorld world, Vector3 origin, float radius, Hit blast, Entity exempt,
            List<EntityBehaviour> entities, List<IDamageableStructure> structures)
        {
            world.CollectEntities(origin, radius, entities);
            if (world.CombatRules.Pipeline != null)
            {
                foreach (EntityBehaviour target in entities)
                {
                    if (target.Entity != exempt)
                    {
                        world.CombatRules.Pipeline.Apply(blast, target.Entity);
                    }
                }
            }

            world.CollectStructures(origin, radius, structures);
            foreach (IDamageableStructure solid in structures)
            {
                solid.TakeHit(new StructureHit(blast, StructureHitKind.Direct));
            }
        }
    }
}
