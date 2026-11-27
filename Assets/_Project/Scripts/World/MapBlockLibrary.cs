using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// Every city block prefab the map builder can use. Zenthium > Map > Refresh Block Library fills it from the
    /// block folder, so it isn't edited by hand.
    /// </summary>
    public sealed class MapBlockLibrary : ScriptableObject
    {
        [SerializeField] private List<MapBlock> blocks = new List<MapBlock>();

        public IReadOnlyList<MapBlock> Blocks => blocks;

        /// <summary>For the editor command that refreshes it.</summary>
        public void SetBlocks(IEnumerable<MapBlock> newBlocks)
        {
            blocks.Clear();
            blocks.AddRange(newBlocks);
        }
    }
}
