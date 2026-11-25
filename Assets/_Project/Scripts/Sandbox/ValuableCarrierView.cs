using TMPro;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Placeholder presentation for a Valuable carrier (world.md, Carrying): a money bag icon under it with the
    /// number held at its bottom right, growing with each Valuable and hidden at 0, and a "KA-CHING!" callout when it
    /// drops them. The real art and comic callouts come in Milestones 10 and 16.
    /// </summary>
    public sealed class ValuableCarrierView : MonoBehaviour
    {
        private const string Callout = "KA-CHING!";

        [SerializeField] private ValuableCarrier carrier;
        [SerializeField] private TMP_Text icon;
        [SerializeField] private TMP_Text callout;

        [Tooltip("Real seconds the callout stays up.")]
        [SerializeField, Min(0.1f)] private float calloutSeconds = 1.5f;

        private Vector3 iconBaseScale;
        private float calloutLeft;

        private void Awake()
        {
            iconBaseScale = icon.transform.localScale;
            callout.text = Callout;
            callout.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            carrier.OnCountChanged += ShowCount;
            carrier.OnDroppedAll += ShowCallout;
            ShowCount();
        }

        private void OnDisable()
        {
            if (carrier != null)
            {
                carrier.OnCountChanged -= ShowCount;
                carrier.OnDroppedAll -= ShowCallout;
            }
        }

        private void Update()
        {
            if (calloutLeft <= 0f)
            {
                return;
            }

            calloutLeft -= Time.unscaledDeltaTime;
            if (calloutLeft <= 0f)
            {
                callout.gameObject.SetActive(false);
            }
        }

        private void ShowCount()
        {
            int count = carrier.Count;
            icon.gameObject.SetActive(count > 0);
            if (count == 0)
            {
                return;
            }

            icon.SetText("$<size=50%>{0}</size>", count);
            float scale = carrier.Service != null && carrier.Service.World != null
                ? ValuableRules.IconScale(count, carrier.Service.World.Database.ValuableConstants)
                : 1f;
            icon.transform.localScale = iconBaseScale * scale;
        }

        private void ShowCallout()
        {
            callout.gameObject.SetActive(true);
            calloutLeft = calloutSeconds;
        }
    }
}
