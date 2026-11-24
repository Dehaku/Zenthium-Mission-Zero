using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Editor.DataImport;

namespace Zenthium.Tests.EditMode
{
    public class DesignDataParserTests
    {
        private static ValueRecord ParseSingleValue(string values)
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(DesignDataTestCsv.ModuleRowWithValues("Ink Beast", values));
            Assert.That(result.Errors, Is.Empty);
            ModuleRecord module = result.Data.Modules.Find(m => m.Id == "Ink Beast");
            Assert.That(module.Values, Has.Count.EqualTo(1));
            return module.Values[0];
        }

        private static void AssertFailsWithOneErrorMentioning(DesignDataParseResult result, string text)
        {
            Assert.That(result.Data, Is.Null, "A failed parse must return no data, so nothing partial is written.");
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain(text));
        }

        [Test]
        public void Parse_TierColumns_ResolveToTheirScaleValue()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse();

            Assert.That(result.Errors, Is.Empty);
            ModuleRecord kick = result.Data.Modules.Find(m => m.Id == "Kick");
            Assert.That(kick.KnockbackTier.Word, Is.EqualTo("Normal"));
            Assert.That(kick.KnockbackTier.Value, Is.EqualTo(4f));
            Assert.That(kick.RangeTier.Value, Is.EqualTo(2f));
        }

        [Test]
        public void Parse_CategoryWithSpacesAndParentheses_MatchesEnum()
        {
            string row = DesignDataTestCsv.ModuleRow("Evac Balloon", "Gear (active)", "", "", "30");

            DesignDataParseResult result = DesignDataTestCsv.Parse(row);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Data.Modules.Find(m => m.Id == "Evac Balloon").Category, Is.EqualTo(ModuleCategory.GearActive));
        }

        [Test]
        public void Parse_QuotedField_KeepsItsComma()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse();

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Data.Props[0].Id, Is.EqualTo("Car, truck"));
            Assert.That(result.Data.Props[0].Size, Is.EqualTo(PropSize.Large));
        }

        [TestCase("summon_lifetime_s=20", 20f)]
        [TestCase("detectability=-0.5", -0.5f)]
        public void Values_PlainNumber_IsNumber(string values, float expected)
        {
            ValueRecord value = ParseSingleValue(values);

            Assert.That(value.Kind, Is.EqualTo(DataValueKind.Number));
            Assert.That(value.Number, Is.EqualTo(expected));
        }

        [Test]
        public void Values_LowHigh_IsRange()
        {
            ValueRecord value = ParseSingleValue("goons=3-5");

            Assert.That(value.Kind, Is.EqualTo(DataValueKind.Range));
            Assert.That(value.RangeMin, Is.EqualTo(3f));
            Assert.That(value.RangeMax, Is.EqualTo(5f));
        }

        [Test]
        public void Values_TierKey_ResolvesTierWord()
        {
            ValueRecord value = ParseSingleValue("tower_stun=Brief");

            Assert.That(value.Kind, Is.EqualTo(DataValueKind.Tier));
            Assert.That(value.Tier.Word, Is.EqualTo("Brief"));
            Assert.That(value.Tier.Value, Is.EqualTo(1f));
        }

        [Test]
        public void Values_ModuleKey_NamesModule()
        {
            ValueRecord value = ParseSingleValue("summon_attack=Ink Lash");

            Assert.That(value.Kind, Is.EqualTo(DataValueKind.Module));
            Assert.That(value.ModuleId, Is.EqualTo("Ink Lash"));
        }

        [Test]
        public void Parse_UnknownTierWord_InTierColumn_Fails()
        {
            string row = DesignDataTestCsv.ModuleRow("Mighty Kick", "Active", "Strength", "Mighty", "3");

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(row), "Mighty");
        }

        [Test]
        public void Parse_UnknownTierWord_InValues_Fails()
        {
            string row = DesignDataTestCsv.ModuleRowWithValues("Card Throw", "tower_stun=Brif");

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(row), "Brif");
        }

        [Test]
        public void Parse_DuplicateId_Fails()
        {
            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(DesignDataTestCsv.KickRow), "duplicate id 'Kick'");
        }

        [Test]
        public void Parse_MissingModuleReference_Fails()
        {
            string row = DesignDataTestCsv.ModuleRowWithValues("Ink Beast", "summon_attack=Ink Lassh");

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(row), "Ink Lassh");
        }

        [Test]
        public void Parse_UnknownConstant_Fails()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(extraConstantRows: "formula,hp_per_tuffness,10,\n");

            AssertFailsWithOneErrorMentioning(result, "hp_per_tuffness");
        }

        [Test]
        public void Parse_TagsAndDamageType_AreRead()
        {
            string row = DesignDataTestCsv.ModuleRow("Spectrum Bolt", "Active", "Intelligence", "Light", "3", "", "",
                "", "", "", "", "", "", "Magic", "Ranged; Debuff; Resist:Toughness; Resist:Will");

            DesignDataParseResult result = DesignDataTestCsv.Parse(row);

            Assert.That(result.Errors, Is.Empty);
            ModuleRecord bolt = result.Data.Modules.Find(m => m.Id == "Spectrum Bolt");
            Assert.That(bolt.DamageType, Is.EqualTo(DamageType.Magic));
            Assert.That(bolt.Tags, Is.EquivalentTo(new[] { ModuleTag.Ranged, ModuleTag.Debuff }));
            Assert.That(bolt.ResistStats, Is.EqualTo(new[] { Stat.Toughness, Stat.Will }));
        }

        [Test]
        public void Parse_NoRegenTag_NamesTheResource()
        {
            string row = DesignDataTestCsv.ModuleRow("Reaper Battery", "Passive", "", "", "", "", "", "", "", "", "",
                "", "", "", "CannotBeDisabled; NoRegen:Charge");

            DesignDataParseResult result = DesignDataTestCsv.Parse(row);

            Assert.That(result.Errors, Is.Empty);
            ModuleRecord battery = result.Data.Modules.Find(m => m.Id == "Reaper Battery");
            Assert.That(battery.Tags, Is.EqualTo(new[] { ModuleTag.CannotBeDisabled }));
            Assert.That(battery.NoRegenResources, Is.EqualTo(new[] { ResourceType.Charge }));
        }

        [TestCase("Sneaky", TestName = "Parse_UnknownTag_Fails")]
        [TestCase("Resist:Luck", TestName = "Parse_ResistWithAnUnknownStat_Fails")]
        public void Parse_BadTag_Fails(string tag)
        {
            string row = DesignDataTestCsv.ModuleRow("Odd Module", "Passive", "", "", "", "", "", "", "", "", "",
                "", "", "", tag);

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(row), tag);
        }

        [Test]
        public void Parse_HeroKit_ListsModulesBySlotType()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(extraHeroRows: "Rune,20,20,10,10,10,,Ink Lash,,,\n");

            Assert.That(result.Errors, Is.Empty);
            HeroRecord rune = result.Data.Heroes.Find(h => h.Id == "Rune");
            Assert.That(rune.ActiveIds, Is.EqualTo(new[] { "Ink Lash" }));
            Assert.That(rune.PassiveIds, Is.Empty);
        }

        [TestCase("Rune,20,20,10,10,10,,Ink Lassh,,,\n", "Ink Lassh", TestName = "Parse_HeroKit_UnknownModule_Fails")]
        [TestCase("Rune,20,20,10,10,10,,,Ink Lash,,\n", "Ink Lash", TestName = "Parse_HeroKit_WrongSlotType_Fails")]
        [TestCase("Rune,20,20,10,10,10,,Ink Lash; Ink Lash,,,\n", "twice", TestName = "Parse_HeroKit_Duplicate_Fails")]
        public void Parse_BadHeroKit_Fails(string heroRow, string mentioned)
        {
            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(extraHeroRows: heroRow), mentioned);
        }

        [Test]
        public void Parse_WorldConstants_AreRead()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse();

            Assert.That(result.Errors, Is.Empty);
            WorldConstants world = result.Data.World;
            Assert.That(world.MaterialHp(StructureMaterial.Glass), Is.EqualTo(10f));
            Assert.That(world.MaterialHp(StructureMaterial.Crystalline), Is.EqualTo(1000f));
            Assert.That(world.ImpactStructureDamageTier.Value, Is.EqualTo(2f));
            Assert.That(world.ImpactEntityDamageTier.Value, Is.EqualTo(0.5f));
            Assert.That(world.BuildingImpactMultiplier, Is.EqualTo(10f));
            Assert.That(world.BuildingNeighborRadius, Is.EqualTo(20f));
        }

        [Test]
        public void Parse_MaterialWithoutHp_Fails()
        {
            string constants = DesignDataTestCsv.Constants.Replace("world,material_hp.Metal,600,\n", string.Empty);

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(constants: constants), "material_hp.Metal");
        }

        [Test]
        public void Parse_UnknownMaterial_Fails()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(extraConstantRows: "world,material_hp.Paper,5,\n");

            AssertFailsWithOneErrorMentioning(result, "material_hp.Paper");
        }
    }
}
