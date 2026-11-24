using System.Globalization;
using TMPro;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A small in-world label on a wall, door or prop for sandbox scenes: what it is, and its HP or "Broken". It's a
    /// separate object that follows its target, so the target's scale doesn't stretch it. Test tool only.
    /// </summary>
    public sealed class StructureDebugLabel : MonoBehaviour
    {
        private const string NumberFormat = "0.#";

        [SerializeField] private Structure target;
        [SerializeField] private TMP_Text label;

        [Tooltip("What the label calls it, such as \"Wood wall\".")]
        [SerializeField] private string title;

        [Tooltip("Where the label sits relative to its target, in meters.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, 0f);

        private void OnEnable()
        {
            target.OnHealthChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.OnHealthChanged -= Refresh;
            }
        }

        private void LateUpdate()
        {
            if (target != null && target.isActiveAndEnabled)
            {
                transform.position = target.transform.position + offset;
            }
        }

        private void Start()
        {
            // The target configures its HP when it wakes, which may be after this was enabled.
            Refresh();
        }

        private void Refresh()
        {
            if (!target.IsConfigured)
            {
                label.text = title;
                return;
            }

            label.text = target.IsBroken
                ? title + "\nBroken"
                : title + "\n" + target.Hp.ToString(NumberFormat, CultureInfo.InvariantCulture) + " / "
                    + target.MaxHp.ToString(NumberFormat, CultureInfo.InvariantCulture) + " HP";
        }
    }
}
