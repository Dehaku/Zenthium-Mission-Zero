using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Sandbox;
using Zenthium.World;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_World (Milestone 5): a larger version of the Player sandbox's room with two buildings (a Wood
    /// house, and an L-shaped Stone shop with two interior zones and a locked Metal door), a sample wall of each
    /// material, one prop of each props.csv row, a Heavy Turret firing at a dummy, the hero select and MoreInfo
    /// panels, and a baked NavMesh. Building walls are left out of the bake and carve their own holes, so breaking
    /// one opens the NavMesh. Every structure has an HP label. Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class WorldSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create World Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_World.unity";
        private const string NavMeshPath = "Assets/_Project/Scenes/Sandbox_World_NavMesh.asset";
        private const string TurretPrefabPath = PlayerSandboxSceneCreator.PrefabFolder + "/HeavyTurret.prefab";

        private const float RoomSize = 60f;
        private const float WallHeight = 2f;
        private const float WallThickness = 0.5f;
        private const float OpeningWidth = 2f;

        // A door's trigger reaches this far either side of its panel, so it opens before anyone touches it.
        private const float DoorReach = 1.5f;

        // Sockets for arson stand this far outside the opening (maps.md, Sockets).
        private const float ArsonSocketDistance = 1.5f;

        private const float SampleWallLength = 4f;
        private const float SampleWallZ = -22f;
        private static readonly float[] SampleWallX = { -12f, -6f, 0f, 6f, 12f };

        private const float PropRowZ = -13f;
        private const float RingThickness = 0.02f;
        private const float RingHeight = 0.05f;
        private const float LabelFontSize = 3f;
        private static readonly Vector2 LabelSize = new Vector2(6f, 2f);
        private static readonly Vector3 LabelOffset = new Vector3(0f, 3f, 0f);

        private static readonly Vector3 TurretPosition =
            new Vector3(26f, PlayerSandboxSceneCreator.CapsuleHalfHeight, -26f);

        // A dummy for the turret to shoot, so it doesn't wear the player down while they test other things.
        private static readonly Vector3 TurretTargetPosition =
            new Vector3(18f, PlayerSandboxSceneCreator.CapsuleHalfHeight, -26f);

        private static readonly Color GlassColor = new Color(0.65f, 0.9f, 1f);
        private static readonly Color WoodColor = new Color(0.55f, 0.35f, 0.15f);
        private static readonly Color StoneColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color MetalColor = new Color(0.3f, 0.35f, 0.45f);
        private static readonly Color CrystallineColor = new Color(0.8f, 0.3f, 0.9f);
        private static readonly Color PropColor = new Color(0.85f, 0.75f, 0.2f);
        private static readonly Color VehicleColor = new Color(0.2f, 0.55f, 0.3f);
        private static readonly Color RingColor = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color TurretColor = new Color(0.25f, 0.25f, 0.25f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class StructureFields
        {
            public const string World = "world";
        }

        private static class PartFields
        {
            public const string Material = "material";
        }

        private static class DoorFields
        {
            public const string Panel = "panel";
            public const string IsLocked = "isLocked";
        }

        private static class BuildingFields
        {
            public const string Location = "location";
            public const string Registry = "registry";
        }

        private static class RegistryFields
        {
            public const string Database = "database";
        }

        private static class PropFields
        {
            public const string Data = "data";
        }

        private static class SocketFields
        {
            public const string Options = "options";
            public const string Content = "content";
            public const string Weight = "weight";
        }

        private static class LabelFields
        {
            public const string Target = "target";
            public const string Label = "label";
            public const string Title = "title";
            public const string Offset = "offset";
        }

        private static class RingFields
        {
            public const string Explosion = "explosion";
            public const string Ring = "ring";
        }

        private static class TurretFields
        {
            public const string Tags = "tags";
        }

        private static class AutoCasterFields
        {
            public const string Target = "target";
        }

        /// <summary>A prop to place: its props.csv row, a look and a box size.</summary>
        private readonly struct PropSpec
        {
            public PropSpec(string id, string title, float x, Vector3 size, bool isVehicle)
            {
                Id = id;
                Title = title;
                X = x;
                Size = size;
                IsVehicle = isVehicle;
            }

            public string Id { get; }
            public string Title { get; }
            public float X { get; }
            public Vector3 Size { get; }
            public bool IsVehicle { get; }
        }

        private static readonly PropSpec[] PropSpecs =
        {
            new PropSpec(DataIds.Props.SmallProp, "Bench (Small)", -14f, new Vector3(1.6f, 0.8f, 0.6f), false),
            new PropSpec(DataIds.Props.MediumProp, "Dumpster (Medium)", -8f, new Vector3(2f, 1.5f, 1.2f), false),
            new PropSpec(DataIds.Props.LargeProp, "Tree (Large)", -2f, new Vector3(1.2f, 3f, 1.2f), false),
            new PropSpec(DataIds.Props.CarTruck, "Car", 6f, new Vector3(2f, 1.2f, 4f), true),
            new PropSpec(DataIds.Props.Motorcycle, "Motorcycle", 13f, new Vector3(0.8f, 1f, 2f), true),
        };

        /// <summary>Scene objects the structures need.</summary>
        private sealed class BuildContext
        {
            public PlayerSandboxSceneCreator.Room Room;
            public BuildingRegistry Registry;
            public Transform Labels;
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxWorld()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room, RoomSize))
            {
                return;
            }

            GameDatabase database = room.Database;
            if (!database.Entities.TryGet(DataIds.Entities.HeavyTurret, out EntityData turretData)
                || !database.Modules.TryGet(DataIds.Modules.HeavyGun, out ModuleData heavyGun))
            {
                Debug.LogError($"The game data has no {DataIds.Entities.HeavyTurret} or {DataIds.Modules.HeavyGun}; "
                    + $"{ScenePath} wasn't written.");
                return;
            }

            // Matches the hero select's slots dropdown, which starts at all slots.
            Wire(room.Player, so => Find(so, CombatSandboxSceneCreator.EntityFields.UnlockedSlots).intValue =
                database.ModuleConstants.SlotsPerType);

            var registry = new GameObject("Building Registry").AddComponent<BuildingRegistry>();
            Wire(registry, so => SetReference(so, RegistryFields.Database, database));
            var context = new BuildContext
            {
                Room = room,
                Registry = registry,
                Labels = new GameObject("Structure Labels").transform,
            };

            BuildWoodHouse(context);
            BuildStoneShop(context);
            BuildSampleWalls(context);
            BuildProps(context);
            EntityBehaviour turretTarget =
                PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, TurretTargetPosition, room);
            turretTarget.name = "Turret Target Dummy";
            EntityBehaviour turret = PlaceTurret(room, turretData, heavyGun, turretTarget);

            // The panel inspects the player, so you can raise your Strength to lift Large props.
            CombatSandboxSceneCreator.BindDebugPanel(room.DebugPanel, room.Player, turret);
            ModulesSandboxSceneCreator.CreateModulesCanvas(room);
            BakeNavMesh(room.Geometry);

            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath}, {NavMeshPath} and {TurretPrefabPath}.");
        }

        /// <summary>A 10 x 10 m Wood house: an unlocked Wood door south, a Glass window west.</summary>
        private static void BuildWoodHouse(BuildContext context)
        {
            Transform root = CreateBuilding(context, "Wood House", LocationType.House);
            const float minX = 9f;
            const float maxX = 19f;
            const float minZ = 8f;
            const float maxZ = 18f;
            float doorX = (minX + maxX) * 0.5f;
            float windowZ = (minZ + maxZ) * 0.5f;

            WallWithOpening(context, root, StructureMaterial.Wood, new Vector3(minX, 0f, minZ),
                new Vector3(maxX, 0f, minZ), doorX - minX, opening => CreateDoor(context, root, opening,
                    StructureMaterial.Wood, false, Vector3.back));
            Wall(context, root, StructureMaterial.Wood, new Vector3(minX, 0f, maxZ), new Vector3(maxX, 0f, maxZ));
            Wall(context, root, StructureMaterial.Wood, new Vector3(maxX, 0f, minZ), new Vector3(maxX, 0f, maxZ));
            WallWithOpening(context, root, StructureMaterial.Wood, new Vector3(minX, 0f, minZ),
                new Vector3(minX, 0f, maxZ), windowZ - minZ, opening => CreateWindow(context, root, opening,
                    Vector3.left));

            CreateZone(root, "Interior", Rect.MinMaxRect(minX, minZ, maxX, maxZ));
            CreateSocket(root, "Valuables Socket", new Vector3(doorX, 0f, maxZ - 2f), SocketContent.Valuables);
        }

        /// <summary>
        /// An L-shaped Stone shop with two interior zones: a locked Metal door south, a Glass window west. 19 m from
        /// the Wood house, so they're neighbors.
        /// </summary>
        private static void BuildStoneShop(BuildContext context)
        {
            Transform root = CreateBuilding(context, "Stone Shop (L-shaped)", LocationType.Store);
            var southWest = new Vector3(-22f, 0f, 8f);
            var southEast = new Vector3(-10f, 0f, 8f);
            var innerEast = new Vector3(-10f, 0f, 14f);
            var innerCorner = new Vector3(-16f, 0f, 14f);
            var northEast = new Vector3(-16f, 0f, 22f);
            var northWest = new Vector3(-22f, 0f, 22f);

            WallWithOpening(context, root, StructureMaterial.Stone, southWest, southEast, 9f,
                opening => CreateDoor(context, root, opening, StructureMaterial.Metal, true, Vector3.back));
            Wall(context, root, StructureMaterial.Stone, southEast, innerEast);
            Wall(context, root, StructureMaterial.Stone, innerEast, innerCorner);
            Wall(context, root, StructureMaterial.Stone, innerCorner, northEast);
            Wall(context, root, StructureMaterial.Stone, northEast, northWest);
            WallWithOpening(context, root, StructureMaterial.Stone, southWest, northWest, 10f,
                opening => CreateWindow(context, root, opening, Vector3.left));

            CreateZone(root, "Interior (front)", Rect.MinMaxRect(-22f, 8f, -10f, 14f));
            CreateZone(root, "Interior (back)", Rect.MinMaxRect(-22f, 14f, -16f, 22f));
        }

        /// <summary>One free-standing wall of each material, weakest to strongest, with no building.</summary>
        private static void BuildSampleWalls(BuildContext context)
        {
            var root = new GameObject("Sample Walls").transform;
            var materials = (StructureMaterial[])System.Enum.GetValues(typeof(StructureMaterial));
            for (int i = 0; i < materials.Length && i < SampleWallX.Length; i++)
            {
                float half = SampleWallLength * 0.5f;
                Transform wall = Wall(context, root, materials[i], new Vector3(SampleWallX[i] - half, 0f, SampleWallZ),
                    new Vector3(SampleWallX[i] + half, 0f, SampleWallZ));
                if (materials[i] == StructureMaterial.Glass)
                {
                    wall.gameObject.AddComponent<SeeThrough>();
                }
            }
        }

        private static void BuildProps(BuildContext context)
        {
            var root = new GameObject("Props").transform;
            foreach (PropSpec spec in PropSpecs)
            {
                if (!context.Room.Database.Props.TryGet(spec.Id, out PropData data))
                {
                    Debug.LogError($"The game data has no prop {spec.Id}; it was left out.");
                    continue;
                }

                GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = spec.Title;
                prop.transform.SetParent(root, false);
                prop.transform.localPosition = new Vector3(spec.X, spec.Size.y * 0.5f, PropRowZ);
                prop.transform.localScale = spec.Size;
                Color color = spec.IsVehicle ? VehicleColor : PropColor;
                prop.GetComponent<Renderer>().sharedMaterial =
                    ModulePrefabBuilder.SaveMaterial(spec.IsVehicle ? "Vehicle" : "Prop", color, prop);

                prop.AddComponent<Rigidbody>().isKinematic = true;
                Structure structure = AddStructure(context, prop);
                Wire(prop.AddComponent<PropBody>(), so => SetReference(so, PropFields.Data, data));
                NavMeshObstacle obstacle = prop.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = Vector3.one;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                if (spec.IsVehicle)
                {
                    AddExplosion(prop, spec.Size);
                }

                AddLabel(context, structure, spec.Title);
            }
        }

        private static void AddExplosion(GameObject vehicle, Vector3 size)
        {
            var explosion = vehicle.AddComponent<VehicleExplosion>();
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Blast Ring";

            // A collider here would join the vehicle's body.
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(vehicle.transform, false);

            // Just above the ground, in the vehicle's scaled space; the view sets the true size when it's shown.
            ring.transform.localPosition = new Vector3(0f, (RingHeight - size.y * 0.5f) / size.y, 0f);
            ring.transform.localScale = new Vector3(1f, RingThickness / size.y, 1f);
            ring.GetComponent<Renderer>().sharedMaterial = ModulePrefabBuilder.SaveMaterial("Blast", RingColor, ring);
            Wire(vehicle.AddComponent<ExplosionRingView>(), so =>
            {
                SetReference(so, RingFields.Explosion, explosion);
                SetReference(so, RingFields.Ring, ring.transform);
            });
        }

        private static EntityBehaviour PlaceTurret(PlayerSandboxSceneCreator.Room room, EntityData turretData,
            ModuleData heavyGun, EntityBehaviour target)
        {
            GameObject body = PlayerSandboxSceneCreator.BuildEntityBody("Heavy Turret", room.Database, null,
                turretData, Team.Neutral);
            body.GetComponent<Renderer>().sharedMaterial = ModulePrefabBuilder.SaveMaterial("Turret", TurretColor, body);
            Wire(body.GetComponent<EntityBehaviour>(), so =>
            {
                SerializedProperty modules = Find(so, CombatSandboxSceneCreator.EntityFields.Modules);
                modules.arraySize = 1;
                modules.GetArrayElementAtIndex(0).objectReferenceValue = heavyGun;
                SerializedProperty tags = Find(so, TurretFields.Tags);
                tags.arraySize = 2;
                tags.GetArrayElementAtIndex(0).enumValueIndex = (int)EntityTag.Hackable;
                tags.GetArrayElementAtIndex(1).enumValueIndex = (int)EntityTag.Immobile;
            });
            GameObject prefab = PlayerSandboxSceneCreator.SaveAsPrefab(body, TurretPrefabPath);

            EntityBehaviour turret = PlayerSandboxSceneCreator.PlaceEntity(prefab, TurretPosition, room);
            Wire(turret.gameObject.AddComponent<SandboxAutoCaster>(),
                so => SetReference(so, AutoCasterFields.Target, target));
            return turret;
        }

        private static Transform CreateBuilding(BuildContext context, string buildingName, LocationType location)
        {
            var root = new GameObject(buildingName);
            Wire(root.AddComponent<Building>(), so =>
            {
                Find(so, BuildingFields.Location).enumValueIndex = (int)location;
                SetReference(so, BuildingFields.Registry, context.Registry);
            });
            return root.transform;
        }

        /// <summary>A wall from one corner to another along the ground. Building walls carve the NavMesh.</summary>
        private static Transform Wall(BuildContext context, Transform parent, StructureMaterial material, Vector3 from,
            Vector3 to)
        {
            Transform wall = Panel(context, parent, material + " Wall", material, from, to, WallThickness);
            AddCarving(wall.gameObject);
            AddLabel(context, wall.GetComponent<Structure>(), material + " wall");
            return wall;
        }

        /// <summary>
        /// A wall with a gap <see cref="OpeningWidth"/> wide, centered <paramref name="openingCenter"/> meters from
        /// <paramref name="from"/>, which the callback fills (with a door or a window).
        /// </summary>
        private static void WallWithOpening(BuildContext context, Transform parent, StructureMaterial material,
            Vector3 from, Vector3 to, float openingCenter, System.Action<(Vector3 From, Vector3 To)> fillOpening)
        {
            Vector3 along = (to - from).normalized;
            Vector3 openingFrom = from + along * (openingCenter - OpeningWidth * 0.5f);
            Vector3 openingTo = from + along * (openingCenter + OpeningWidth * 0.5f);
            Wall(context, parent, material, from, openingFrom);
            Wall(context, parent, material, openingTo, to);
            fillOpening((openingFrom, openingTo));
        }

        private static void CreateWindow(BuildContext context, Transform parent, (Vector3 From, Vector3 To) opening,
            Vector3 outward)
        {
            Transform window = Panel(context, parent, "Window", StructureMaterial.Glass, opening.From, opening.To,
                WallThickness);
            window.gameObject.AddComponent<SeeThrough>();
            AddCarving(window.gameObject);
            AddLabel(context, window.GetComponent<Structure>(), "Glass window");
            CreateSocket(parent, "Arson Socket (window)", Midpoint(opening) + outward * ArsonSocketDistance,
                SocketContent.Arson);
        }

        /// <summary>A door: a trigger box around the doorway, with its panel as a child. Doors don't carve.</summary>
        private static void CreateDoor(BuildContext context, Transform parent, (Vector3 From, Vector3 To) opening,
            StructureMaterial material, bool isLocked, Vector3 outward)
        {
            Vector3 center = Midpoint(opening);
            Vector3 along = opening.To - opening.From;
            var doorObject = new GameObject(isLocked ? "Locked Door" : "Door");
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.SetPositionAndRotation(center + Vector3.up * (WallHeight * 0.5f),
                Quaternion.LookRotation(Vector3.Cross(along, Vector3.up).normalized, Vector3.up));
            doorObject.AddComponent<BoxCollider>().size =
                new Vector3(along.magnitude, WallHeight, WallThickness + 2f * DoorReach);

            Transform panel = Panel(context, doorObject.transform, "Door Panel", material, opening.From, opening.To,
                WallThickness);
            Wire(doorObject.AddComponent<Door>(), so =>
            {
                SetReference(so, DoorFields.Panel, panel.GetComponent<BuildingPart>());
                Find(so, DoorFields.IsLocked).boolValue = isLocked;
            });
            AddLabel(context, panel.GetComponent<Structure>(), (isLocked ? "Locked " : string.Empty) + material + " door");
            CreateSocket(parent, "Arson Socket (door)", center + outward * ArsonSocketDistance, SocketContent.Arson);
        }

        /// <summary>A wall-shaped structure: a cube from one point to another, with its material's look.</summary>
        private static Transform Panel(BuildContext context, Transform parent, string objectName,
            StructureMaterial material, Vector3 from, Vector3 to, float thickness)
        {
            Vector3 along = to - from;
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = objectName;
            panel.transform.SetParent(parent, true);
            panel.transform.SetPositionAndRotation((from + to) * 0.5f + Vector3.up * (WallHeight * 0.5f),
                Quaternion.LookRotation(along.normalized, Vector3.up));

            // Along the wall is the cube's Z, so the thickness is X. Building roots and doors are never scaled.
            panel.transform.localScale = new Vector3(thickness, WallHeight, along.magnitude);
            panel.GetComponent<Renderer>().sharedMaterial =
                ModulePrefabBuilder.SaveMaterial(material.ToString(), MaterialColor(material), panel);

            AddStructure(context, panel);
            Wire(panel.AddComponent<BuildingPart>(),
                so => Find(so, PartFields.Material).enumValueIndex = (int)material);
            return panel.transform;
        }

        private static Structure AddStructure(BuildContext context, GameObject target)
        {
            var structure = target.AddComponent<Structure>();
            Wire(structure, so => SetReference(so, StructureFields.World, context.Room.World));
            return structure;
        }

        private static void AddCarving(GameObject wall)
        {
            NavMeshObstacle obstacle = wall.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
        }

        private static void CreateZone(Transform building, string zoneName, Rect area)
        {
            var zone = new GameObject(zoneName);
            zone.transform.SetParent(building, false);
            zone.transform.position = new Vector3(area.center.x, WallHeight * 0.5f, area.center.y);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(area.width, WallHeight, area.height);
            zone.AddComponent<InteriorZone>();
        }

        private static void CreateSocket(Transform building, string socketName, Vector3 position,
            SocketContent content)
        {
            var socket = new GameObject(socketName);
            socket.transform.SetParent(building, false);
            socket.transform.position = position;
            Wire(socket.AddComponent<MapSocket>(), so =>
            {
                SerializedProperty options = Find(so, SocketFields.Options);
                options.arraySize = 1;
                SerializedProperty option = options.GetArrayElementAtIndex(0);
                option.FindPropertyRelative(SocketFields.Content).enumValueIndex = (int)content;
                option.FindPropertyRelative(SocketFields.Weight).floatValue = 1f;
            });
        }

        private static void AddLabel(BuildContext context, Structure target, string title)
        {
            var labelObject = new GameObject(title + " Label", typeof(RectTransform), typeof(TextMeshPro));
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(context.Labels, false);
            rect.position = target.transform.position + LabelOffset;

            // Lying flat, facing the overhead camera.
            rect.rotation = Quaternion.Euler(90f, 0f, 0f);
            rect.sizeDelta = LabelSize;
            var text = labelObject.GetComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = LabelFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = title;

            Wire(labelObject.AddComponent<StructureDebugLabel>(), so =>
            {
                SetReference(so, LabelFields.Target, target);
                SetReference(so, LabelFields.Label, text);
                Find(so, LabelFields.Title).stringValue = title;
                Find(so, LabelFields.Offset).vector3Value = LabelOffset;
            });
        }

        /// <summary>
        /// Bakes the room's floor, outer walls and pillars only: buildings and props carve the NavMesh at runtime,
        /// and doors stay walkable, as enemies plan paths as if doors weren't there (world.md, Doors).
        /// </summary>
        private static void BakeNavMesh(Transform geometry)
        {
            var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        private static Vector3 Midpoint((Vector3 From, Vector3 To) opening)
        {
            return (opening.From + opening.To) * 0.5f;
        }

        private static Color MaterialColor(StructureMaterial material)
        {
            switch (material)
            {
                case StructureMaterial.Glass:
                    return GlassColor;
                case StructureMaterial.Wood:
                    return WoodColor;
                case StructureMaterial.Stone:
                    return StoneColor;
                case StructureMaterial.Metal:
                    return MetalColor;
                default:
                    return CrystallineColor;
            }
        }
    }
}
