using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Editor.DataImport;

namespace Zenthium.Tests.EditMode
{
    public class DataIdsGeneratorTests
    {
        [TestCase("Shush!", "Shush")]
        [TestCase("Car, truck", "CarTruck")]
        [TestCase("Gold-Plated Chrome", "GoldPlatedChrome")]
        [TestCase("summon_lifetime_s", "SummonLifetimeS")]
        [TestCase("3D Glasses", "_3DGlasses")]
        public void IdentifierFormatter_MakesPascalCaseIdentifier(string id, string expected)
        {
            Assert.That(IdentifierFormatter.TryFormat(id, out string identifier), Is.True);
            Assert.That(identifier, Is.EqualTo(expected));
        }

        [Test]
        public void Generate_TwoIdsWithTheSameIdentifier_Fails()
        {
            string rows = DesignDataTestCsv.ModuleRow("Shush!", "Active", "Will", "", "15")
                + DesignDataTestCsv.ModuleRow("Shush", "Active", "Will", "", "15");
            DesignDataParseResult parsed = DesignDataTestCsv.Parse(rows);
            Assert.That(parsed.Errors, Is.Empty);
            var errors = new List<string>();

            string code = DataIdsGenerator.Generate(parsed.Data, errors);

            Assert.That(code, Is.Null);
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("'Shush!'").And.Contain("'Shush'"));
        }

        [Test]
        public void Generate_IdWithQuote_IsEscapedInTheConstant()
        {
            DesignDataParseResult parsed = DesignDataTestCsv.Parse(extraHeroRows: "\"The \"\"Boss\"\"\",10,10,10,10,10,,\n");
            Assert.That(parsed.Errors, Is.Empty);

            string code = DataIdsGenerator.Generate(parsed.Data, new List<string>());

            Assert.That(code, Does.Contain("public const string TheBoss = \"The \\\"Boss\\\"\";"));
        }
    }
}
