using UnityEngine;
using Zenthium.AI;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Tints an AI entity red while a Telegraph module winds up (enemies.md, Other rules: flash red before it hits).
    /// Test visual until art arrives.
    /// </summary>
    public sealed class TelegraphFlashView : MonoBehaviour
    {
        // URP Lit's color property.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private AiBrain brain;
        [SerializeField] private Renderer body;
        [SerializeField] private Color flashColor = new Color(1f, 0.1f, 0.1f);

        private MaterialPropertyBlock block;
        private bool isShown;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            if (isShown && body != null)
            {
                body.SetPropertyBlock(null);
                isShown = false;
            }
        }

        private void LateUpdate()
        {
            if (brain.IsTelegraphing == isShown)
            {
                return;
            }

            isShown = brain.IsTelegraphing;
            if (isShown)
            {
                block.SetColor(BaseColorId, flashColor);
                body.SetPropertyBlock(block);
            }
            else
            {
                body.SetPropertyBlock(null);
            }
        }
    }
}
