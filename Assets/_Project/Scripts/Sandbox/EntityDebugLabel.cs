using System.Text;
using TMPro;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A small in-world label over an entity for sandbox scenes: its team, HP, and whether it's Incapacitated,
    /// dragged or Invulnerable. Test tool only; the real HUD comes in Milestone 10.
    /// </summary>
    public sealed class EntityDebugLabel : MonoBehaviour
    {
        [SerializeField] private EntityBehaviour target;
        [SerializeField] private TMP_Text label;

        private readonly StringBuilder text = new StringBuilder();
        private Entity entity;
        private bool isDirty;
        private bool shownDragged;
        private bool shownInvulnerable;

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

        private void LateUpdate()
        {
            if (entity == null)
            {
                return;
            }

            // Dragging and Invulnerable time change without an event, so watch them.
            if (isDirty || entity.IsBeingDragged != shownDragged || entity.IsInvulnerable != shownInvulnerable)
            {
                Refresh();
            }
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            entity = newEntity;
            entity.Health.OnHealthChanged += MarkDirty;
            isDirty = true;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.Health.OnHealthChanged -= MarkDirty;
                entity = null;
            }
        }

        private void MarkDirty(float current, float max)
        {
            isDirty = true;
        }

        private void Refresh()
        {
            isDirty = false;
            shownDragged = entity.IsBeingDragged;
            shownInvulnerable = entity.IsInvulnerable;

            text.Clear();
            text.Append(entity.Team).Append('\n');

            // Rounded up, so a sliver of HP never reads as 0.
            text.Append("HP ").Append(Mathf.CeilToInt(entity.Health.Current))
                .Append(" / ").Append(Mathf.CeilToInt(entity.Health.Max));
            if (entity.IsIncapacitated)
            {
                text.Append("\nIncapacitated");
            }

            if (shownDragged)
            {
                text.Append("\nDragged");
            }

            if (shownInvulnerable)
            {
                text.Append("\nInvulnerable");
            }

            label.SetText(text);
        }
    }
}
