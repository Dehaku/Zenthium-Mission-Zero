using TMPro;
using UnityEditor;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Sandbox;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds the prefabs module behaviors spawn (summon body, glyph, smoke cloud, charge, shield, beam) with
    /// placeholder primitive visuals and flat-colored materials, and wires them to a scene's Module World. Visuals
    /// have no colliders, so they never block movement, knockback or sweeps. Real art comes in Milestone 16.
    /// </summary>
    internal static class ModulePrefabBuilder
    {
        private const string ModulesFolderName = "Modules";
        private const string MaterialsFolderName = "Materials";
        private const string ModulesFolder = PlayerSandboxSceneCreator.PrefabFolder + "/" + ModulesFolderName;
        private const string MaterialsFolder = PlayerSandboxSceneCreator.PrefabFolder + "/" + MaterialsFolderName;
        private const string PrefabExtension = ".prefab";
        private const string MaterialExtension = ".mat";

        // The spawn point is an entity's center, 1 m up; ground visuals sit just above the ground.
        private const float GroundOffset = -0.95f;
        private const float FlatThickness = 0.02f;
        private const float SmokeHeight = 1.5f;
        private const float SummonScale = 0.8f;
        private const float ChargeBodySize = 0.4f;
        private const float CountdownHeight = 0.6f;
        private const float CountdownFontSize = 6f;
        private static readonly Vector2 CountdownSize = new Vector2(2f, 1.5f);
        private const float BeamThickness = 0.2f;

        // Matches ShieldZone's default reach, so the visual sits where it blocks.
        private const float ShieldReach = 1.5f;
        private const float ShieldDepth = 0.2f;
        private const float ShieldHeight = 1f;

        private static readonly Color InkColor = new Color(0.15f, 0.1f, 0.3f);
        private static readonly Color GlyphColor = new Color(0.6f, 0.2f, 0.9f);
        private static readonly Color SmokeColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color BlastColor = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color ChargeColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color ShieldColor = new Color(0.3f, 0.7f, 1f);
        private static readonly Color BeamColor = new Color(1f, 0.9f, 0.3f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class WorldFields
        {
            public const string SummonPrefab = "summonPrefab";
            public const string GlyphPrefab = "glyphPrefab";
            public const string SmokePrefab = "smokePrefab";
            public const string ChargePrefab = "chargePrefab";
            public const string ShieldPrefab = "shieldPrefab";
            public const string BeamVisualPrefab = "beamVisualPrefab";
        }

        private static class GlyphFields
        {
            public const string Disc = "disc";
        }

        private static class SmokeFields
        {
            public const string Puff = "puff";
        }

        private static class ChargeViewFields
        {
            public const string Charge = "charge";
            public const string RadiusCircle = "radiusCircle";
            public const string Countdown = "countdown";
        }

        /// <summary>Rebuilds every module prefab and sets them on the Module World.</summary>
        internal static void BuildAndWire(ModuleWorld world, GameDatabase database)
        {
            EnsureFolder(ModulesFolderName, ModulesFolder);
            EnsureFolder(MaterialsFolderName, MaterialsFolder);

            GameObject summon = Save(BuildSummon(database), "Summon");
            GameObject glyph = Save(BuildGlyph(), "Glyph");
            GameObject smoke = Save(BuildSmoke(), "SmokeCloud");
            GameObject charge = Save(BuildCharge(), "Charge");
            GameObject shield = Save(BuildShield(), "Shield");
            GameObject beam = Save(BuildBeam(), "BeamVisual");

            Wire(world, so =>
            {
                SetReference(so, WorldFields.SummonPrefab, summon.GetComponent<EntityBehaviour>());
                SetReference(so, WorldFields.GlyphPrefab, glyph.GetComponent<PlacedGlyph>());
                SetReference(so, WorldFields.SmokePrefab, smoke.GetComponent<SmokeCloud>());
                SetReference(so, WorldFields.ChargePrefab, charge.GetComponent<TimedCharge>());
                SetReference(so, WorldFields.ShieldPrefab, shield.GetComponent<ShieldZone>());
                SetReference(so, WorldFields.BeamVisualPrefab, beam);
            });
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildSummon(GameDatabase database)
        {
            // Its entity, team and modules are set when it's spawned (ModuleWorld.SpawnSummon).
            GameObject body = PlayerSandboxSceneCreator.BuildEntityBody("Summon", database, null, null, Team.Ally);
            body.transform.localScale = Vector3.one * SummonScale;
            body.GetComponent<Renderer>().sharedMaterial = SaveMaterial("Ink", InkColor, body);
            body.AddComponent<EntityDragTarget>();
            body.AddComponent<PlaceholderSummonBrain>();
            return body;
        }

        private static GameObject BuildGlyph()
        {
            var root = new GameObject("Glyph");
            Transform disc = Visual(PrimitiveType.Cylinder, "Disc", root.transform, GlyphColor,
                new Vector3(0f, GroundOffset, 0f), new Vector3(1f, FlatThickness, 1f));
            Wire(root.AddComponent<PlacedGlyph>(), so => SetReference(so, GlyphFields.Disc, disc));
            return root;
        }

        private static GameObject BuildSmoke()
        {
            var root = new GameObject("Smoke Cloud");
            Transform puff = Visual(PrimitiveType.Cylinder, "Puff", root.transform, SmokeColor,
                new Vector3(0f, GroundOffset + SmokeHeight * 0.5f, 0f), new Vector3(1f, SmokeHeight * 0.5f, 1f));
            Wire(root.AddComponent<SmokeCloud>(), so => SetReference(so, SmokeFields.Puff, puff));
            return root;
        }

        private static GameObject BuildCharge()
        {
            var root = new GameObject("Charge");
            var charge = root.AddComponent<TimedCharge>();
            Transform circle = Visual(PrimitiveType.Cylinder, "Blast Radius", root.transform, BlastColor,
                new Vector3(0f, GroundOffset, 0f), new Vector3(1f, FlatThickness, 1f));
            Visual(PrimitiveType.Cube, "Charge Body", root.transform, ChargeColor, Vector3.zero,
                Vector3.one * ChargeBodySize);

            var countdownObject = new GameObject("Countdown", typeof(RectTransform), typeof(TextMeshPro));
            var countdownRect = (RectTransform)countdownObject.transform;
            countdownRect.SetParent(root.transform, false);
            countdownRect.localPosition = new Vector3(0f, CountdownHeight, 0f);

            // Lying flat, facing the overhead camera.
            countdownRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            countdownRect.sizeDelta = CountdownSize;
            var countdown = countdownObject.GetComponent<TextMeshPro>();
            countdown.font = TMP_Settings.defaultFontAsset;
            countdown.fontSize = CountdownFontSize;
            countdown.alignment = TextAlignmentOptions.Center;
            countdown.color = BlastColor;
            countdown.text = string.Empty;

            Wire(root.AddComponent<ChargeView>(), so =>
            {
                SetReference(so, ChargeViewFields.Charge, charge);
                SetReference(so, ChargeViewFields.RadiusCircle, circle);
                SetReference(so, ChargeViewFields.Countdown, countdown);
            });
            return root;
        }

        private static GameObject BuildShield()
        {
            var root = new GameObject("Shield");
            Visual(PrimitiveType.Cube, "Crescent", root.transform, ShieldColor, new Vector3(0f, 0f, ShieldReach),
                new Vector3(ShieldReach * 2f, ShieldHeight, ShieldDepth));
            root.AddComponent<ShieldZone>();
            return root;
        }

        private static GameObject BuildBeam()
        {
            // Stretched along Z to the beam's length when shown (ModuleWorld.ShowBeam).
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Beam Visual";
            Object.DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.localScale = new Vector3(BeamThickness, BeamThickness, 1f);
            beam.GetComponent<Renderer>().sharedMaterial = SaveMaterial("Beam", BeamColor, beam);
            return beam;
        }

        private static Transform Visual(PrimitiveType shape, string objectName, Transform parent, Color color,
            Vector3 localPosition, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(shape);
            visual.name = objectName;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = SaveMaterial(objectName, color, visual);
            return visual.transform;
        }

        /// <summary>A copy of the primitive's default material in a flat color, saved so prefabs can use it.</summary>
        private static Material SaveMaterial(string materialName, Color color, GameObject primitive)
        {
            string path = $"{MaterialsFolder}/{materialName}{MaterialExtension}";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(primitive.GetComponent<Renderer>().sharedMaterial);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Save(GameObject built, string fileName)
        {
            return PlayerSandboxSceneCreator.SaveAsPrefab(built, $"{ModulesFolder}/{fileName}{PrefabExtension}");
        }

        private static void EnsureFolder(string folderName, string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(PlayerSandboxSceneCreator.PrefabFolder, folderName);
            }
        }
    }
}
