using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Zenthium.AI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Sandbox;
using Zenthium.World;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_Map (Milestone 8): the Player sandbox's services and player with no room, a map builder that
    /// lays out the block library at runtime, the fog of war, and the launcher panel (size, location, seed, exits,
    /// rank, crop circles, a goon to spawn, and the fog on or off). Saves the ground, exit zone and crop circle prefabs
    /// the builder uses, and refreshes the block library. The map's NavMesh is baked when it's built, so the scene has
    /// none. Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class MapSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Map Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Map.unity";
        private const string MapPrefabFolder = PlayerSandboxSceneCreator.PrefabFolder + "/Map";
        private const string GroundPrefabPath = MapPrefabFolder + "/MapGround.prefab";
        private const string ExitZonePrefabPath = MapPrefabFolder + "/ExitZone.prefab";
        private const string CropCirclePrefabPath = MapPrefabFolder + "/CropCircle.prefab";
        private const string FogMaterialPath = PlayerSandboxSceneCreator.PrefabFolder + "/Materials/FogOfWar.mat";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        // The ground's top is at Y 0.
        private const float GroundThickness = 0.2f;

        // Exit zones and crop circles: flat markers just above the ground and the block lots.
        private const float MarkerHeight = 0.05f;
        private const float MarkerThickness = 0.02f;
        private const float ExitZoneHeight = 2f;

        private static readonly Color GroundColor = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color ExitZoneColor = new Color(0.2f, 0.6f, 1f, 0.45f);
        private static readonly Color CropCircleColor = new Color(0.3f, 1f, 0.4f, 0.35f);

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 PanelPosition = new Vector2(-10f, -10f);
        private const float PanelWidth = 460f;
        private const int PanelPadding = 8;
        private const float Spacing = 4f;
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);

        private static class RegistryFields
        {
            public const string Database = "database";
            public const string World = "world";
        }

        private static class AssemblerFields
        {
            public const string World = "world";
            public const string Registry = "registry";
            public const string MapBounds = "mapBounds";
            public const string Library = "library";
            public const string GroundPrefab = "groundPrefab";
            public const string ExitZonePrefab = "exitZonePrefab";
            public const string CropCirclePrefab = "cropCirclePrefab";
        }

        private static class FogFields
        {
            public const string World = "world";
            public const string Assembler = "assembler";
            public const string Viewer = "viewer";
            public const string Fog = "fog";
            public const string Quad = "quad";
            public const string Valuables = "valuables";
        }

        private static class FogToggleFields
        {
            public const string View = "view";
            public const string Hiding = "hiding";
            public const string Button = "button";
            public const string Label = "label";
        }

        private static class LauncherFields
        {
            public const string Assembler = "assembler";
            public const string World = "world";
            public const string Player = "player";
            public const string Faction = "faction";
            public const string Goon = "goon";
            public const string SizeDropdown = "sizeDropdown";
            public const string LocationDropdown = "locationDropdown";
            public const string ExitsDropdown = "exitsDropdown";
            public const string RankDropdown = "rankDropdown";
            public const string SeedInput = "seedInput";
            public const string CropCirclesToggle = "cropCirclesToggle";
            public const string BuildButton = "buildButton";
            public const string NewSeedButton = "newSeedButton";
            public const string SpawnButton = "spawnButton";
            public const string Status = "status";
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxMap()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room))
            {
                return;
            }

            GameDatabase database = room.Database;
            if (!database.Factions.TryGet(DataIds.Factions.GalacticRepoAgency, out FactionData repo)
                || !database.Entities.TryGet(DataIds.Entities.Grey, out EntityData grey)
                || database.MapConstants.BlockSize <= 0f)
            {
                Debug.LogError($"The game data is missing the Repo Agency, Grey or the map constants; {ScenePath} "
                    + "wasn't written. Re-import the design CSVs.");
                return;
            }

            MapServices map = CreateMapServices(room, grey);
            CreateLauncher(room, map.Assembler, repo, grey, map.FogView, map.FogHiding);
            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath} and the map prefabs in {MapPrefabFolder}. " + LibraryReport(map.Library));
        }

        /// <summary>What <see cref="CreateMapServices"/> made, for scenes that build maps.</summary>
        internal sealed class MapServices
        {
            public MapBlockLibrary Library;
            public BuildingRegistry Registry;
            public MapBounds Bounds;
            public FactionEntitySpawner Spawner;
            public MapAssembler Assembler;
            public FogOfWarView FogView;
            public FogHiding FogHiding;
        }

        /// <summary>
        /// Replaces the room with what builds maps at runtime: removes the room, its dummies and the combat panel that
        /// inspects them, refreshes the block library, saves the ground, exit zone and crop circle prefabs, and adds
        /// the building registry, map bounds, faction spawner, map assembler and the fog of war.
        /// </summary>
        internal static MapServices CreateMapServices(PlayerSandboxSceneCreator.Room room, EntityData grey)
        {
            Object.DestroyImmediate(room.Geometry.gameObject);
            foreach (EntityBehaviour dummy in room.Dummies)
            {
                Object.DestroyImmediate(dummy.gameObject);
            }

            room.Dummies.Clear();
            Object.DestroyImmediate(room.DebugPanel.GetComponentInParent<Canvas>().gameObject);

            var map = new MapServices { Library = MapBlockLibraryRefresher.Refresh() };
            MapBlockLibraryRefresher.EnsureFolder(MapPrefabFolder);
            GameObject ground = PlayerSandboxSceneCreator.SaveAsPrefab(BuildGround(), GroundPrefabPath);
            GameObject exitZone = PlayerSandboxSceneCreator.SaveAsPrefab(BuildExitZone(), ExitZonePrefabPath);
            GameObject cropCircle = PlayerSandboxSceneCreator.SaveAsPrefab(BuildCropCircle(), CropCirclePrefabPath);

            map.Registry = new GameObject("Building Registry").AddComponent<BuildingRegistry>();
            Wire(map.Registry, so =>
            {
                SetReference(so, RegistryFields.Database, room.Database);
                SetReference(so, RegistryFields.World, room.World);
            });
            map.Bounds = new GameObject("Map Bounds").AddComponent<MapBounds>();
            map.Spawner = RepoSandboxSceneCreator.CreateSpawner(room, map.Bounds, grey);

            map.Assembler = new GameObject("Map Assembler").AddComponent<MapAssembler>();
            Wire(map.Assembler, so =>
            {
                SetReference(so, AssemblerFields.World, room.World);
                SetReference(so, AssemblerFields.Registry, map.Registry);
                SetReference(so, AssemblerFields.MapBounds, map.Bounds);
                SetReference(so, AssemblerFields.Library, map.Library);
                SetReference(so, AssemblerFields.GroundPrefab, ground.transform);
                SetReference(so, AssemblerFields.ExitZonePrefab, exitZone.GetComponent<ExitZone>());
                SetReference(so, AssemblerFields.CropCirclePrefab, cropCircle.GetComponent<CropCircle>());
            });

            (map.FogView, map.FogHiding) = CreateFog(room, map.Assembler);
            return map;
        }

        internal static string LibraryReport(MapBlockLibrary library)
        {
            return library.Blocks.Count == 0 ? "The block library is empty: run Zenthium > Map > Create Graybox Blocks."
                : $"The block library has {library.Blocks.Count} blocks.";
        }

        /// <summary>A box 1 m across, its top at Y 0, which the builder scales to the map.</summary>
        private static GameObject BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Map Ground";
            ground.transform.localPosition = new Vector3(0f, -GroundThickness * 0.5f, 0f);
            ground.transform.localScale = new Vector3(1f, GroundThickness, 1f);
            ground.GetComponent<Renderer>().sharedMaterial =
                ModulePrefabBuilder.SaveMaterial("MapGround", GroundColor, ground);
            return ground;
        }

        /// <summary>A 1 m square zone with a see-through blue marker, scaled to the exit zone size by the builder.</summary>
        private static GameObject BuildExitZone()
        {
            var zone = new GameObject("Exit Zone");
            zone.AddComponent<ExitZone>();
            var box = zone.GetComponent<BoxCollider>();
            box.size = new Vector3(1f, ExitZoneHeight, 1f);
            box.center = new Vector3(0f, ExitZoneHeight * 0.5f, 0f);
            AiSandboxSceneCreator.Visual(PrimitiveType.Cube, "Marker", zone.transform,
                new Vector3(0f, MarkerHeight, 0f), new Vector3(1f, MarkerThickness, 1f),
                AiSandboxSceneCreator.TransparentMaterial("ExitZone", ExitZoneColor));
            return zone;
        }

        /// <summary>
        /// A crop circle 1 m across with its see-through disc; the builder gives it the Module World and faction and
        /// scales it to the faction's radius.
        /// </summary>
        private static GameObject BuildCropCircle()
        {
            var circle = new GameObject("Crop Circle");
            circle.AddComponent<CropCircle>();

            // A cylinder primitive is 1 m across and 2 m tall, so it scales by half the thickness.
            AiSandboxSceneCreator.Visual(PrimitiveType.Cylinder, "Disc", circle.transform,
                new Vector3(0f, MarkerHeight, 0f), new Vector3(1f, MarkerThickness * 0.5f, 1f),
                AiSandboxSceneCreator.TransparentMaterial("CropCircle", CropCircleColor));
            return circle;
        }

        /// <summary>
        /// The fog of war, revealed by the player: its logic, the quad it's drawn on (no collider, so aim and sight
        /// rays pass), and the hiding of what's out of sight.
        /// </summary>
        private static (FogOfWarView View, FogHiding Hiding) CreateFog(PlayerSandboxSceneCreator.Room room,
            MapAssembler assembler)
        {
            var fogObject = new GameObject("Fog of War");
            var fog = fogObject.AddComponent<FogOfWar>();
            Wire(fog, so =>
            {
                SetReference(so, FogFields.World, room.World);
                SetReference(so, FogFields.Assembler, assembler);
                SetReference(so, FogFields.Viewer, room.Player);
            });

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Fog Quad";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(fogObject.transform, false);
            var quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            quadRenderer.sharedMaterial = FogMaterial();
            quadRenderer.enabled = false;

            var view = fogObject.AddComponent<FogOfWarView>();
            Wire(view, so =>
            {
                SetReference(so, FogFields.Fog, fog);
                SetReference(so, FogFields.Quad, quadRenderer);
            });
            var hiding = fogObject.AddComponent<FogHiding>();
            Wire(hiding, so =>
            {
                SetReference(so, FogFields.Fog, fog);
                SetReference(so, FogFields.World, room.World);
                SetReference(so, FogFields.Valuables, room.Valuables);
            });
            return (view, hiding);
        }

        /// <summary>Unlit and black, so only the fog texture's alpha shows.</summary>
        private static Material FogMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(UnlitShaderName));
                AssetDatabase.CreateAsset(material, FogMaterialPath);
            }

            material.color = Color.black;
            return AiSandboxSceneCreator.MakeTransparent(material);
        }

        /// <summary>A screen overlay canvas that scales with the screen.</summary>
        internal static Transform CreateOverlayCanvas(string canvasName)
        {
            var canvasObject = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            return canvasObject.transform;
        }

        /// <summary>A dark panel at the top right whose rows stack downwards and size it.</summary>
        internal static Transform CreatePanel(Transform canvas, string panelName)
        {
            var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var panelRect = (RectTransform)panelObject.transform;
            panelRect.SetParent(canvas, false);
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            panelRect.anchoredPosition = PanelPosition;
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
            panelObject.GetComponent<Image>().color = PanelColor;
            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panelObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panelObject.transform;
        }

        /// <summary>A button that shows or hides the fog of war and the hiding of what's out of sight.</summary>
        internal static Button CreateFogButton(Transform row, TMP_DefaultControls.Resources tmpResources,
            FogOfWarView fogView, FogHiding fogHiding)
        {
            Button fogButton = CombatSandboxSceneCreator.CreateButton(row, tmpResources, "Fog: on");
            Wire(fogButton.gameObject.AddComponent<FogToggle>(), so =>
            {
                SetReference(so, FogToggleFields.View, fogView);
                SetReference(so, FogToggleFields.Hiding, fogHiding);
                SetReference(so, FogToggleFields.Button, fogButton);
                SetReference(so, FogToggleFields.Label, fogButton.GetComponentInChildren<TMP_Text>());
            });
            return fogButton;
        }

        /// <summary>The launcher panel at the top right.</summary>
        private static void CreateLauncher(PlayerSandboxSceneCreator.Room room, MapAssembler assembler,
            FactionData repo, EntityData grey, FogOfWarView fogView, FogHiding fogHiding)
        {
            Transform canvas = CreateOverlayCanvas("Map Launcher Canvas");
            Transform panel = CreatePanel(canvas, "Map Launcher");
            GameObject panelObject = panel.gameObject;
            TMP_DefaultControls.Resources tmpResources = CombatSandboxSceneCreator.TmpResources();
            DefaultControls.Resources uiResources = CombatSandboxSceneCreator.UiResources();

            Transform mapRow = CombatSandboxSceneCreator.CreateRow(panel, "Map Row");
            TMP_Dropdown sizeDropdown = CombatSandboxSceneCreator.CreateDropdown(mapRow, tmpResources);
            TMP_Dropdown locationDropdown = CombatSandboxSceneCreator.CreateDropdown(mapRow, tmpResources);

            Transform seedRow = CombatSandboxSceneCreator.CreateRow(panel, "Seed Row");
            TMP_InputField seedInput = CombatSandboxSceneCreator.CreateInput(seedRow, tmpResources, "Seed", "0");
            seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            Button buildButton = CombatSandboxSceneCreator.CreateButton(seedRow, tmpResources, "Build");
            Button newSeedButton = CombatSandboxSceneCreator.CreateButton(seedRow, tmpResources, "New seed");

            Transform missionRow = CombatSandboxSceneCreator.CreateRow(panel, "Mission Row");
            TMP_Dropdown exitsDropdown = CombatSandboxSceneCreator.CreateDropdown(missionRow, tmpResources);
            TMP_Dropdown rankDropdown = CombatSandboxSceneCreator.CreateDropdown(missionRow, tmpResources);
            Toggle cropCirclesToggle =
                CombatSandboxSceneCreator.CreateToggle(missionRow, uiResources, "Crop circles", true);

            Transform spawnRow = CombatSandboxSceneCreator.CreateRow(panel, "Spawn Row");
            Button spawnButton = CombatSandboxSceneCreator.CreateButton(spawnRow, tmpResources, "Spawn Grey");
            CreateFogButton(spawnRow, tmpResources, fogView, fogHiding);

            TMP_Text status = CombatSandboxSceneCreator.CreateText(panel, tmpResources, "Status");

            Wire(panelObject.AddComponent<MapLauncherPanel>(), so =>
            {
                SetReference(so, LauncherFields.Assembler, assembler);
                SetReference(so, LauncherFields.World, room.World);
                SetReference(so, LauncherFields.Player, room.Player);
                SetReference(so, LauncherFields.Faction, repo);
                SetReference(so, LauncherFields.Goon, grey);
                SetReference(so, LauncherFields.SizeDropdown, sizeDropdown);
                SetReference(so, LauncherFields.LocationDropdown, locationDropdown);
                SetReference(so, LauncherFields.ExitsDropdown, exitsDropdown);
                SetReference(so, LauncherFields.RankDropdown, rankDropdown);
                SetReference(so, LauncherFields.SeedInput, seedInput);
                SetReference(so, LauncherFields.CropCirclesToggle, cropCirclesToggle);
                SetReference(so, LauncherFields.BuildButton, buildButton);
                SetReference(so, LauncherFields.NewSeedButton, newSeedButton);
                SetReference(so, LauncherFields.SpawnButton, spawnButton);
                SetReference(so, LauncherFields.Status, status);
            });
        }
    }
}
