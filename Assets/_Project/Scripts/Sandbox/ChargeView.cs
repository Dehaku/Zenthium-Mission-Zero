using TMPro;
using UnityEngine;
using Zenthium.Modules;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Placeholder presentation for a placed charge (modules.md, Breaching Charge): a red circle the size of the
    /// blast and a countdown number. The real art comes in Milestone 16.
    /// </summary>
    public sealed class ChargeView : MonoBehaviour
    {
        [SerializeField] private TimedCharge charge;
        [SerializeField] private Transform radiusCircle;
        [SerializeField] private TMP_Text countdown;

        private int shownSeconds = -1;

        private void OnEnable()
        {
            charge.OnArmed += ShowRadius;
            charge.OnFuseChanged += ShowFuse;
        }

        private void OnDisable()
        {
            charge.OnArmed -= ShowRadius;
            charge.OnFuseChanged -= ShowFuse;
        }

        private void ShowRadius(float radius)
        {
            Vector3 scale = radiusCircle.localScale;
            radiusCircle.localScale = new Vector3(radius * 2f, scale.y, radius * 2f);
        }

        private void ShowFuse(float seconds)
        {
            int whole = Mathf.CeilToInt(seconds);
            if (whole != shownSeconds)
            {
                shownSeconds = whole;
                countdown.SetText("{0}", whole);
            }
        }
    }
}
