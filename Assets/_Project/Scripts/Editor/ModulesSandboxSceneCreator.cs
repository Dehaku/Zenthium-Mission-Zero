using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Player;
using Zenthium.Sandbox;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_Modules: the Player sandbox's room, plus a turret dummy that fires Energy Blaster at the player,
    /// a civilian dummy, a hero select panel and the MoreInfo view. Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class ModulesSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Modules Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Modules.unity";

        private static readonly Vector3 TurretPosition =
            new Vector3(10f, PlayerSandboxSceneCreator.CapsuleHalfHeight, 10f);
        private static readonly Vector3 CivilianPosition =
            new Vector3(-10f, PlayerSandboxSceneCreator.CapsuleHalfHeight, -10f);

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        // The right-hand 500-wide column of the sandbox layout (see SandboxHudSetup): hero select at the top,
        // MoreInfo below it.
        private const float RightColumnWidth = 500f;
        private const float InfoPanelHeight = 800f;
        private const int PanelPadding = 8;
        private const float Spacing = 4f;
        private const float InfoTextSize = 15f;
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class HeroSelectFields
        {
            public const string Database = "database";
            public const string Player = "player";
            public const string HeroDropdown = "heroDropdown";
            public const string UnlockedSlotsDropdown = "unlockedSlotsDropdown";
            public const string TestModuleDropdown = "testModuleDropdown";
            public const string ApplyButton = "applyButton";
        }

        private static class InfoViewFields
        {
            public const string Controller = "controller";
            public const string Modules = "modules";
            public const string Panel = "panel";
            public const string Label = "label";
        }

        private static class AutoCasterFields
        {
            public const string Target = "target";
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxModules()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room))
            {
                return;
            }

            if (!room.Database.Modules.TryGet(DataIds.Modules.EnergyBlaster, out ModuleData energyBlaster))
            {
                Debug.LogError($"The game data has no {DataIds.Modules.EnergyBlaster}; {ScenePath} wasn't written.");
                return;
            }

            // Matches the hero select's slots dropdown, which starts at all slots.
            Wire(room.Player, so => Find(so, CombatSandboxSceneCreator.EntityFields.UnlockedSlots).intValue =
                room.Database.ModuleConstants.SlotsPerType);

            EntityBehaviour turret = PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, TurretPosition, room);
            turret.name = "Turret Dummy (Energy Blaster)";
            Wire(turret, so =>
            {
                SerializedProperty modules = Find(so, CombatSandboxSceneCreator.EntityFields.Modules);
                modules.arraySize = 1;
                modules.GetArrayElementAtIndex(0).objectReferenceValue = energyBlaster;
            });
            Wire(turret.gameObject.AddComponent<SandboxAutoCaster>(),
                so => SetReference(so, AutoCasterFields.Target, room.Player));

            // Here the panel inspects and hits the player (to test Defiance, Taunt and Armor), with the turret attacking.
            CombatSandboxSceneCreator.BindDebugPanel(room.DebugPanel, room.Player, turret);

            EntityBehaviour civilian = PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, CivilianPosition, room);
            civilian.name = "Civilian Dummy";
            Wire(civilian, so =>
                Find(so, CombatSandboxSceneCreator.EntityFields.Team).enumValueIndex = (int)Team.Civilian);

            CreateModulesCanvas(room);

            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath}.");
        }

        private static void CreateModulesCanvas(PlayerSandboxSceneCreator.Room room)
        {
            var canvasObject = new GameObject("Modules Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            // Shrink: the whole reference layout always fits on screen, whatever the Game view's aspect.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            TMP_DefaultControls.Resources tmp = CombatSandboxSceneCreator.TmpResources();

            Transform heroPanel = CreatePanel(canvasObject.transform, "Hero Select Panel", Vector2.one, RightColumnWidth);
            CombatSandboxSceneCreator.CreateText(heroPanel, tmp, "Title").text = "Hero select (Apply rebuilds the player)";
            TMP_Dropdown heroDropdown = CombatSandboxSceneCreator.CreateDropdown(
                CombatSandboxSceneCreator.CreateRow(heroPanel, "Hero Row"), tmp);
            Transform unlockRow = CombatSandboxSceneCreator.CreateRow(heroPanel, "Unlock Row");
            CombatSandboxSceneCreator.CreateText(unlockRow, tmp, "Unlock Label").text = "Unlocked slots";
            TMP_Dropdown unlockedSlots = CombatSandboxSceneCreator.CreateDropdown(unlockRow, tmp);
            TMP_Dropdown testModule = CombatSandboxSceneCreator.CreateDropdown(
                CombatSandboxSceneCreator.CreateRow(heroPanel, "Test Module Row"), tmp);
            Button apply = CombatSandboxSceneCreator.CreateButton(
                CombatSandboxSceneCreator.CreateRow(heroPanel, "Apply Row"), tmp, "Apply");

            Wire(heroPanel.gameObject.AddComponent<HeroSelectPanel>(), so =>
            {
                SetReference(so, HeroSelectFields.Database, room.Database);
                SetReference(so, HeroSelectFields.Player, room.Player);
                SetReference(so, HeroSelectFields.HeroDropdown, heroDropdown);
                SetReference(so, HeroSelectFields.UnlockedSlotsDropdown, unlockedSlots);
                SetReference(so, HeroSelectFields.TestModuleDropdown, testModule);
                SetReference(so, HeroSelectFields.ApplyButton, apply);
            });

            // The view sits on the canvas, not the panel, since it hides the panel while MoreInfo isn't held.
            Transform infoPanel = CreatePanel(canvasObject.transform, "Module Info Panel", Vector2.right,
                RightColumnWidth);
            var infoRect = (RectTransform)infoPanel;
            // A fixed size the text fills, so auto-sizing shrinks long info instead of growing the panel off screen.
            infoPanel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            infoPanel.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = true;
            infoRect.sizeDelta = new Vector2(RightColumnWidth, InfoPanelHeight);
            TMP_Text infoText = CombatSandboxSceneCreator.CreateText(infoPanel, tmp, "Info Text");
            infoText.fontSize = InfoTextSize;
            infoText.enableAutoSizing = true;
            infoText.fontSizeMax = InfoTextSize;

            Wire(canvasObject.AddComponent<ModuleInfoView>(), so =>
            {
                SetReference(so, InfoViewFields.Controller, room.Player.GetComponent<PlayerController>());
                SetReference(so, InfoViewFields.Modules, room.Player.GetComponent<EntityModules>());
                SetReference(so, InfoViewFields.Panel, infoPanel.gameObject);
                SetReference(so, InfoViewFields.Label, infoText);
            });
        }

        /// <param name="anchor">The screen point the panel hangs from, which is also its pivot.</param>
        private static Transform CreatePanel(Transform canvas, string panelName, Vector2 anchor, float width)
        {
            var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var rect = (RectTransform)panelObject.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(width, 0f);
            panelObject.GetComponent<Image>().color = PanelColor;
            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panelObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }
    }
}
