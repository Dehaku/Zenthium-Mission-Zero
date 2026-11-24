using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Flat Armor while equipped, from the module's <c>armor</c> value, such as Padded Suit.
    /// </summary>
    public sealed class ArmorPassive : ModuleBehaviour
    {
        public const string ArmorKey = "armor";

        private readonly float armor;
        private Entity armored;
        private ModifierHandle handle;

        public ArmorPassive(ModuleData data, float armor) : base(data)
        {
            this.armor = armor;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Armor +").Append(armor.ToString("0.##", CultureInfo.InvariantCulture)).Append('\n');
        }

        protected override void OnEnabled()
        {
            armored = Owner;
            handle = armored.Defenses.AddArmor(armor);
        }

        protected override void OnDisabled()
        {
            if (armored != null)
            {
                armored.Defenses.RemoveArmor(handle);
                armored = null;
            }
        }
    }
}
