using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Zenthium.Missions
{
    /// <summary>Reloads the scene it's in, which starts the pending mission or shows the launcher.</summary>
    public static class MissionSceneLoader
    {
        public static void ReloadActive()
        {
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // The Editor loads by path, so the scene needn't be in the build's scene list while testing.
            EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
            else
            {
                Debug.LogError($"{scene.name} isn't in the build's scene list, so it can't be reloaded.");
            }
#endif
        }
    }
}
