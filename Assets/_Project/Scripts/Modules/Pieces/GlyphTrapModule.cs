using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Glyph Trap (modules.md): places a glyph at the aimed point, as far as the range tier. The first opponent to
    /// step within its radius triggers it: every opponent inside takes its effects (a Root, resisted by Will), and
    /// the glyph is gone. One glyph at a time; a new cast replaces it.
    /// </summary>
    public sealed class GlyphTrapModule : ModuleBehaviour
    {
        private readonly HitEffects effects;
        private PlacedGlyph glyph;

        public GlyphTrapModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
            text.Append("Triggers within ").Append(Data.RadiusTier.Value.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" m, once\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            RemoveGlyph();
            glyph = Context.World.SpawnGlyph(aim.PlacementPoint(Context.Position, Data.RangeTier.Value));
            if (glyph != null)
            {
                glyph.Arm(Context, effects, Data.RadiusTier.Value);
            }
        }

        protected override void OnDisabled()
        {
            RemoveGlyph();
        }

        private void RemoveGlyph()
        {
            if (glyph != null)
            {
                Object.Destroy(glyph.gameObject);
            }

            glyph = null;
        }
    }
}
