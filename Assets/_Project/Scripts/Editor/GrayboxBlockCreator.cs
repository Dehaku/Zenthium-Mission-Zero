using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Editor.DataImport;
using Zenthium.World;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Zenthium > Map > Create Graybox Blocks: one placeholder block prefab per location in maps.md, except SWIFT
    /// Tower, so maps can be built before the real blocks exist. Each block holds its location's building and a second
    /// building from the same district (a Park holds a pond and trees instead), weighted sockets, and a tinted lot.
    /// Hand-built blocks (<c>Docs/HowTo_AddBlock.md</c>) go next to them and replace them over time.
    /// </summary>
    internal static class GrayboxBlockCreator
    {
        private const string MenuPath = "Zenthium/Map/Create Graybox Blocks";
        private const string Folder = MapBlockLibraryRefresher.BlocksFolder + "/Graybox";
        private const string PrefabExtension = ".prefab";

        // Meters kept clear between the buildings and the block's edge, and between the two buildings.
        private const float EdgeMargin = 6f;
        private const float InnerGap = 2f;

        // The second building is set back from the middle so its door has room to open onto open ground.
        private const float FillerSetback = 6f;
        private const float LotThickness = 0.02f;
        private const float BuildingLabelOffset = 1.5f;

        // The Park's pond is taller than an agent's step, so the bake treats it as an obstacle.
        private static readonly Vector3 PondSize = new Vector3(14f, 1f, 10f);
        private static readonly Vector3 TreeSize = new Vector3(1.2f, 3f, 1.2f);
        private static readonly Vector2[] TreeQuadrantOffsets =
        {
            new Vector2(-0.6f, 0.2f), new Vector2(0.3f, -0.4f), new Vector2(0.5f, 0.6f), new Vector2(-0.3f, -0.7f),
        };

        private static readonly Color DowntownColor = new Color(0.45f, 0.45f, 0.55f);
        private static readonly Color IndustrialColor = new Color(0.5f, 0.45f, 0.35f);
        private static readonly Color ResidentialColor = new Color(0.35f, 0.5f, 0.35f);
        private static readonly Color PondColor = new Color(0.2f, 0.4f, 0.8f);
        private static readonly Color TreeColor = new Color(0.15f, 0.45f, 0.15f);

        private static class BlockFields
        {
            public const string District = "district";
            public const string ExtraLocations = "extraLocations";
        }

        [MenuItem(MenuPath)]
        private static void CreateBlocks()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog("TextMesh Pro resources missing",
                    "Import them first: Window > TextMeshPro > Import TMP Essential Resources.", "OK");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GeneratedAssetWriter.DatabasePath);
            if (database == null || database.MapConstants.BlockSize <= 0f)
            {
                EditorUtility.DisplayDialog("Game data missing",
                    "Run Zenthium > Data > Import Design CSVs first: the block size comes from constants.csv.", "OK");
                return;
            }

            if (AssetDatabase.IsValidFolder(Folder) && !EditorUtility.DisplayDialog("Graybox blocks already exist",
                    $"Replace the block prefabs in {Folder} with fresh ones? Blocks elsewhere aren't touched.",
                    "Replace", "Cancel"))
            {
                return;
            }

            MapBlockLibraryRefresher.EnsureFolder(Folder);
            float half = database.MapConstants.BlockSize * 0.5f;
            int count = 0;
            foreach (LocationType location in (LocationType[])System.Enum.GetValues(typeof(LocationType)))
            {
                if (LocationDistricts.IsUnique(location))
                {
                    continue;
                }

                GameObject block = BuildBlock(location, half);
                PlayerSandboxSceneCreator.SaveAsPrefab(block, $"{Folder}/Block_{location}{PrefabExtension}");
                count++;
            }

            MapBlockLibraryRefresher.Refresh();
            Debug.Log($"Created {count} graybox blocks in {Folder}.");
        }

        private static GameObject BuildBlock(LocationType location, float half)
        {
            District district = LocationDistricts.Of(location);
            var root = new GameObject($"Block {location}");
            Wire(root.AddComponent<MapBlock>(), so =>
            {
                Find(so, BlockFields.District).enumValueIndex = (int)district;
                SerializedProperty extra = Find(so, BlockFields.ExtraLocations);
                bool isOutdoor = location == LocationType.Park;
                extra.arraySize = isOutdoor ? 1 : 0;
                if (isOutdoor)
                {
                    extra.GetArrayElementAtIndex(0).enumValueIndex = (int)location;
                }
            });

            // The lot's tint shows the district; it has no collider, so the bake sees only the map's ground.
            AiSandboxSceneCreator.Visual(PrimitiveType.Cube, "Lot", root.transform,
                new Vector3(0f, LotThickness * 0.5f, 0f), new Vector3(half * 2f, LotThickness, half * 2f),
                AiSandboxSceneCreator.ModuleMaterial($"Lot{district}", DistrictColor(district)));

            if (location == LocationType.Park)
            {
                BuildPark(root.transform, half);
                return root;
            }

            var context = new WorldSandboxSceneCreator.BuildContext { Labels = root.transform, LabelsStructures = false };
            StructureMaterial material = DistrictMaterial(district);

            // The location's building fills the -X, +Z quarter; the second one the +X, -Z quarter.
            BuildRectBuilding(context, root.transform, location, material,
                Rect.MinMaxRect(-half + EdgeMargin, InnerGap, -InnerGap, half - EdgeMargin), true);
            BuildRectBuilding(context, root.transform, Filler(location), material,
                Rect.MinMaxRect(InnerGap + FillerSetback, -half + EdgeMargin, half - EdgeMargin,
                    -InnerGap - FillerSetback), false);

            // The open quarters: crop circles, squads and citizens.
            WorldSandboxSceneCreator.CreateSocket(root.transform, "Open Socket (NE)",
                new Vector3(half * 0.5f, 0f, half * 0.5f), new SocketOption(SocketContent.CropCircle, 1f),
                new SocketOption(SocketContent.Goon, 1f), new SocketOption(SocketContent.Citizen, 1f));
            WorldSandboxSceneCreator.CreateSocket(root.transform, "Open Socket (SW)",
                new Vector3(-half * 0.5f, 0f, -half * 0.5f), new SocketOption(SocketContent.Citizen, 2f),
                new SocketOption(SocketContent.Goon, 1f), new SocketOption(SocketContent.CropCircle, 1f));
            return root;
        }

        /// <summary>
        /// A box building with an unlocked door in the middle of its south side (or north side), and a window in the
        /// middle of each side wall. A Valuables socket stands in its middle.
        /// </summary>
        private static void BuildRectBuilding(WorldSandboxSceneCreator.BuildContext context, Transform block,
            LocationType location, StructureMaterial material, Rect area, bool doorSouth)
        {
            string title = ObjectNames.NicifyVariableName(location.ToString());
            Transform root = WorldSandboxSceneCreator.CreateBuilding(context, title, location);
            root.SetParent(block, false);
            var southWest = new Vector3(area.xMin, 0f, area.yMin);
            var southEast = new Vector3(area.xMax, 0f, area.yMin);
            var northEast = new Vector3(area.xMax, 0f, area.yMax);
            var northWest = new Vector3(area.xMin, 0f, area.yMax);

            Vector3 doorFrom = doorSouth ? southWest : northWest;
            Vector3 doorTo = doorSouth ? southEast : northEast;
            Vector3 doorOutward = doorSouth ? Vector3.back : Vector3.forward;
            WorldSandboxSceneCreator.WallWithOpening(context, root, material, doorFrom, doorTo, area.width * 0.5f,
                opening => WorldSandboxSceneCreator.CreateDoor(context, root, opening, material, false, doorOutward));
            WorldSandboxSceneCreator.Wall(context, root, material, doorSouth ? northWest : southWest,
                doorSouth ? northEast : southEast);
            WorldSandboxSceneCreator.WallWithOpening(context, root, material, southWest, northWest,
                area.height * 0.5f, opening => WorldSandboxSceneCreator.CreateWindow(context, root, opening,
                    Vector3.left));
            WorldSandboxSceneCreator.WallWithOpening(context, root, material, southEast, northEast,
                area.height * 0.5f, opening => WorldSandboxSceneCreator.CreateWindow(context, root, opening,
                    Vector3.right));

            WorldSandboxSceneCreator.CreateZone(root, "Interior", area);
            WorldSandboxSceneCreator.AddBuildingViews(context, root, title,
                new Vector3(area.center.x, 0f, area.yMax + BuildingLabelOffset));
            WorldSandboxSceneCreator.CreateSocket(root, "Valuables Socket",
                new Vector3(area.center.x, 0f, area.center.y), new SocketOption(SocketContent.Valuables, 2f),
                new SocketOption(SocketContent.Citizen, 1f));
        }

        /// <summary>A pond in the -X, +Z quarter, trees in the others, and three open sockets.</summary>
        private static void BuildPark(Transform block, float half)
        {
            Material pondMaterial = AiSandboxSceneCreator.ModuleMaterial("Pond", PondColor);
            Material treeMaterial = AiSandboxSceneCreator.ModuleMaterial("ParkTree", TreeColor);
            var quarter = new Vector2(-half * 0.5f, half * 0.5f);
            Obstacle(PrimitiveType.Cube, "Pond", block, new Vector3(quarter.x, PondSize.y * 0.5f, quarter.y), PondSize,
                pondMaterial);

            var quarters = new[]
            {
                new Vector2(half * 0.5f, half * 0.5f),
                new Vector2(half * 0.5f, -half * 0.5f),
                new Vector2(-half * 0.5f, -half * 0.5f),
            };
            foreach (Vector2 center in quarters)
            {
                foreach (Vector2 offset in TreeQuadrantOffsets)
                {
                    Vector2 point = center + offset * (half * 0.4f);

                    // The Cylinder primitive is 2 m tall at scale 1, so it scales by half the height.
                    Obstacle(PrimitiveType.Cylinder, "Tree", block, new Vector3(point.x, TreeSize.y * 0.5f, point.y),
                        new Vector3(TreeSize.x, TreeSize.y * 0.5f, TreeSize.z), treeMaterial);
                }

                WorldSandboxSceneCreator.CreateSocket(block, "Open Socket", new Vector3(center.x, 0f, center.y),
                    new SocketOption(SocketContent.Citizen, 2f), new SocketOption(SocketContent.Goon, 1f),
                    new SocketOption(SocketContent.CropCircle, 1f));
            }
        }

        /// <summary>A fixed obstacle with its collider: not a structure, so the NavMesh bake keeps it out.</summary>
        private static void Obstacle(PrimitiveType type, string objectName, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject obstacle = GameObject.CreatePrimitive(type);
            obstacle.name = objectName;
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.localPosition = position;
            obstacle.transform.localScale = scale;
            obstacle.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>The block's second building: the next location in the same district, skipping outdoor ones.</summary>
        private static LocationType Filler(LocationType location)
        {
            District district = LocationDistricts.Of(location);
            var candidates = new List<LocationType>();
            foreach (LocationType other in (LocationType[])System.Enum.GetValues(typeof(LocationType)))
            {
                if (LocationDistricts.Of(other) == district && !LocationDistricts.IsUnique(other)
                    && other != LocationType.Park)
                {
                    candidates.Add(other);
                }
            }

            int index = candidates.IndexOf(location);
            return candidates[(index + 1) % candidates.Count];
        }

        private static StructureMaterial DistrictMaterial(District district)
        {
            switch (district)
            {
                case District.Downtown:
                    return StructureMaterial.Stone;
                case District.Industrial:
                    return StructureMaterial.Metal;
                default:
                    return StructureMaterial.Wood;
            }
        }

        private static Color DistrictColor(District district)
        {
            switch (district)
            {
                case District.Downtown:
                    return DowntownColor;
                case District.Industrial:
                    return IndustrialColor;
                default:
                    return ResidentialColor;
            }
        }
    }
}
