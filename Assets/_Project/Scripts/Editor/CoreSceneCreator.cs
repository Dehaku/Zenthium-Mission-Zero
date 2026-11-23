using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenthium.Player;

namespace Zenthium.Editor
{
    /// <summary>
    /// Creates the core scenes and puts them first in the build's scene list. Safe to run again:
    /// existing scenes are left untouched.
    /// </summary>
    internal static class CoreSceneCreator
    {
        private const string MenuPath = "Zenthium/Setup/Create Core Scenes";
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string BootScene = "Boot";
        private const string HqScene = "HQ";
        private const string MissionScene = "Mission";

        // Build order: Boot must be first, since it's the scene a build starts in.
        private static readonly string[] BuildOrder = { BootScene, HqScene, MissionScene };

        [MenuItem(MenuPath)]
        private static void CreateCoreScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CreateSceneIfMissing(BootScene, NewSceneSetup.EmptyScene, null);
            CreateSceneIfMissing(HqScene, NewSceneSetup.DefaultGameObjects, null);
            CreateSceneIfMissing(MissionScene, NewSceneSetup.DefaultGameObjects, AddCameraRig);
            PutCoreScenesFirstInBuild();
        }

        private static void CreateSceneIfMissing(string sceneName, NewSceneSetup setup, System.Action<Scene> configure)
        {
            string path = ScenePath(sceneName);
            if (File.Exists(path))
            {
                Debug.Log($"{sceneName} scene already exists at {path}; left unchanged.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
            configure?.Invoke(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created {path}.");
        }

        private static void AddCameraRig(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out Camera _))
                {
                    root.AddComponent<OverheadCameraRig>();
                    return;
                }
            }

            Debug.LogWarning($"No camera found in the new {scene.name} scene; add Overhead Camera Rig by hand.");
        }

        private static void PutCoreScenesFirstInBuild()
        {
            var corePaths = BuildOrder.Select(ScenePath).ToList();
            var scenes = new List<EditorBuildSettingsScene>(
                corePaths.Select(path => new EditorBuildSettingsScene(path, true)));
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => !corePaths.Contains(s.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Build scene list: " + string.Join(", ", scenes.Select(s => s.path)));
        }

        private static string ScenePath(string sceneName)
        {
            return $"{SceneFolder}/{sceneName}.unity";
        }
    }
}
