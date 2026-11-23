# Build Log

One entry per milestone session (`Docs/BuildPlan.md`). Newest at the bottom.

---

## Milestone 0: Foundation (implemented, not yet checked, 2026-11-22)

### Done
- Mirrored the design branch into `Docs/design/` (39 files) and `Docs/tools/balance.py`, at commit `339a995`. Source and refresh steps are in `Docs/design/SOURCE.md`.
- `Packages/manifest.json` (approved): added `com.unity.ai.navigation` 2.0.16 (this Editor's version) and listed `com.unity.test-framework` 1.8.0 directly.
- `Assets/InputSystem_Actions.inputactions` (approved: Claude edits the JSON). Same file and GUID, so it stays the project-wide actions asset. The template's original is backed up outside the project.
  - **Player map** from `design/controls.md`: Move, AimPointer, AimStick, Power1-4, Gear1-4, Kick, Drag, Sprint, MoreInfo, FriendlyFire, Pause, SkipHold. Map id kept.
  - **UI map** kept for uGUI menus, minus the Touch, XR and Joystick bindings and the two XR tracking actions.
  - **Control schemes:** Keyboard&Mouse and Gamepad only.
- Folders and assembly definitions (approved layout), created only where needed now: `Zenthium.Core` (references Unity.InputSystem, for the generated input class), `Zenthium.Player`, `Zenthium.Editor` (Editor only, for the scene setup command), `Zenthium.Tests.EditMode` and `Zenthium.Tests.PlayMode`. Also `Assets/_Project/Scenes/` and `Scripts/Core/Controls/`.
- `Assets/_Project/Scripts/Player/OverheadCameraRig.cs`: an orthographic camera looking straight down at XZ, smoothly following a target. Screen-up is +Z, so 2D input maps directly to world X and Z.

### Choices made in the input bindings (confirmed by the user, 2026-11-22)
- **Aim is two actions:** `AimPointer` (mouse screen position, to be raycast onto the ground) and `AimStick` (right stick direction). One action can't serve both well.
- **Gear 1-4 on the D-pad:** up, right, down, left (clockwise). `design/controls.md` only says "D-pad".
- **"Hold" buttons are plain buttons:** MoreInfo, FriendlyFire and SkipHold. Code reads whether they're held, and times the skip hold, so no hold duration sits in the asset.
- **Sprint is a plain button;** the toggle is done in code.

### Wording for `CLAUDE.md` (Architecture TODO: scene and mission flow), added by the user's request
> - **Scenes and flow:** `Boot` → `HQ` → `Mission` → back to `HQ`. `Boot` creates long-lived services, then loads `HQ`. `HQ` starts a mission by handing a mission setup object to `Mission`, which assembles the map at runtime. `Sandbox_*` scenes are for testing single systems and are never in a build.

First pass: whether `Mission` loads additively, and where services live, is decided when those systems exist.

### Editor steps for the user
1. **Let Unity import the package changes.** AI Navigation installs on focus. Check the Console for errors.
2. **Input class generation (done by Claude at the user's request):** the asset's import settings now generate `InputSystem_Actions` in namespace `Zenthium.Core.Controls` at `Assets/_Project/Scripts/Core/Controls/InputSystem_Actions.cs`. It must be inside `Scripts/Core/`, because assemblies with a `.asmdef` can't reference code in `Assembly-CSharp`. If the file doesn't appear after Unity regains focus, right-click the asset and choose **Reimport**.
3. **Open the actions** (double-click the asset) and look over the Player map against `design/controls.md`.
4. **Check project-wide actions:** **Edit > Project Settings > Input System Package** should still show `InputSystem_Actions` as the project-wide actions asset.
5. **Scenes, build list and camera:** run **Zenthium > Setup > Create Core Scenes** (added by Claude at the user's request; code in `Scripts/Editor/CoreSceneCreator.cs`, assembly `Zenthium.Editor`). It:
   - creates `Boot` (empty), `HQ` and `Mission` (default camera and light) in `Assets/_Project/Scenes/`, skipping any that already exist;
   - adds Overhead Camera Rig to `Mission`'s camera (its target stays empty until the player prefab exists in Milestone 3);
   - puts `Boot`, `HQ` and `Mission` first in the build's scene list, keeping SampleScene after them.
   It asks to save any open scene changes first, and leaves `Mission` open when done.

### What to check
- [ ] The Console has no errors after import and after generating the input class.
- [ ] `InputSystem_Actions.cs` is generated in `Scripts/Core/Controls/`, and the project compiles.
- [ ] The only expected Console messages are warnings that `Zenthium.Tests.EditMode` and `Zenthium.Tests.PlayMode` won't be compiled because they have no scripts. They go away when Milestone 1 adds the first tests. `Zenthium.Core` warns the same way until step 2 generates the input class.
- [ ] In `Mission`, pressing Play makes the Main Camera orthographic, looking straight down.
- [ ] Each "You check" item for Milestone 0 in `Docs/BuildPlan.md`.

### Findings
- **Python isn't installed** on this PC, so `Docs/tools/balance.py report` can't run. Milestone 1 needs its output for test literals: install Python 3 (with `openpyxl` only if you want the `xlsx` view), or paste a report run from another machine.
- **Test Framework 1.8.0 is already in the project** as an indirect dependency (depth 1 in `packages-lock.json`). It isn't listed directly in `manifest.json`.
- **TextMeshPro** is included in uGUI 2.6.0, so no separate package is needed.
- **AI Navigation** isn't installed.
- **WebGL build support** is installed for this Editor.
- **Input:** `InputSystem_Actions.inputactions` still holds the template's actions, and C# class generation is off (`generateWrapperCode: 0` in its import settings).
- **Fog of war candidates** (for Milestone 8; none confirmed for WebGL yet):
  - Fog of War by Kiwi Studios (Fab): Unity 6 URP, line of sight with planar distance and occlusion, depth-texture based. Paid.
  - FOV Mapping by StupaSoft (Asset Store): free, GPU-based, URP. Built for 2022.3, so Unity 6 is unconfirmed.
  - Custom: a CPU visibility grid on XZ, raycast from the player, drawn as a texture overlay. No dependency and WebGL-safe, but more code.

### Decisions
- Packages, input setup and the folder, assembly and scene layout: approved and done (above).
- Fog of war: decide in Milestone 8, from the candidates above.

### Deferred
- **Boot loader script:** comes with the first long-lived service. Until then, open scenes directly.
- **Python for `balance.py`:** needed before Milestone 1's formula tests (see Findings).

---

## Milestone 1: Data pipeline (checked by the user, 2026-11-23)

### Done
- **New assembly `Zenthium.Data`** (`Scripts/Data/`, approved). It references nothing else of ours and lets `Zenthium.Editor` see its internals, so only the importer fills in data assets. `Zenthium.Editor` now references it; `Zenthium.Tests.EditMode` references `Zenthium.Data` and `Zenthium.Editor`.
- **Data types:**
  - **Assets, one per CSV row:** `ModuleData`, `EntityData`, `HeroData`, `PropData`, `PriceData`, and `FactionData`, one per distinct faction name in entities.csv. They share the base `DataEntry` (id = the row's name, verbatim).
  - **Shared pieces:** `TierValue` (tier word plus resolved number), `DataValueList` (the parsed `values` column) and the enums in `DataEnums.cs`.
  - **Constants:** `FormulaConstants`, `RankScalingConstants` (the `mirror` rows), `EconomyConstants` and `TierTables`.
- **`GameDatabase`:** holds direct references to every asset plus the constants, with `TryGet` lookups by id through `DataTable<T>`.
- **`BalanceFormulas`:** section 1 (HP, Armor, Normal damage, dodge, awareness fill time, resource value) and section 4 (damage by tier).
- **Importer** (`Scripts/Editor/DataImport/`), menu **Zenthium > Data > Import Design CSVs**:
  - **Parse and check:** `DesignDataParser` turns CSV text into records and collects every error. It covers unknown tier words, duplicate ids, missing module references, unknown enum words, bad numbers, missing columns, and missing or unknown constants.
  - **Any error stops the import:** nothing is written, and each error goes to the Console.
  - **Writing:** `GeneratedAssetWriter` finds existing assets by stored id and updates them in place.
  - **`DataIds.cs`:** generated at `Scripts/Data/Generated/DataIds.cs` by `DataIdsGenerator`. It is checked for identifier clashes before any asset is written, and only rewritten when it changes.
- **Tests (EditMode):** `BalanceFormulasTests`, `DesignDataParserTests` and `DataIdsGeneratorTests`, with the shared CSV fixture `DesignDataTestCsv`.

### Decisions (approved by the user)
- **Non-numeric `values`:** a table in `ValueParser` lists the keys whose values are tier words (`tower_stun`, `lovers_hypnotize`, `hanged_man_root`: Duration; `death_bonus`: Damage) or module ids (`summon_attack`). Any other value must be a number or a `low-high` range. The design docs are unchanged. A new non-numeric key fails the import with a message saying to add it there.
- **Factions:** one `FactionData` asset per faction name, including "Summons" and "Neutral". Entities reference them directly.
- **Removed rows:** the asset is kept and a warning logged; it's left out of `GameDatabase`.
- **Ignored:** the `reference` constants and modules.csv's `budget_ok` and `notes` columns are for `balance.py` only.
- **Mission types:** `MissionType` (Rescue, Countdown, Invasion, Arson Riot) was added now, for the `income_mix` rows. A mission type with no row gets a 1x multiplier (balance.md section 7: "Arson Riot is even").
- **Awareness:** beyond vision range, the fill time is infinite. Detectability's effect on it waits for Milestone 6.

### Anchor values, checked by hand
Python still isn't installed, so `balance.py report` wasn't run. Worked from its formulas instead:
- Kicks to defeat at rank D: goon 3-4, Champion 5-10, Leader 18-40.
- Goon hits a starting hero survives: 10-40.

The kick counts need Milestone 2's damage pipeline, so their tests come then. Milestone 1's tests cover the formula outputs at the anchor inputs.

### Editor steps for the user
1. **Let Unity compile.** Expect only the existing warning that `Zenthium.Tests.PlayMode` has no scripts.
2. **Run Zenthium > Data > Import Design CSVs.** It creates `Assets/_Project/Data/Generated/` (`GameDatabase.asset` plus `Modules/`, `Factions/`, `Entities/`, `Heroes/`, `Props/`, `Prices/`) and `Assets/_Project/Scripts/Data/Generated/DataIds.cs`, then recompiles.

### What to check
- [x] The import logs a summary: 158 modules, 9 factions, 50 entities, 8 heroes, 5 props, 10 prices, 0 warnings. There are no errors.
- [x] Open a few assets, such as `Modules/CardThrow` (its values), `Entities/Grey` (faction reference) and `GameDatabase` (constants and tables).
- [x] Change a number in a CSV and import again. The asset updates, and something that references it keeps the reference: for example, a test field, or `Ink Beast`'s `summon_attack` pointing to `Ink Lash`.
- [x] Run the EditMode tests `BalanceFormulasTests`, `DesignDataParserTests` and `DataIdsGeneratorTests`.

### Fixes after the first import
- **`prices.csv` line 11** (approved by the user, 2026-11-23): the Cosmetic row's note had an unquoted comma, so the row had 5 cells under a 4-column header and the import stopped. The note is now quoted: `"A costume, palette or alternate look"`. The same fix was pushed to the design branch as commit `02bd9b8` (at the user's request), and `Docs/design/SOURCE.md` now points to that commit.

### Open questions and findings
- **Uppercut's cost:** `combat.md` says 50 Stamina, while `modules.csv` says 15. The CSV is the source of numbers, but one of the two is out of date.
- **Data the CSVs don't have:** hero kits, enemy module lists, module tags, triggers and damage types exist only in the markdown. Milestones 4 and 7 will need a home for them in data.

### Deferred
- **Kick-count anchor tests:** come with Milestone 2's damage pipeline.
- **Nothing uses `GameDatabase` yet:** the first runtime consumer (Milestone 2 or 3) gets a serialized reference to it.

---

## Milestone 2: Entity and combat core (checked by the user, 2026-11-23)

### Done
- **New assemblies** (approved):

  | Assembly | Folder | References |
  |---|---|---|
  | `Zenthium.Time` | `Scripts/Time/` | nothing |
  | `Zenthium.Entities` | `Scripts/Entities/` | Core, Data, Time |
  | `Zenthium.Combat` | `Scripts/Combat/` | Core, Data, Entities |
  | `Zenthium.Sandbox` | `Scripts/Sandbox/` | Core, Data, Time, Entities, Combat, `UnityEngine.UI`, `Unity.TextMeshPro` |

  - `Zenthium.Time`'s namespace is `Zenthium.Timing`: a `Zenthium.Time` namespace would hide `UnityEngine.Time` in every `Zenthium.*` namespace.
  - `Zenthium.Editor` now also references Time, Entities, Combat, Sandbox, `UnityEngine.UI`, `Unity.TextMeshPro` and `Unity.InputSystem`.
  - `Zenthium.Tests.EditMode` now also references Time, Entities and Combat.
- **Data:**
  - `constants.csv` has 7 new `formula` rows (approved, local only): `base_stat`, `base_move_speed`, `defiance_chance_per_will`, `defiance_roll_cost`, `defiance_invulnerable_player`, `defiance_invulnerable_other`, `resource_regen_per_second`.
  - They're imported into the new `EntityConstants` (`GameDatabase.EntityConstants`) by `ConstantsParser`.
  - `DamageType` enum (Physical, Magic, Energy) in `DataEnums.cs`.
  - `RankScalingConstants.StatBonus` and `HpDamageBonus`, by difficulty level (ranks above D: 0 at D, 4 at S, 5 at S2).
- **Random:** `IRandomSource` and `UnityRandomSource` in `Zenthium.Core`, so tests can force dodge and Defiance rolls.
- **Time:**
  - `GameClock` has three parts:
    - pause;
    - a global scale that affects everyone (for the player's Defiance slowdown and cutscenes);
    - a time-power scale (Time Slow and Time Stop) that entities exempt from time powers ignore.
  - `GameDelta` is for game time per entity; `CooldownDelta` is real seconds, 0 while paused.
  - `GameClockDriver` is the only writer of `Time.timeScale` (paused ? 0 : global scale). Time powers never touch it, so the player keeps full speed.
- **Entities:**
  - `Entity`: the one entity type, built from an `EntitySetup` (`ForHero`, `ForFactionEntity`). It holds:
    - team (`Team`), an optional rank tier, `EntityTag`s (`Hackable`, `Coward`);
    - a module list that only grants resources for now;
    - `EntityStats`: base × (1 + innate + difficulty) × Π(1 + boost), plus Detectability as a multiplier;
    - `Health`, `Defenses` (Armor, Damage Reduction, dodge), `MoveSpeed`, `Defiance`, `Invulnerability` and `ResourcePools`.
  - Shared helper: `ModifierStack`, with handles so a source can remove its own modifier.
  - Logic events: `Entity.OnDamaged`, `OnIncapacitated`, `OnDefianceProc`; `Health.OnHealthChanged`; `ResourcePools.OnResourceChanged`; plus `EntityStats.OnStatsChanged`, `Defenses.OnDefensesChanged` and `MoveSpeed.OnMoveSpeedChanged`.
  - `EntityBehaviour`: the thin MonoBehaviour. Hero *or* faction entity, team, difficulty level, modules. Ticks the entity with unscaled delta (clamped like `Time.deltaTime`), and `ResetEntity` rebuilds it.
- **Combat:**
  - `DamagePipeline.Apply(Hit, Entity)`, in this order:
    1. friendly fire, and Incapacitated targets (no event);
    2. the source's rank damage bonus;
    3. Invulnerable (Blocked);
    4. dodge (direct hits with a source only);
    5. Armor, then Damage Reduction, with the 5% floor (DoTs skip this);
    6. HP, where Defiance is checked.
  - Entities only take damage through it: `Entity.ReceiveDamage` is internal, visible to `Zenthium.Combat` only.
  - `FriendlyFireRules` (matrix below). `CombatRulesBehaviour` owns the pipeline in a scene.
- **Sandbox:**
  - `CombatDebugPanel` (uGUI + TMP) shows the dummy's HP, Armor, Damage Reduction, dodge, Defiance chance and stacks, move speed and stats. It has:
    - Hit (amount, type, DoT, from the attacker or the environment);
    - Kick (from the attacker: Kick's data tier and stat);
    - Heal;
    - set a base stat, add a % boost;
    - add a Damage Reduction %, clear modifiers;
    - reset;
    - a log of damage, Defiance and Incapacitated events.
  - `CombatSandboxSceneCreator` (Editor): menu **Zenthium > Setup > Create Combat Sandbox Scene**.
- **Design doc:** `stats.md` (Primary stats) has the Toughness-change rule. This is a local edit, like the `constants.csv` rows.
- **Tests (EditMode):**
  - `EntityStatsTests`: Chad's 90; difficulty adds to innate; boosts multiply; move speed bonuses add.
  - `HealthTests`: max HP; Toughness gain and loss; heal cap; no revive.
  - `DamagePipelineTests`: 63; 75% Damage Reduction; 5% minimum; DoT; dodge; rank damage bonus; Incapacitated; friendly fire.
  - `DefianceTests`: 213% sequence; 1 HP; cleanse; invulnerable time by team; blocking; no proc.
  - `FriendlyFireRulesTests`: the matrix.
  - `ResourcePoolsTests`: grants, spending, regen, `NoRegen`, real seconds, pause.
  - `GameClockTests`.
  - `CombatAnchorTests`: the kick and goon-hit anchors deferred from Milestone 1.
  - Shared helper: `CombatTestSetup` (literal constants, `FixedRandom`). `DesignDataTestCsv` gained the 7 new constants; no assertions changed.

### Decisions (approved by the user)
- **The 7 new constants:** added to `constants.csv` as `formula` rows, local only. The design branch is not updated.
- **Friendly fire:**
  - Player hurts Enemy and Neutral; Ally and Civilian only while friendly fire is held; never itself.
  - Ally hurts Enemy and Neutral only.
  - Enemy hurts everyone, other enemies too, unless the dev toggle is off.
  - Neutral hurts everyone.
  - Environment hurts everyone.
  - Not covered by the matrix: civilians hurt no one.
- **Dodge:** direct hits only; never DoT ticks or environmental damage.
- **Resources:** regen runs in real seconds, stopped only while paused; pools start full.
- **Move speed:** bonuses add, as base × (1 + Σ bonuses). Slows multiply on top in Milestone 4.
- **Toughness changes:** gaining Toughness raises current HP by the max HP gained, and losing it doesn't lower current HP. Added to `stats.md`.
- **Incapacitated:** takes no damage, and Heal doesn't revive. Revives come in Milestone 4.
- **Sandbox_Combat:** built by a menu command.

### Choices made in code (not asked)
- The source's rank damage bonus is applied by the pipeline, before Armor, so the 5% floor is 5% of the boosted hit.
- Invulnerable and dodged hits raise `OnDamaged` with outcome Blocked or Dodged (for "Miss" popups later). Hits stopped by friendly fire or an Incapacitated target raise nothing.
- Defiance invulnerability runs on game time (slowed by Time Slow for non-players), like status durations.
- Defiance Roll is a counter on `Defiance`, not a status. Milestone 4 can move it into statuses if needed.
- Detectability is a multiplier starting at 1, with modifiers that multiply. Its effect on awareness comes in Milestone 6.

### Stubs
- `IDefianceCleanse`: the Defiance cleanse. Milestone 4's statuses implement it; until then a proc cleanses nothing.
- `EntitySetup.Modules` / `Entity.Modules`: a plain list standing in for Milestone 4's slots. It only grants resources.
- `FriendlyFireRules.IsPlayerFriendlyFireHeld`: Milestone 3 wires the button.
- `Invulnerability`: granted only by Defiance for now. Milestone 4's Invulnerable status (Dash, No Big Deal) may take it over.

### Editor steps for the user
1. **Let Unity compile.** Expect only the existing warning that `Zenthium.Tests.PlayMode` has no scripts.
2. **Run Zenthium > Data > Import Design CSVs.** This puts the 7 new constants into `GameDatabase`; until then they read 0. Expect the same summary as before: 158 modules, 9 factions, 50 entities, 8 heroes, 5 props, 10 prices, 0 warnings.
3. **Run Window > TextMeshPro > Import TMP Essential Resources.** It creates `Assets/TextMesh Pro/`.
4. **Run Zenthium > Setup > Create Combat Sandbox Scene.** It creates `Assets/_Project/Scenes/Sandbox_Combat.unity` (not in the build list) with:
   - a top-down camera, ground, Game Clock and Combat Rules;
   - a Grey goon dummy (Enemy, rank D) and a Rush Hour attacker (Player);
   - an EventSystem with the Input System UI module;
   - the wired debug panel at the top left.
5. **Press Play in `Sandbox_Combat`** and use the panel.

### What to check
- [x] The Console has no errors after compiling, importing and building the scene.
- [x] Run the whole EditMode suite: shared Data code and the test fixture changed. The new classes are `EntityStatsTests`, `HealthTests`, `DamagePipelineTests`, `DefianceTests`, `FriendlyFireRulesTests`, `ResourcePoolsTests`, `GameClockTests` and `CombatAnchorTests`. `DesignDataParserTests` uses the changed fixture.
- [x] In `Sandbox_Combat`, the readout starts at HP 100 / 100, Armor 5, Damage Reduction 0%, Dodge 5%, Defiance 1%, move speed 6 m/s, every stat 10.
- [x] **Kick:** 45 damage each from Rush Hour (Strength 20), so 3 Kicks incapacitate it. Rarely, a Kick is dodged (5%) or Defiance procs (1%).
- [x] **Reset, then set Toughness to 20:** HP 200 / 200, Armor 10. Hit for 100 from the environment (untick "From attacker") with "Add DR %" at 30: 63 damage.
- [x] **Add DR % at 50 twice** (after Clear mods): Damage Reduction 75%.
- [x] **Set Will to 1000:** Defiance 100%. Hit for 10000: "Defiance!", 1 HP, about 1 s of Invulnerable counting down, Defiance Roll stacks 1, chance 0%. The next lethal hit after that incapacitates.
- [x] **DoT ticked:** the damage ignores Armor and Damage Reduction.

### Fixes after the first run (2026-11-23)
- **The scene command didn't appear in the menu.**
  - **Cause:** Unity never compiled `SandboxCombatSceneCreator.cs`. It was missing from the compile inputs, and reimporting didn't add it, probably because the file was created while a compile was running.
  - **Fix:** the code moved to a new file, `CombatSandboxSceneCreator.cs`, which got a fresh GUID. The old file and its `.meta` were deleted.
  - **Menu label:** now **Create Combat Sandbox Scene**, with no underscore, since Unity can read `_` plus a letter in a menu path as a shortcut key.
- **The scene saved empty data references** (`database`, `hero`, `factionEntity`), so Play logged "needs a Game Database".
  - **Cause:** the command loaded the data before `EditorSceneManager.NewScene`, which unloads unused assets.
  - **Fix:** it now loads the data again after the new scene opens, and stops with an error if that fails.
  - **Result:** after re-running the command, Play runs with no errors (checked by the user).

### Open questions and findings
- **Unity 6.6 writes the Editor log to the project's `Logs/Editor.log`**, not `%LOCALAPPDATA%\Unity\Editor\`. Claude can read it to diagnose errors, read-only.
- **The local design mirror now differs from the design branch:** `constants.csv` has 7 more rows, `stats.md` one more line, and `combat.md` the Uppercut fix below. Refreshing `Docs/design/` from the branch (`SOURCE.md`) would drop them. Push them to the design branch when you're ready.
- **Losing Toughness while above the new max HP** (resolved by the user, 2026-11-23): current HP is capped at the new max, as `stats.md` says.
- **Uppercut's cost** (from Milestone 1; resolved by the user, 2026-11-23): 15 Stamina, as in `modules.csv`. `combat.md`'s regen example now says "Uppercut's 15 Stamina takes 15 s".
- **Defiance slowdown for the player:** the amount and length of "time slows briefly" aren't in the design. It's presentation (Milestone 10), and `GameClock.SetGlobalScale` is its hook.

### Deferred
- Slows, statuses, the real cleanse, revives and module slots: Milestone 4.
- Kick knockback, the friendly fire button and the dummy with movement: Milestone 3.
