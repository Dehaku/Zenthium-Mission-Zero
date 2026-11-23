# Data importer

The importer is an editor menu command that reads `Docs/design/data/*.csv` and creates or updates ScriptableObject assets in `Assets/_Project/Data/Generated/`.

- Convert tier words (Light, Normal, Heavy, Short, Long and so on) to numbers using `constants.csv`. Keep the tier word on the asset for display.
- Fail with a clear error on unknown tier words, duplicate IDs, and references to entries that don't exist. Don't write partial results.
- Re-running the import updates existing assets in place, so references to them in prefabs and other assets survive.
- Assets reference data entries directly, not by name. Where code must name a specific entry, generate a constants class from the IDs and use that, never a string literal.
- A Module's numbers come from data; its behavior is code built from the shared Module parts. The data's Module ID is how the two are linked.
- The importer is editor-only code: keep it in `Assets/_Project/Scripts/Editor/`.
