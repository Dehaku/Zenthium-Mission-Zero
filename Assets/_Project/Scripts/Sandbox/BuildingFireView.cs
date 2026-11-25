using System.Collections.Generic;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Placeholder presentation for a building's fire (world.md, Fire): its walls, windows and doors turn orange while
    /// it burns and black once burned out, and take their own colors back when it's put out. Real fire effects come in
    /// Milestone 16.
    /// </summary>
    public sealed class BuildingFireView : MonoBehaviour
    {
        [SerializeField] private Building building;
        [SerializeField] private Color burningColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color burnedOutColor = new Color(0.1f, 0.1f, 0.1f);

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Color> ownColors = new List<Color>();

        private void Start()
        {
            // After the building wakes, so its parts are known.
            foreach (BuildingPart part in building.Parts)
            {
                if (part.TryGetComponent(out Renderer visual))
                {
                    renderers.Add(visual);
                    ownColors.Add(visual.material.color);
                }
            }

            Show();
        }

        private void OnEnable()
        {
            building.OnFireStateChanged += Show;
        }

        private void OnDisable()
        {
            if (building != null)
            {
                building.OnFireStateChanged -= Show;
            }
        }

        private void Show()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                switch (building.FireState)
                {
                    case BuildingFireState.Burning:
                        renderers[i].material.color = burningColor;
                        break;
                    case BuildingFireState.BurnedOut:
                        renderers[i].material.color = burnedOutColor;
                        break;
                    default:
                        renderers[i].material.color = ownColors[i];
                        break;
                }
            }
        }
    }
}
