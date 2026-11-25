using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A button that reloads the sandbox scene in Play mode, so a test can start over without stopping and starting
    /// Play. Test tool only.
    /// </summary>
    public sealed class SandboxSceneReset : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void OnEnable()
        {
            button.onClick.AddListener(ReloadScene);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ReloadScene);
            }
        }

        private void ReloadScene()
        {
            Scene scene = gameObject.scene;
#if UNITY_EDITOR
            // Sandbox scenes are left out of the build's scene list, which LoadScene needs; the Editor can load by path.
            EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
#endif
        }
    }
}
