using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// The menu command that imports <c>Docs/design/data/*.csv</c> into the generated data assets
    /// and the <c>DataIds</c> class. See Docs/claude/data-importer.md.
    /// </summary>
    internal static class DesignDataImporter
    {
        private const string MenuPath = "Zenthium/Data/Import Design CSVs";
        private const string DataFolder = "Docs/design/data";
        private const string DialogTitle = "Import Design CSVs";

        [MenuItem(MenuPath)]
        private static void Import()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            var errors = new List<string>();
            var source = new DesignDataSource(
                ReadFile(projectRoot, DesignDataSource.ConstantsFile, errors),
                ReadFile(projectRoot, DesignDataSource.ModulesFile, errors),
                ReadFile(projectRoot, DesignDataSource.EntitiesFile, errors),
                ReadFile(projectRoot, DesignDataSource.HeroesFile, errors),
                ReadFile(projectRoot, DesignDataSource.PropsFile, errors),
                ReadFile(projectRoot, DesignDataSource.PricesFile, errors),
                ReadFile(projectRoot, DesignDataSource.FactionsFile, errors));
            if (errors.Count > 0)
            {
                Fail(errors);
                return;
            }

            DesignDataParseResult result = DesignDataParser.Parse(source);
            if (result.Data == null)
            {
                Fail(result.Errors);
                return;
            }

            // Generated before any asset is written, so a bad id stops the import with nothing changed.
            string idsCode = DataIdsGenerator.Generate(result.Data, errors);
            if (idsCode == null)
            {
                Fail(errors);
                return;
            }

            var warnings = new List<string>();
            GeneratedAssetWriter.Write(result.Data, warnings);
            bool idsChanged = DataIdsGenerator.WriteIfChanged(projectRoot, idsCode);
            if (idsChanged)
            {
                // Picks up the new DataIds.cs, which recompiles scripts.
                AssetDatabase.Refresh();
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning($"Design data import: {warning}");
            }

            ParsedDesignData data = result.Data;
            Debug.Log($"Design data imported: {data.Modules.Count} modules, {data.FactionIds.Count} factions, "
                + $"{data.Entities.Count} entities, {data.Heroes.Count} heroes, {data.Props.Count} props, "
                + $"{data.Prices.Count} prices, {warnings.Count} warnings. "
                + (idsChanged ? $"{DataIdsGenerator.OutputPath} was updated." : $"{DataIdsGenerator.OutputPath} is unchanged."));
        }

        private static string ReadFile(string projectRoot, string fileName, List<string> errors)
        {
            string path = Path.Combine(projectRoot, DataFolder, fileName);
            if (!File.Exists(path))
            {
                errors.Add($"{fileName}: not found at {path}.");
                return null;
            }

            return File.ReadAllText(path);
        }

        private static void Fail(IReadOnlyList<string> errors)
        {
            foreach (string error in errors)
            {
                Debug.LogError($"Design data import: {error}");
            }

            EditorUtility.DisplayDialog(DialogTitle,
                $"The import found {errors.Count} error(s), so nothing was written. See the Console for each one.", "OK");
        }
    }
}
