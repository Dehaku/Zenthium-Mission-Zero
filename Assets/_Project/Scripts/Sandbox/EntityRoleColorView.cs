using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Colors an entity's body by its role, so playtests can tell them apart at a glance: the player, allies,
    /// citizens, neutral entities, and enemies by rank tier (goon, Champion, Leader). It follows team changes, such as
    /// a Charmed enemy. Test visual until art arrives. It colors its own copy of the material, so a property block on
    /// top, such as the telegraph flash, still shows and clears back to this color.
    /// </summary>
    public sealed class EntityRoleColorView : MonoBehaviour
    {
        [SerializeField] private EntityBehaviour target;
        [SerializeField] private Renderer body;
        [SerializeField] private Color playerColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color allyColor = new Color(0.3f, 0.9f, 0.9f);
        [SerializeField] private Color civilianColor = new Color(1f, 0.9f, 0.55f);
        [SerializeField] private Color neutralColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color goonColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color championColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color leaderColor = new Color(0.65f, 0.25f, 0.85f);

        private Entity entity;
        private Material material;

        private void OnEnable()
        {
            // The entity may run Awake before or after this, so bind now and on every (re)creation.
            target.OnEntityCreated += Bind;
            if (target.Entity != null)
            {
                Bind(target.Entity);
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.OnEntityCreated -= Bind;
            }

            Unbind();
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            entity = newEntity;
            entity.OnTeamChanged += Apply;
            Apply();
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnTeamChanged -= Apply;
                entity = null;
            }
        }

        private void Apply()
        {
            if (material == null)
            {
                material = body.material;
            }

            material.color = ColorFor(entity);
        }

        private Color ColorFor(Entity colored)
        {
            switch (colored.Team)
            {
                case Team.Player:
                    return playerColor;
                case Team.Ally:
                    return allyColor;
                case Team.Civilian:
                    return civilianColor;
                case Team.Neutral:
                    return neutralColor;
                default:
                    return TierColor(colored.RankTier);
            }
        }

        private Color TierColor(RankTier? tier)
        {
            switch (tier)
            {
                case RankTier.Champion:
                    return championColor;
                case RankTier.Leader:
                    return leaderColor;
                default:
                    return goonColor;
            }
        }
    }
}
