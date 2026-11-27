using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// Hides what moves outside the player's sight (maps.md, Visibility; the user's decision for 8b): enemies, neutral
    /// entities, citizens and loose Valuables. The player's own side always shows. It uses
    /// <c>Renderer.forceRenderingOff</c>, not <c>enabled</c>, so views that show and hide their own parts keep working;
    /// world-space canvases such as labels are switched off. Switched off, it shows everything again.
    /// </summary>
    public sealed class FogHiding : MonoBehaviour
    {
        [SerializeField] private FogOfWar fog;
        [SerializeField] private ModuleWorld world;
        [SerializeField] private ValuableService valuables;

        /// <summary>The visual parts of one hidden object, found once.</summary>
        private sealed class Parts
        {
            public Renderer[] Renderers;
            public Canvas[] Canvases;
            public bool IsHidden;
        }

        private readonly Dictionary<Component, Parts> parts = new Dictionary<Component, Parts>();
        private readonly List<EntityBehaviour> entities = new List<EntityBehaviour>();
        private readonly List<Component> stale = new List<Component>();

        private void OnEnable()
        {
            fog.OnUpdated += Refresh;
        }

        private void OnDisable()
        {
            if (fog != null)
            {
                fog.OnUpdated -= Refresh;
            }

            foreach (KeyValuePair<Component, Parts> pair in parts)
            {
                if (pair.Key != null)
                {
                    SetHidden(pair.Value, false);
                }
            }

            parts.Clear();
        }

        private void Refresh()
        {
            world.CollectAll(entities);
            foreach (EntityBehaviour body in entities)
            {
                Entity entity = body.Entity;
                bool hides = body != fog.Viewer && entity != null && !IsPlayersSide(entity.Team);
                Apply(body, hides && !fog.IsVisible(body.transform.position));
            }

            if (valuables != null)
            {
                foreach (ValuableBag bag in valuables.Bags)
                {
                    if (bag != null)
                    {
                        Apply(bag, !fog.IsVisible(bag.transform.position));
                    }
                }
            }

            ForgetDestroyed();
        }

        private static bool IsPlayersSide(Team team)
        {
            return team == Team.Player || team == Team.Ally;
        }

        private void Apply(Component target, bool hidden)
        {
            if (!parts.TryGetValue(target, out Parts found))
            {
                if (!hidden)
                {
                    return;
                }

                found = new Parts
                {
                    Renderers = target.GetComponentsInChildren<Renderer>(true),
                    Canvases = target.GetComponentsInChildren<Canvas>(true),
                };
                parts.Add(target, found);
            }

            if (found.IsHidden != hidden)
            {
                SetHidden(found, hidden);
            }
        }

        private static void SetHidden(Parts found, bool hidden)
        {
            found.IsHidden = hidden;
            foreach (Renderer part in found.Renderers)
            {
                if (part != null)
                {
                    part.forceRenderingOff = hidden;
                }
            }

            foreach (Canvas canvas in found.Canvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = !hidden;
                }
            }
        }

        private void ForgetDestroyed()
        {
            stale.Clear();
            foreach (Component key in parts.Keys)
            {
                if (key == null)
                {
                    stale.Add(key);
                }
            }

            foreach (Component key in stale)
            {
                parts.Remove(key);
            }
        }
    }
}
