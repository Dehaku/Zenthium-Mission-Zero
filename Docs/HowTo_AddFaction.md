# How to add a faction

The Galactic Repo Agency (Milestone 7) is the template. A faction is data first: most factions need no new code beyond their new modules. The design docs are the source of truth. Read the faction's file in `Docs/design/Factions/`, `design/factions.md` and the modules it lists before starting. Ask about anything they leave open.

## 1. Data
All numbers come from `Docs/design/data/*.csv`. Never take them from markdown or put them in code.

1. **`entities.csv`:** one row per entity. The faction's rows usually exist already, with `rank_tier`, `min_rank` and `budget_cost`. Fill in:
   - `modules`: in AI priority order, `;`-separated, as named in `modules.csv`. Base actions such as `Drag` go in the list too. They take no slot, but they say the entity uses them: an entity whose list has Drag drags citizens.
   - `locked_until`: optional, as `Module:Rank; Module:Rank`. A locked module is left out below that rank.
   - `tags`: the entity's own tags, such as `Coward; Hackable`.
   - `values`: entity-only numbers, such as the Tow Saucer's `crash_damage` and `crash_radius_m`.
2. **`factions.csv`:** one row per faction, with the `faction` column spelled exactly as in `entities.csv`.
   - `tags`: tags every entity in the faction has, such as `LootDestination:CropCircle`.
   - `arrival`: the arrival tag without its prefix (`Beam`, `Van`, `OnFoot`, `ClownCar`, `Sewer`, `Portal`).
   - `mission_weights` and `location_weights`: as `rescue=3; arson_riot=1` and `bank=2`. Unlisted ones are 1.
   - `values`: faction-only numbers, such as crop circles and the Mothership Strike.
   - A faction without a row has no tags, arrival or weights.
3. **New tags:** an entity tag that's new gets a value at the **end** of `EntityTag` (`Scripts/Data/EntityTag.cs`), because scenes store tags by index. A parameterized tag such as `LootDestination:<place>` gets one value per place (`LootDestinationCropCircle`). The importer matches it with the colon left out.
4. **Run Zenthium > Data > Import Design CSVs.** It fails, with the row named, on an unknown module, tag, rank or arrival, a duplicate, a lock on a module that isn't listed, or a faction with no entities.

## 2. Modules
1. For each listed module, check `ModuleCatalog.TryCreate` (`Scripts/Modules/Core/ModuleCatalog.cs`). A module with no case logs "no behavior yet" and does nothing.
2. Build a new module from the shared parts before writing a new class:
   - `HitEffects`: damage, knockback, a status and its modifiers.
   - Deliveries: `MeleeStrikeModule`, `ProjectileModule`, `BeamModule`, `AreaStrikeModule`, `PullModule`, `TimedCharge`.
   - `SummonModule` and `CallForBackupModule`.
   - Passives that tick, such as `NotarizedPassive`.
   - `AreaBlast` for environmental damage.
3. Link modules to entities and statuses through tags and interfaces, never by naming an entity:
   - `ILootDestination`, `IStealable`, `IPullable`;
   - `IAwarenessReset` and `IAwarenessReveal` (the AI implements them);
   - `EntitySpawner`.
4. A number the module needs goes in its `modules.csv` row (a tier column or `values`). The catalog's `Require` names what's missing.

## 3. Faction rules
- **Loot destinations:** a `LootDestination:<place>` tag plus a scene component that implements `ILootDestination` and registers with `ModuleWorld`, as `CropCircle` does. The AI's looting, citizen dragging and towing find it through `ModuleWorld.TryFindLootDestination`, and `LootRules` decides where loot goes.
- **Faction-wide effects:** a component with its rules read from the faction's `values` by a plain-C# rules class, such as `CropCircleRules` and `MothershipRules`. Keep the rule logic in the plain class so it can be tested.
- **Arrival:** `ArrivalStub` until Milestone 9.

## 4. Sandbox
1. Copy `RepoSandboxSceneCreator` (`Scripts/Editor/`), with its own menu path and scene path.
   - Place entities with `PlaceRepoEntity`'s pattern: a training dummy given the faction entity and a rank, plus `AiSandboxSceneCreator.AddGoonAi`.
   - Add the citizen and stash helpers, `MapBounds`, and an `EntitySpawner` if any module spawns entities.
2. Rewrite `Assets/_Project/Sandbox/MilestoneChecklist.txt` for it. Rooms built with `PlayerSandboxSceneCreator.TryBuildRoom` show it, and Caps Lock toggles it.

## 5. Tests and log
- Add EditMode tests for the new rules classes and anything the importer parses differently. Read `Docs/claude/writing-tests.md` first.
- Add a `Docs/BuildLog.md` entry: what was built, the data edits, the decisions, stubs and deferred items, the Editor steps, and what to check.
