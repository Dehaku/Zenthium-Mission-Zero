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
    /// Builds Sandbox_AI (Milestones 6a and 6b) on the World sandbox's 60 m room: the Wood house and the L-shaped Stone
    /// shop, cover that hides the squads and the turret from the player's start, two squads patrolling sockets (melee
    /// goons with Speed Punch and their Leader; ranged goons with Energy Blaster, one taking loot off the map, and one
    /// with Cannons, a Telegraph module), a goon locked in the Stone shop (its locked door is Wood here) to test
    /// breaking out through the door, a Heavy Turret running the AI with a Stubborn citizen beside it, a Scared and
    /// a downed citizen, two stashes of Valuables and the lost counter, the map's bounds, the hero select and
    /// MoreInfo panels, an enemy friendly fire toggle, and a baked NavMesh. Goons are Cowards, except the Leader and
    /// the locked-in goon. Each goon shows its awareness bar, "!", alert circle, telegraph flash and AI state; each
    /// citizen its kind.
    /// </summary>
    internal static class AiSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create AI Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_AI.unity";
        private const string NavMeshPath = "Assets/_Project/Scenes/Sandbox_AI_NavMesh.asset";

        private const float RoomSize = 60f;
        private const float Height = PlayerSandboxSceneCreator.CapsuleHalfHeight;

        // Cover between the player's start and the squads to the north, and the turret to the south-west.
        private static readonly Vector3 NorthCoverPosition = new Vector3(0f, 1f, 5f);
        private static readonly Vector3 NorthCoverSize = new Vector3(16f, 2f, 0.5f);
        private static readonly Vector3 SouthCoverPosition = new Vector3(-14f, 1f, -14f);
        private static readonly Vector3 SouthCoverSize = new Vector3(4f, 2f, 4f);

        private static readonly Vector3[] MeleeSockets =
        {
            new Vector3(12f, 0f, 26f),
            new Vector3(25f, 0f, 25f),
            new Vector3(25f, 0f, 20f),
        };

        private static readonly Vector3[] RangedSockets =
        {
            new Vector3(-3f, 0f, 25f),
            new Vector3(4f, 0f, 18f),
            new Vector3(-6f, 0f, 14f),
        };

        private static readonly Vector3[] MeleeGoonPositions =
        {
            new Vector3(11f, Height, 25f),
            new Vector3(13f, Height, 25f),
            new Vector3(12f, Height, 27f),
        };

        private static readonly Vector3[] RangedGoonPositions =
        {
            new Vector3(-4f, Height, 24f),
            new Vector3(-2f, Height, 24f),
        };

        private static readonly Vector3 TelegraphGoonPosition = new Vector3(-3f, Height, 26f);

        // In the Stone shop's back room, by its west window: it sees out, but its only way out is the locked door.
        private static readonly Vector3 DoorTestGoonPosition = new Vector3(-19f, Height, 18f);
        private static readonly Vector3 TurretPosition = new Vector3(-25f, Height, -25f);
        private static readonly Vector3 StubbornCitizenPosition = new Vector3(-21f, Height, -25f);

        // Milestone 6b. The Leader patrols with Squad A, so the other goons have someone to hand their loot to.
        private static readonly Vector3 LeaderPosition = new Vector3(14f, Height, 27f);

        // Beside Squad B's first socket: idle goons see it and loot it at once.
        private static readonly Vector3 SquadStashPosition = new Vector3(-7f, BagHeight, 27f);
        private const int SquadStashBags = 3;

        // Over 15 m from every patrol, so goons only spot it once they're aware, in the middle of a fight.
        private static readonly Vector3 FightStashPosition = new Vector3(4f, BagHeight, -5f);
        private const int FightStashBags = 4;
        private const float BagHeight = 0.3f;
        private const float BagSpacing = 0.8f;

        private static readonly Vector3 ScaredCitizenPosition = new Vector3(2f, Height, -14f);
        private static readonly Vector3 DownedCitizenPosition = new Vector3(-9f, Height, -6f);
        private const int ScaredCitizenValuables = 2;
        private const int DownedCitizenValuables = 1;
        private const string EntityTagsField = "tags";
        private const string StartingValuablesField = "startingValuables";
        private const string BagServiceField = "service";

        // Goon visuals, in the goon's local space: the bar above the debug label (screen-up is +Z), the "!" above
        // that, the state under the goon, and the alert circle just above the ground.
        private static readonly Vector3 BarPosition = new Vector3(0f, 1.2f, 2.1f);
        private static readonly Vector3 BarSize = new Vector3(1.4f, 0.05f, 0.18f);
        private static readonly Vector3 ExclaimPosition = new Vector3(0f, 1.3f, 2.9f);
        private static readonly Vector3 StateLabelPosition = new Vector3(0f, 1.1f, -2f);
        private static readonly Vector2 TextBox = new Vector2(4f, 1f);
        private const float ExclaimFontSize = 8f;
        private const float StateFontSize = 3f;
        private const float CircleHeight = -0.97f;
        private const float CircleThickness = 0.01f;
        private static readonly Quaternion FacingCamera = Quaternion.Euler(90f, 0f, 0f);

        private static readonly Color BarBackColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color BarFillColor = new Color(1f, 0.8f, 0.1f);
        private static readonly Color ExclaimColor = new Color(1f, 0.85f, 0.1f);
        private static readonly Color CircleColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        private static readonly Vector2 ToggleButtonPosition = new Vector2(-12f, 68f);
        private static readonly Color ToggleButtonColor = new Color(0.15f, 0.3f, 0.6f, 0.9f);
        private static readonly Vector2 CanvasReferenceResolution = new Vector2(1920f, 1080f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class BrainFields
        {
            public const string Squad = "squad";
            public const string MapBounds = "mapBounds";
        }

        private static class SquadFields
        {
            public const string Clock = "clock";
            public const string Sockets = "sockets";
        }

        private static class SocketFields
        {
            public const string Options = "options";
            public const string Content = "content";
            public const string Weight = "weight";
        }

        private static class AwarenessViewFields
        {
            public const string Brain = "brain";
            public const string Bar = "bar";
            public const string BarFill = "barFill";
            public const string Exclaim = "exclaim";
            public const string AlertCircle = "alertCircle";
        }

        private static class FlashFields
        {
            public const string Brain = "brain";
            public const string Body = "body";
        }

        private static class StateLabelFields
        {
            public const string Brain = "brain";
            public const string Label = "label";
            public const string Looter = "looter";
        }

        private static class CitizenFields
        {
            public const string StartingKind = "startingKind";
        }

        private static class CitizenLabelFields
        {
            public const string Citizen = "citizen";
            public const string Label = "label";
        }

        private static class MapBoundsFields
        {
            public const string Size = "size";
        }

        private static class ToggleFields
        {
            public const string CombatRules = "combatRules";
            public const string Button = "button";
            public const string Label = "label";
        }

        // URP Lit's settings for a see-through surface.
        private static class UrpLit
        {
            public const string Surface = "_Surface";
            public const string SourceBlend = "_SrcBlend";
            public const string DestinationBlend = "_DstBlend";
            public const string ZWrite = "_ZWrite";
            public const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";
            public const string RenderTypeTag = "RenderType";
            public const string TransparentTag = "Transparent";
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxAi()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room, RoomSize))
            {
                return;
            }

            GameDatabase database = room.Database;
            if (!database.Modules.TryGet(DataIds.Modules.SpeedPunch, out ModuleData speedPunch)
                || !database.Modules.TryGet(DataIds.Modules.EnergyBlaster, out ModuleData energyBlaster)
                || !database.Modules.TryGet(DataIds.Modules.Cannons, out ModuleData cannons)
                || !database.Modules.TryGet(DataIds.Modules.HeavyGun, out ModuleData heavyGun)
                || !database.Entities.TryGet(DataIds.Entities.HeavyTurret, out EntityData turretData)
                || !database.Entities.TryGet(DataIds.Entities.GleebPeep, out EntityData leaderData))
            {
                Debug.LogError(
                    $"The game data is missing a module, the Heavy Turret or GleebPeep; {ScenePath} wasn't written.");
                return;
            }

            // Matches the hero select's slots dropdown, which starts at all slots.
            Wire(room.Player, so => Find(so, CombatSandboxSceneCreator.EntityFields.UnlockedSlots).intValue =
                database.ModuleConstants.SlotsPerType);

            // The room's dummies would stand in the middle as targets; the AI's own goons replace them.
            foreach (EntityBehaviour dummy in room.Dummies)
            {
                Object.DestroyImmediate(dummy.gameObject);
            }

            room.Dummies.Clear();

            // A Wood door (100 HP), so the goon locked inside breaks out in a reasonable time.
            WorldSandboxSceneCreator.BuildTestBuildings(room, StructureMaterial.Wood);
            PlayerSandboxSceneCreator.CreateBlock(room.Geometry, "North Cover", NorthCoverPosition, NorthCoverSize);
            PlayerSandboxSceneCreator.CreateBlock(room.Geometry, "South Cover", SouthCoverPosition, SouthCoverSize);

            // The room's walls stand on its edge, so goons escape within the bounds' margin of them.
            var mapBounds = new GameObject("Map Bounds").AddComponent<MapBounds>();
            Wire(mapBounds, so => Find(so, MapBoundsFields.Size).vector2Value = new Vector2(RoomSize, RoomSize));

            AiSquad melee = CreateSquad(room, "Squad A (melee)", MeleeSockets);
            AiSquad ranged = CreateSquad(room, "Squad B (ranged)", RangedSockets);
            EntityBehaviour firstGoon = null;
            for (int i = 0; i < MeleeGoonPositions.Length; i++)
            {
                EntityBehaviour goon = PlaceGoon(room, $"Melee Goon {i + 1} (Speed Punch)", MeleeGoonPositions[i],
                    speedPunch, melee, mapBounds, EntityTag.Coward);
                firstGoon = firstGoon != null ? firstGoon : goon;
            }

            EntityBehaviour leader = PlaceGoon(room, "Leader (GleebPeep, Speed Punch)", LeaderPosition, speedPunch,
                melee, mapBounds);
            Wire(leader, so =>
                SetReference(so, CombatSandboxSceneCreator.EntityFields.FactionEntity, leaderData));

            PlaceGoon(room, "Ranged Goon 1 (Energy Blaster)", RangedGoonPositions[0], energyBlaster, ranged, mapBounds,
                EntityTag.Coward);
            PlaceGoon(room, "Ranged Goon 2 (Energy Blaster, loot off the map)", RangedGoonPositions[1], energyBlaster,
                ranged, mapBounds, EntityTag.Coward, EntityTag.LootDestinationMapEdge);
            PlaceGoon(room, "Telegraph Goon (Cannons)", TelegraphGoonPosition, cannons, ranged, mapBounds,
                EntityTag.Coward);
            PlaceGoon(room, "Door Test Goon (locked in the shop)", DoorTestGoonPosition, speedPunch, null, mapBounds);

            PlaceBags(room, "Squad B Stash", SquadStashPosition, SquadStashBags);
            PlaceBags(room, "Fight Stash", FightStashPosition, FightStashBags);
            WorldSandboxSceneCreator.CreateLostCounter(room.Valuables);

            EntityBehaviour turret = WorldSandboxSceneCreator.PlaceTurret(room, turretData, heavyGun, TurretPosition, null);
            AddAiViews(turret.GetComponent<AiBrain>());
            PlaceCitizen(room, "Stubborn Citizen (by the turret)", StubbornCitizenPosition, CitizenKind.Stubborn, 0);
            PlaceCitizen(room, $"Scared Citizen ({ScaredCitizenValuables} Valuables)", ScaredCitizenPosition,
                CitizenKind.Scared, ScaredCitizenValuables);
            PlaceCitizen(room, $"Downed Citizen ({DownedCitizenValuables} Valuable)", DownedCitizenPosition,
                CitizenKind.Incapacitated, DownedCitizenValuables);

            CombatSandboxSceneCreator.BindDebugPanel(room.DebugPanel, room.Player, firstGoon);
            ModulesSandboxSceneCreator.CreateModulesCanvas(room);
            CreateFriendlyFireToggle(room);
            WorldSandboxSceneCreator.BakeNavMesh(room.Geometry, NavMeshPath);

            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath} and {NavMeshPath}.");
        }

        internal static AiSquad CreateSquad(PlayerSandboxSceneCreator.Room room, string squadName, Vector3[] socketPositions)
        {
            var squad = new GameObject(squadName).AddComponent<AiSquad>();
            var sockets = new MapSocket[socketPositions.Length];
            for (int i = 0; i < socketPositions.Length; i++)
            {
                var socketObject = new GameObject($"Patrol Socket {i + 1}");
                socketObject.transform.SetParent(squad.transform, false);
                socketObject.transform.position = socketPositions[i];
                sockets[i] = socketObject.AddComponent<MapSocket>();
                Wire(sockets[i], so =>
                {
                    SerializedProperty options = Find(so, SocketFields.Options);
                    options.arraySize = 1;
                    SerializedProperty option = options.GetArrayElementAtIndex(0);
                    option.FindPropertyRelative(SocketFields.Content).enumValueIndex = (int)SocketContent.Goon;
                    option.FindPropertyRelative(SocketFields.Weight).floatValue = 1f;
                });
            }

            Wire(squad, so =>
            {
                SetReference(so, SquadFields.Clock, room.Clock);
                SerializedProperty list = Find(so, SquadFields.Sockets);
                list.arraySize = sockets.Length;
                for (int i = 0; i < sockets.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = sockets[i];
                }
            });
            return squad;
        }

        /// <summary>
        /// A training dummy turned goon: one module, its tags, the AI with its squad and the map's edge, looting,
        /// Charming's conversion, and the AI views.
        /// </summary>
        private static EntityBehaviour PlaceGoon(PlayerSandboxSceneCreator.Room room, string goonName, Vector3 position,
            ModuleData module, AiSquad squad, MapBounds mapBounds, params EntityTag[] tags)
        {
            EntityBehaviour goon = PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, position, room);
            goon.name = goonName;
            Wire(goon, so =>
            {
                SerializedProperty modules = Find(so, CombatSandboxSceneCreator.EntityFields.Modules);
                modules.arraySize = 1;
                modules.GetArrayElementAtIndex(0).objectReferenceValue = module;
                SerializedProperty tagList = Find(so, EntityTagsField);
                tagList.arraySize = tags.Length;
                for (int i = 0; i < tags.Length; i++)
                {
                    tagList.GetArrayElementAtIndex(i).enumValueIndex = (int)tags[i];
                }
            });

            AddGoonAi(goon.gameObject, squad, mapBounds);
            return goon;
        }

        /// <summary>The AI with its squad and the map's edge, looting, Charming's conversion, and the AI views.</summary>
        /// <param name="squad">Null for none; so is <paramref name="mapBounds"/>, such as on a prefab.</param>
        internal static AiBrain AddGoonAi(GameObject goon, AiSquad squad, MapBounds mapBounds)
        {
            goon.AddComponent<AiNavigator>();
            goon.AddComponent<AiLooter>();
            goon.AddComponent<CharmConversion>();
            var brain = goon.AddComponent<AiBrain>();
            Wire(brain, so =>
            {
                SetReference(so, BrainFields.Squad, squad);
                SetReference(so, BrainFields.MapBounds, mapBounds);
            });
            AddAiViews(brain);
            return brain;
        }

        /// <summary>A training dummy turned citizen: its kind, the Valuables it carries, and a label.</summary>
        internal static void PlaceCitizen(PlayerSandboxSceneCreator.Room room, string citizenName, Vector3 position,
            CitizenKind kind, int valuables)
        {
            EntityBehaviour citizen = PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, position, room);
            citizen.name = citizenName;
            Wire(citizen, so => Find(so, CombatSandboxSceneCreator.EntityFields.Team).enumValueIndex =
                (int)Team.Civilian);
            Wire(citizen.GetComponent<ValuableCarrier>(), so =>
                Find(so, StartingValuablesField).intValue = valuables);

            CitizenBrain brain = AddCitizenParts(citizen.gameObject);
            Wire(brain, so => Find(so, CitizenFields.StartingKind).enumValueIndex = (int)kind);
        }

        /// <summary>A citizen's navigation, brain and state label, on a scene body or a prefab's.</summary>
        internal static CitizenBrain AddCitizenParts(GameObject body)
        {
            body.AddComponent<AiNavigator>();
            var brain = body.AddComponent<CitizenBrain>();
            TextMeshPro label = FlatText(body.transform, "Citizen State", StateLabelPosition, StateFontSize,
                Color.white);
            Wire(label.gameObject.AddComponent<CitizenStateLabel>(), so =>
            {
                SetReference(so, CitizenLabelFields.Citizen, brain);
                SetReference(so, CitizenLabelFields.Label, label);
            });
            return brain;
        }

        /// <summary>A stash of loose Valuables around a point.</summary>
        internal static void PlaceBags(PlayerSandboxSceneCreator.Room room, string stashName, Vector3 center, int count)
        {
            var bagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSandboxSceneCreator.ValuableBagPrefabPath);
            var root = new GameObject(stashName).transform;
            for (int i = 0; i < count; i++)
            {
                var bag = (GameObject)PrefabUtility.InstantiatePrefab(bagPrefab, root);
                bag.transform.position = center + new Vector3(i % 2 * BagSpacing, 0f, i / 2 * BagSpacing);
                Wire(bag.GetComponent<ValuableBag>(), so => SetReference(so, BagServiceField, room.Valuables));
            }
        }

        private static void AddAiViews(AiBrain brain)
        {
            Transform root = brain.transform;

            GameObject bar = new GameObject("Awareness Bar");
            bar.transform.SetParent(root, false);
            bar.transform.localPosition = BarPosition;
            Visual(PrimitiveType.Cube, "Back", bar.transform, Vector3.zero, BarSize,
                ModuleMaterial("AwarenessBarBack", BarBackColor));
            Transform fill = Visual(PrimitiveType.Cube, "Fill", bar.transform, Vector3.up * BarSize.y, BarSize,
                ModuleMaterial("AwarenessBarFill", BarFillColor));
            bar.SetActive(false);

            TextMeshPro exclaim = FlatText(root, "Exclaim", ExclaimPosition, ExclaimFontSize, ExclaimColor);
            exclaim.text = "!";
            exclaim.fontStyle = FontStyles.Bold;
            exclaim.gameObject.SetActive(false);

            Transform circle = Visual(PrimitiveType.Cylinder, "Alert Circle", root, new Vector3(0f, CircleHeight, 0f),
                new Vector3(1f, CircleThickness, 1f), TransparentMaterial("AlertCircle", CircleColor));
            circle.gameObject.SetActive(false);

            Wire(brain.gameObject.AddComponent<AwarenessView>(), so =>
            {
                SetReference(so, AwarenessViewFields.Brain, brain);
                SetReference(so, AwarenessViewFields.Bar, bar);
                SetReference(so, AwarenessViewFields.BarFill, fill);
                SetReference(so, AwarenessViewFields.Exclaim, exclaim.gameObject);
                SetReference(so, AwarenessViewFields.AlertCircle, circle);
            });

            Wire(brain.gameObject.AddComponent<TelegraphFlashView>(), so =>
            {
                SetReference(so, FlashFields.Brain, brain);
                SetReference(so, FlashFields.Body, brain.GetComponent<Renderer>());
            });

            TextMeshPro state = FlatText(root, "AI State", StateLabelPosition, StateFontSize, Color.white);
            Wire(state.gameObject.AddComponent<AiStateLabel>(), so =>
            {
                SetReference(so, StateLabelFields.Brain, brain);
                SetReference(so, StateLabelFields.Label, state);
                SetReference(so, StateLabelFields.Looter, brain.GetComponent<AiLooter>());
            });
        }

        /// <summary>The dev toggle for enemy friendly fire, above the Reset scene button.</summary>
        private static void CreateFriendlyFireToggle(PlayerSandboxSceneCreator.Room room)
        {
            var canvasObject = new GameObject("AI Sandbox Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            Button button = SandboxHudSetup.CreateButton(canvasObject.transform, "Enemy Friendly Fire Button",
                "Enemy FF: on", ToggleButtonPosition, ToggleButtonColor, out TMP_Text label);
            Wire(button.gameObject.AddComponent<EnemyFriendlyFireToggle>(), so =>
            {
                SetReference(so, ToggleFields.CombatRules, room.CombatRules);
                SetReference(so, ToggleFields.Button, button);
                SetReference(so, ToggleFields.Label, label);
            });
        }

        /// <summary>A primitive with no collider (it would join the entity's body), in the parent's space.</summary>
        internal static Transform Visual(PrimitiveType type, string objectName, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = objectName;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return visual.transform;
        }

        internal static TextMeshPro FlatText(Transform parent, string objectName, Vector3 localPosition, float fontSize,
            Color color)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshPro));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.localPosition = localPosition;
            rect.localRotation = FacingCamera;
            rect.sizeDelta = TextBox;
            var text = textObject.GetComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.text = string.Empty;
            return text;
        }

        internal static Material ModuleMaterial(string materialName, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material material = ModulePrefabBuilder.SaveMaterial(materialName, color, primitive);
            Object.DestroyImmediate(primitive);
            return material;
        }

        /// <summary>A saved flat-color material that lets its alpha show through (URP Lit, transparent surface).</summary>
        internal static Material TransparentMaterial(string materialName, Color color)
        {
            return MakeTransparent(ModuleMaterial(materialName, color));
        }

        /// <summary>Switches a saved URP Lit or Unlit material to a transparent surface that lets its alpha show through.</summary>
        internal static Material MakeTransparent(Material material)
        {
            material.SetFloat(UrpLit.Surface, 1f);
            material.SetFloat(UrpLit.SourceBlend, (float)BlendMode.SrcAlpha);
            material.SetFloat(UrpLit.DestinationBlend, (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat(UrpLit.ZWrite, 0f);
            material.SetOverrideTag(UrpLit.RenderTypeTag, UrpLit.TransparentTag);
            material.EnableKeyword(UrpLit.TransparentKeyword);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
