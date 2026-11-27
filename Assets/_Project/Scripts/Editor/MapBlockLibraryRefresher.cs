using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Editor
{
    /// <summary>
    /// Zenthium > Map > Refresh Block Library: collects every <see cref="MapBlock"/> prefab under the block folder
    /// into the library the map builder uses, and warns about locations no block holds yet.
    /// </summary>
    internal static class MapBlockLibraryRefresher
    {
        private const string MenuPath = "Zenthium/Map/Refresh Block Library";
        internal const string BlocksFolder = PlayerSandboxSceneCreator.PrefabFolder + "/Blocks";
        internal const string LibraryPath = "Assets/_Project/Data/MapBlockLibrary.asset";
        private const string PrefabFilter = "t:Prefab";

        [MenuItem(MenuPath)]
        private static void RefreshFromMenu()
        {
            Refresh();
        }

        internal static MapBlockLibrary Refresh()
        {
            EnsureFolder(BlocksFolder);
            var blocks = new List<MapBlock>();
            foreach (string guid in AssetDatabase.FindAssets(PrefabFilter, new[] { BlocksFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.TryGetComponent(out MapBlock block))
                {
                    blocks.Add(block);
                }
            }

            var library = AssetDatabase.LoadAssetAtPath<MapBlockLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<MapBlockLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.SetBlocks(blocks);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LibraryPath} lists {blocks.Count} blocks from {BlocksFolder}.", library);
            WarnAboutMissingLocations(blocks);
            return library;
        }

        /// <summary>Folders under Assets, created one level at a time.</summary>
        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        /// <summary>A mission at a location no block holds can't build its map.</summary>
        private static void WarnAboutMissingLocations(List<MapBlock> blocks)
        {
            var held = new HashSet<LocationType>();
            var locations = new List<LocationType>();
            foreach (MapBlock block in blocks)
            {
                block.CollectLocations(locations);
                foreach (LocationType location in locations)
                {
                    if (LocationDistricts.Of(location) == block.District)
                    {
                        held.Add(location);
                    }
                }
            }

            var missing = new List<string>();
            foreach (LocationType location in (LocationType[])Enum.GetValues(typeof(LocationType)))
            {
                if (!held.Contains(location))
                {
                    missing.Add($"{location} ({LocationDistricts.Of(location)})");
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning("No block holds these locations yet, so their missions can't build a map: "
                    + string.Join(", ", missing) + ".");
            }
        }
    }
}
