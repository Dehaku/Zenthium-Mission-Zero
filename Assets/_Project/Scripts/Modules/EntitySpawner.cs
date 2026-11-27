using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Spawns a faction entity ready to act, its AI included, such as Call for Backup's goons. The AI assembly
    /// implements it, so Modules never references the AI. <see cref="ModuleWorld"/> holds the scene's one.
    /// </summary>
    public abstract class EntitySpawner : MonoBehaviour
    {
        /// <param name="difficultyLevel">Mission ranks above D (0 at D), for its rank scaling and locked modules.</param>
        /// <returns>Null if it can't spawn one.</returns>
        public abstract EntityBehaviour Spawn(EntityData entity, Team team, int difficultyLevel, Vector3 position);
    }
}
