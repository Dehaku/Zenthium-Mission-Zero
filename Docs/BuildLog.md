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

---

## Milestone 3: Player controller (checked by the user, 2026-11-23)

### Done
- **New assemblies** (approved):

  | Assembly | Folder | References |
  |---|---|---|
  | `Zenthium.Movement` | `Scripts/Movement/` | Time, Entities |
  | `Zenthium.Modules` | `Scripts/Modules/` | Core, Data, Time, Entities, Combat, Movement |

  - `Zenthium.Player` now also references Data, Time, Entities, Combat, Movement and Modules.
  - `Zenthium.Editor` and `Zenthium.Tests.EditMode` now also reference Movement and Modules.
  - Kick, Drag and knockback are shared rules, not player code: Grey and Strongman have Drag, and Taunt and Barrier Shield knock back with Kick's force. The Player assembly only turns input into calls on them.
- **Data:** no CSV changes. Kick uses its row's stat (Strength), damage tier (Normal), `cooldown_s` (3), knockback tier (Normal, 4 m) and range tier (Melee, 2 m). Drag uses its range tier and `capacity_base` / `capacity_per_strength`. Sprint uses `speed_bonus` (0.5).
- **Entities:**
  - `ModuleTag` (`KnockbackResistant`), a stand-in until Milestone 4's modules carry tags: `EntitySetup.ModuleTags`, `Entity.HasModuleTag`, and a **Module Tags** list on `EntityBehaviour`.
  - Dragged state: `Entity.DraggedBy`, `IsBeingDragged`, `SetDraggedBy`.
  - `EntityBehaviour.ClockDriver`, so an entity's other components share its clock.
- **Combat:**
  - `DamagePipeline`: a dragged target can't be hurt (outcome Blocked, like Invulnerable).
  - `KnockbackResolver` (owned by `CombatRulesBehaviour.Knockback`): the tier's distance when the hit lands. Dodged and blocked hits don't knock back. An Incapacitated target still flies if friendly fire would let the source hurt it (missions.md, Capture). `KnockbackResistant` and dragged targets never move.
- **Time:** `GameClock.VelocityScale(exempt)`. Physics already runs at `Time.timeScale` (pause and the global scale), so velocities only add the time-power scale.
- **Movement:**
  - `KnockbackMotion` (plain C#): a fixed distance over several steps. Each step is told how far the path is clear and never goes further, so a knockback can't tunnel.
  - `EntityMotor` (MonoBehaviour): the only writer of an entity's `linearVelocity`. Its modes are carried, then knocked back, then walking. It capsule-casts ahead during knockback, and anything the body would physically collide with stops it, other entities included. Raises `OnKnockbackObstacleHit` (`KnockbackObstacleHit`: collider, point, unused distance) for Milestone 5's wall damage.
- **Modules:**
  - `ModuleCooldown`: real seconds via `GameClock.CooldownDelta`.
  - `MeleeArc`: an in-front test on XZ.
  - `SprintToggle`: adds and removes Sprint's bonus on `MoveSpeed`.
  - `DragRules`: capacity is `floor(capacity_base + capacity_per_strength x Strength)` (1 at Strength 20, 2 at 100). Targets are picked by kind (citizen, Valuable, any other Incapacitated entity, prop), then by distance. Valuables don't use capacity.
  - `IDragTarget`, `DragTargetKind`, `DragCandidate`, and `EntityDragTarget` (any Incapacitated entity is draggable; see the fix below).
  - `KickAction`: every valid entity within Kick's range and a 60° half-angle arc (a feel value) takes Physical damage from Strength and the damage tier, then is knocked back away from the kicker.
  - `DragAction`: Drag grabs the best target in reach if there's capacity; otherwise it lets go of everything. Targets trail behind like beads on a rope, 1.5 m apart (a feel value). The carrier keeps full speed and can still Kick.
- **Player:**
  - `PlayerController`: owns the generated `InputSystem_Actions` and passes on move, aim, the Sprint toggle, Kick, Drag and the friendly-fire hold (it sets `FriendlyFireRules.IsPlayerFriendlyFireHeld` every frame).
  - `PlayerAim`: casts the mouse onto a flat plane at the player's height, or uses the right stick. The last one used wins, and the aim keeps its direction when the stick is let go.
  - `AimIndicatorView`: turns an arrow to the aim (presentation only).
- **Editor:**
  - `PlayerSandboxSceneCreator`: menu **Zenthium > Setup > Create Player Sandbox Scene**.
  - The serialized-field helpers moved from `CombatSandboxSceneCreator` into `SerializedWiring` so both commands share them. `CombatSandboxSceneCreator.CreateEventSystem`, `CreateDebugPanel`, `EntityFields` and `CombatRulesFields` are now internal for the same reason. Its behavior is unchanged.
- **Tests (EditMode):**
  - New: `KnockbackMotionTests`, `KnockbackResolverTests`, `DragRulesTests`, `ModuleCooldownTests`, `SprintToggleTests` and `MeleeArcTests`.
  - Added cases (no assertions changed): `DamagePipelineTests` (dragged is blocked, released takes damage) and `GameClockTests` (`VelocityScale`).

### Decisions (approved by the user)
- Kick hits every valid target in its arc. The half-angle is a serialized feel value on `KickAction`, starting at 60°.
- Knockback speed is a serialized feel value on `EntityMotor` (20 m/s, so Normal's 4 m takes 0.2 s).
- Anything the body physically collides with stops a knockback, including other entities, since entities collide normally.
- Drag capacity is a target count: `floor(1 + 0.01 x Strength)`.
- Drag button: grab if a target is in reach and there's room; otherwise release everything.
- Stubborn citizens: only the hook now (`IDragTarget.CarrierSlow`, `GrabSeconds`, both 0). The numbers and the slow come in Milestone 6 (for example `stubborn_slow` and `stubborn_grab_s` in Drag's `values`), with `MoveSpeed`'s multiplied slows.
- Kick's damage type is Physical.
- Sprint is a pure toggle: it stays on, even standing still, until pressed again.
- The sandbox and prefabs are built by a menu command.

### Choices made in code (not asked)
- A Kick that hits nothing still starts the cooldown.
- Kick passes over dragged targets entirely (no Blocked event).
- Knockback pushes away from the kicker, or along the aim if the two overlap.
- A dragged body follows its carrier even under Time Slow (the carrier moves it). Walking and knockback follow Time Slow and Time Stop unless the entity is exempt.
- The carrier and what it drags don't collide with each other.
- The carrier lets go of everything when it's Incapacitated or reset. A dragged entity that's reset or disabled is let go.
- Draggable entities: first only Incapacitated `Team.Enemy` entities, now any Incapacitated entity (see the fixes below). An Incapacitated civilian counts as a citizen. Grabbing citizens who are still standing comes in Milestone 6.
- Input is ignored while the clock is paused. Sprint starts off again after an entity reset.

### Stubs
- `ModuleTag` and `EntitySetup.ModuleTags` / the **Module Tags** Inspector list: stand-ins until Milestone 4's modules carry tags.
- `IDragTarget.CarrierSlow` and `GrabSeconds`: Stubborn citizens' hooks, unused until Milestone 6.
- `EntityMotor.OnKnockbackObstacleHit`: nothing listens yet. Milestone 5 adds wall damage.
- `DragTargetKind.Citizen`, `Valuable` and `Prop`: no implementations until Milestones 5 and 6.

### Editor steps for the user
1. **Let Unity compile.** Expect only the existing warning that `Zenthium.Tests.PlayMode` has no scripts. No data re-import is needed.
2. **Run Zenthium > Setup > Create Player Sandbox Scene.** It asks before replacing anything, and asks to save open scene changes. It creates:
   - `Assets/_Project/Prefabs/Player.prefab`: a capsule with Rigidbody (Y position and X/Z rotation frozen, interpolated, continuous dynamic), Entity Behaviour (Rush Hour, Player), Entity Motor, Kick Action, Drag Action, Player Aim, Player Controller, and Aim Indicator View with an `Aim Pivot/Aim Arrow` child (no collider).
   - `Assets/_Project/Prefabs/TrainingDummy.prefab`: a capsule with the same Rigidbody settings, Entity Behaviour (Grey, Enemy, rank D), Entity Motor and Entity Drag Target. No AI.
   - `Assets/_Project/Scenes/Sandbox_Player.unity` (not in the build list): a 30 m walled room with two pillars, the camera following the player, Game Clock, Combat Rules, the player, 3 dummies, and the combat debug panel bound to the first dummy, with the player as attacker.
   - The scene references a prefab can't hold are set on the scene instances: the clock on every entity; Combat Rules on Kick Action and Player Controller; and the camera on Player Aim and the camera rig's target.
3. **Press Play in `Sandbox_Player`.**

### What to check
- [x] The Console has no errors after compiling and running the command.
- [x] Run the whole EditMode suite, since shared code changed (`Entity`, `DamagePipeline`, `GameClock`). The new classes are `KnockbackMotionTests`, `KnockbackResolverTests`, `DragRulesTests`, `ModuleCooldownTests`, `SprintToggleTests` and `MeleeArcTests`. `DamagePipelineTests` and `GameClockTests` have new cases.
- [x] **Move and aim:** WASD and the left stick walk. Left Shift or L3 toggles a visibly faster Sprint. The arrow follows the mouse; the right stick takes over, and moving the mouse takes it back.
- [x] **Kick (Space or A):** a dummy in front within 2 m takes 45 (debug panel) and flies 4 m. It stops at walls, pillars and other dummies, never passes through them. 3 Kicks incapacitate it (the balance anchor). Kick works again only after 3 s. A dummy beside or behind you isn't hit.
- [x] **Kick an incapacitated dummy:** it flies without taking damage.
- [x] **Drag (F or X)** next to an incapacitated dummy: it trails behind you, you keep full speed, and you can still Kick other dummies. Kicks pass over the dragged one. Rush Hour (Strength 20) can drag only one, so F next to a second one lets go instead.
- [x] **Friendly fire (Left Ctrl or B):** set one dummy's Team to Ally in the Inspector before Play. Kicks don't hurt or move it, unless the button is held.
- [x] **KnockbackResistant:** add it to a dummy's Module Tags before Play. It takes damage but doesn't move.
- [x] **Reset dummy** in the panel restores the first dummy, and the carrier lets go of it if it was being dragged.
- [x] **Gamepad:** every input works on a controller.

### Open questions and findings
- **Scene references on prefabs:** a prefab can't hold the scene's Game Clock, Combat Rules or camera, so the command sets them on the instances. Milestone 9's spawner needs another way to hand them over, such as a scene service the entities look up once. Decide when spawning exists.
- **Damage types aren't in modules.csv** (Milestone 1 finding). Kick's Physical is a code constant until they get a data home.
- **Reading `Logs/Editor.log`:** reading it through a shell command was refused by the permission check, but Claude's own file-reading and search tools can read it.
- **Dragging Incapacitated allies** (found by the user; resolved 2026-11-23): see the fixes below.

### Fixes after the first compile (2026-11-23)
- **`Zenthium.Combat` didn't compile:** "The type or namespace name 'KnockbackRules' could not be found".
  - **Cause:** the same as Milestone 2. `KnockbackRules.cs` had a `.meta` file but was missing from the assembly's compile inputs, probably because it was created while Unity was compiling.
  - **Fix:** the class was recreated in a new file, as `KnockbackResolver` (`Combat/KnockbackResolver.cs`), and the old file and its `.meta` were deleted. The test class is now `KnockbackResolverTests`, with the same tests. `CombatRulesBehaviour.Knockback` keeps its name.
- **First play (checked by the user):** the scene builds, all tests pass, and there are no errors. Move, aim, Sprint, Kick (knockback stops at walls) and Drag work.
- **The player kept spinning when no aim input was held.**
  - **Cause:** the Rigidbody's Y rotation was free. Bumps left the body turning, and the aim arrow, a child of the body, turned with it until an aim input set it again.
  - **Fix:** the command now freezes all rotation, as well as Y position. Entities face by aim, not physics. `CLAUDE.md` (Space and physics) and the BuildPlan summary now say rigidbodies freeze Y position and all rotation (approved by the user).
- **Entity info was hard to read** (asked by the user): `Sandbox/EntityDebugLabel` (Sandbox assembly) is a small world-space uGUI label over each entity. It shows team, HP, and Incapacitated, Dragged or Invulnerable. The command adds it to the scene instances only, so the prefabs stay free of sandbox parts. It's a test tool; Milestone 10 builds the real HUD.
- **Editor step:** run **Zenthium > Setup > Create Player Sandbox Scene** again and choose Replace. It rebuilds both prefabs and the scene, so Inspector changes made to them are lost.
- **Any Incapacitated entity can be dragged** (the user's decision, 2026-11-23), allies and neutrals included.
  - **Design docs (local edit, at the user's request):**
    - `modules.md`: the Drag row now says "then any other Incapacitated (0 HP) entity, allies and neutrals included", and Telekinetic Grab grabs an "Incapacitated entity".
    - `statuses.md`: the Incapacitated row says any Incapacitated entity can be Dragged.
  - **Code:**
    - `EntityDragTarget` no longer checks the team.
    - `DragTargetKind.IncapacitatedEnemy` is renamed `IncapacitatedEntity` (`DragRulesTests` updated for the rename only).
    - An Incapacitated civilian counts as `Citizen`, so Drag picks it first.
  - **To check:** set a dummy's Team to Ally or Neutral, defeat it (hold friendly fire for an Ally), and press F next to it.
  - Like the Milestone 2 edits, these design changes are only in the local mirror, not on the design branch.

### Deferred
- Kick throwing a dragged prop and launching Valuables; prop Strength limits; wall damage from knockback: Milestone 5.
- Citizens, Stubborn's slow and grab time: Milestone 6.
- Module tags from data, slows, Sprinter's bigger bonus: Milestone 4.

---

## Milestone 4a: Modules and statuses, shared parts and Rush Hour (implemented, not yet checked, 2026-11-23)

Milestone 4 is split, as approved. **4a** (this entry) builds the shared module parts, statuses, slots, time powers, MoreInfo, Rush Hour's kit and the hero select sandbox. **4b** builds Major Overclock's and Rune's kits.

### Done
- **Data (local edits, approved; the design branch isn't updated):**
  - `modules.csv`: new `damage_type` and `tags` columns after `values`.
    - Tags for all 158 rows come from `modules.md`'s Tags column, written like `Ranged; Debuff; Resist:Will`.
    - Damage types are set where the docs state one, plus the approved choices: Kick, Speed Punch, Uppercut, Shockwave Stomp and Breaching Charge are Physical; Ink Lash is Magic.
  - `heroes.csv`: new `actives`, `passives` and `gear` columns, holding each hero's kit from `heroes.md` in unlock order, `;`-separated.
  - `constants.csv`: `slots_per_type` (4), `preset_slots_unlocked` (1), `cc_duration_multiplier` (0.5), `cc_pulse_on_s` and `cc_pulse_off_s` (0.5 each).
- **Data assembly:**
  - `ModuleTag` moved from Entities to Data (with its `.meta`) and lists every plain tag. `Resist:<Stat>` and `NoRegen:<Resource>` are read as `ModuleData.ResistStats` and `NoRegenResources`.
  - `ModuleData`: `TryGetDamageType`, `HasTag`, `TryGetSlotType`. `HeroData.GetKit(SlotType)`. New `SlotType`, `ModuleCategories` and `ModuleConstants` (`GameDatabase.ModuleConstants`).
  - `Zenthium.Tests.EditMode` can see Data's internals (an approved exception to `writing-tests.md`), so tests build module data with the importer's `Populate`.
- **Importer:**
  - `ModuleTagParser` reads the tags.
  - Hero kits are checked: each module exists, fits its column's slot type and appears once, and each column lists no more than `slots_per_type`.
- **Entities:**
  - **`ModuleSlots`** replaces the `Entity.Modules` stand-in:
    - 4 slots per type, filled in order, locked or unlocked, no duplicates; base actions stay outside.
    - Only unlocked modules grant resources and tags.
    - Disables switch single slots off; `CannotBeDisabled` modules are skipped.
  - **Setup:** `EntitySetup.ForHero(hero, team, unlockedSlotsPerType)` adds the kit. `UnlockedSlotsPerType` defaults to all, for faction entities.
  - **Module tags:** the `ModuleTags` stand-in and the **Module Tags** Inspector list are gone. `Entity.HasModuleTag` reads the equipped modules.
  - **Resources:** `ResourcePools.GrantForModule` also adds resource passives' `<resource>` and `<resource>_regen` values (on top of the +1) and the `NoRegen` tags.
  - **Other additions:**
    - `MoveSpeed.AddSlow`: slows multiply after bonuses add.
    - `DamageModifiers`: dealt and taken, multiplied.
    - `Invulnerability` holds, for Dash.
    - On `Entity`: `Revive`, `OnRevived`, `OnHitDealt`, `OnDefeatedTarget`, and incoming hit handlers (`IIncomingHitHandler`).
  - `IDefianceCleanse` is removed: a Defiance proc now cleanses through the statuses.
- **Statuses** (`Scripts/Statuses/`, compiled into `Zenthium.Entities` by `Statuses.asmref`, approved):
  - `StatusType` (statuses.md's catalog minus its States), `StatusCatalog` (kind, stacking, hostile, what it stops, which slot it disables), `StatusApplication` and `StatusModifier`.
  - **`StatusEffects`** on every entity:
    - Refresh, Intensity (layers with a cap, modifiers scaled by layers) and Independent stacking.
    - Durations on game time.
    - Half-length crowd control on Leaders and the player, and "while inside" areas that pulse on them.
    - Invulnerable blocks hostile statuses; Defiance cleanses them.
    - Disables, Stun and Root (`CanMove`, `CanAct`, `IsSilenced`).
  - `ResistRules` and `CrowdControlPulse`.
- **Combat:**
  - The pipeline applies dealt and taken modifiers with the rank bonus, before Armor.
  - It runs incoming hit handlers on direct hits, and raises the source's hit and defeat events.
  - `DamageEvent.KnockbackPrevented`, which `KnockbackResolver` respects.
- **Movement:** `EntityMotor` doesn't walk while Stunned or Rooted. It has a dash mode (`TryDash`, `OnDashEnded`) that stops at walls like a knockback; the dash speed is serialized (30 m/s, a feel value).
- **Time:** `TimePowers`, owned by `GameClockDriver`: several time powers can run at once, the slowest wins, and durations are real seconds that stop while paused.
- **Modules (the shared parts):**
  - `ModuleActivation`: the gate every activated module shares (can act, slot usable, cooldown in real seconds, cost).
  - `ModuleBehaviour`: the base for behaviors, with the triggers (on hit, on damage taken, on Defiance proc, on defeat, timer tick) and enable/disable for slot disables.
  - `ModuleCatalog`: links module ids to behaviors and reports missing data.
  - `StatFormula`: damage from stat and tier, and the MoreInfo text with live numbers.
  - `HitEffects`: damage, knockback, then a status with its resist roll, shared by every delivery.
  - Deliveries:
    - `MeleeStrikeModule` and `AreaStrikeModule`.
    - `ProjectileModule` with `Projectile`: it sweeps, so it can't tunnel, and freezes with no collision under Time Stop.
  - **`EntityModules`** (MonoBehaviour) replaces `KickAction`, which is deleted. It runs Kick and each equipped slot, ticks cooldowns and timers, forwards trigger events, and turns modules off while their slot is disabled.
  - `ModuleWorld` (scene service): data, combat rules, clock, entity queries, projectile spawning and extraction.
  - `ModuleInfoText`: the MoreInfo text.
- **Kits:**
  - **Rush Hour:** Speed Punch and Uppercut (melee strikes), Dash, Time Slow, Sprinter, Weather the Storm, Express Delivery, Charming (partly), First Aid Spray, Evac Balloon (partly), Padded Suit, Megaphone (partly).
  - **Also:** Kick (now built from the shared parts), Time Stop, Energy Blaster (for the sandbox turret) and Immovable (tag only).
  - **Sprinter and Express Delivery:** Sprint's bonus comes from `SprintToggle.BonusFor`, where Sprinter's `sprint_bonus` replaces Sprint's. Drag's citizen-only capacity comes from `DragRules.CitizenCapacity`.
- **Player:** `PlayerController` uses Power 1-4 (active slots), Gear 1-4 (gear actives) and Kick through `EntityModules`, and exposes `IsMoreInfoHeld`.
- **Sandbox** (its asmdef now also references Movement, Modules and Player, approved):
  - New: `HeroSelectPanel`, `ModuleInfoView` and `SandboxAutoCaster`.
  - `EntityDebugLabel` lists statuses, hidden markers included.
  - `CombatDebugPanel`'s Kick uses Kick's data damage type.
- **Editor:**
  - `PlayerSandboxSceneCreator` builds the room through `TryBuildRoom`, shared by both commands. It adds `EntityModules` to both prefabs, plus a Projectile prefab and a Module World.
  - New **Zenthium > Setup > Create Modules Sandbox Scene** (`ModulesSandboxSceneCreator`).
  - Some `CombatSandboxSceneCreator` UI helpers are now internal so the commands can share them.
- **Tests (EditMode):**
  - **New classes:** `StatusEffectsTests`, `ResistRulesTests`, `ModuleSlotsTests`, `ModuleActivationTests`, `TimePowersTests`, `StatFormulaTests`, `MoveSpeedTests`, and the helper `TestModules`.
  - **New cases (no assertions changed):**
    - `DamagePipelineTests`: dealt and taken modifiers, the incoming hit handler (DoTs skip it), hit and defeat events.
    - `KnockbackResolverTests`: cancelled knockback.
    - `DragRulesTests`: citizen capacity.
    - `SprintToggleTests`: Sprinter.
    - `ResourcePoolsTests`: resource passives and NoRegen.
    - `DesignDataParserTests`: tags, damage type and kits.
  - **Changed for a removed stand-in:** `KnockbackResolverTests.TryGetDistance_KnockbackResistant_DoesNotMove` now gets the tag from an equipped module.
  - **Deleted:** `DefianceTests.Proc_CallsTheCleanse`, which tested the removed `IDefianceCleanse` stub. `StatusEffectsTests.DefianceProc_CleansesDebuffs_KeepsCarryingTreasureAndHiddenMarkers` covers the real cleanse.
  - **Fixtures:** `CombatTestSetup` builds the module constants and has `CreateEntityWithModules`; `DesignDataTestCsv` has the new columns and constants.

### Decisions (approved by the user)
- The split into 4a and 4b; the data columns, constants, `.asmref` and asmdef changes; the damage types above.
- Ink Beast gets a placeholder brain until Milestone 6 (4b).
- Glyph Trap lasts until triggered, roots every enemy in its radius once, and one is out at a time (4b).
- Energy Blaster's projectile speed is a feel value on the Projectile prefab (15 m/s) until projectile modules carry their own.
- Tests may use Data's internals to build module data.
- **Accepted assumptions** (most are for 4b's modules):
  - Taunt's `duration_s` covers Taunted, Marked and the self Damage Reduction.
  - Marked and Overclocked apply before Armor, with the rank bonus.
  - Sprinter's bonus replaces Sprint's.
  - Battery regen adds to the +1.
  - Repulsor Blast is an instant beam.
  - Power Throw throws dragged entities 15 m, with no damage until props exist.
  - First Aid Spray revives a citizen at 25% of max HP.
  - Fresh Ink counts the user's own defeats.
  - Deathly Rage's x3 is +200%.
  - Reactive Plating stacks on hits that land.
  - Breaching Charge's fuse runs on game time.

### Choices made in code (not asked)
- **Statuses:**
  - A refresh never shortens the time left: the timer becomes the longer of what's left and the new duration. Intensity does the same and adds a layer, up to its cap.
  - Only hostile statuses (debuffs, DoTs, crowd control, disables, Pied) are blocked by Invulnerable and cleansed by Defiance. Carrying Treasure and hidden markers aren't hostile.
  - Disables pick a random equipped slot that isn't disabled yet; with none left, the application doesn't take. Disabled modules keep their tags and the resources they granted.
  - A status from a hit applies only if the hit lands (not dodged or blocked). A module with no damage, such as Megaphone, reaches whoever the user could hurt.
- **Rush Hour's modules:**
  - Weather the Storm's extra reduction multiplies with the target's Damage Reduction; the 5% minimum still applies.
  - First Aid Spray next to a downed citizen needs the slot usable and the user able to act, but spends nothing and starts no cooldown.
  - Evac Balloon isn't used (no cooldown) when nothing is in reach. It floats the target up for 1.5 s at 3 m/s (feel values on Module World), then deactivates it.
  - Charming rolls only for enemy goons for now; citizens wait for Stubborn in Milestone 6.
  - Dash needs the user able to move. A knockback, being carried or a reset ends it and its Invulnerable.
- **Other:**
  - Every entity with `EntityModules` gets Kick; enemies won't use it until they have AI.
  - The sandbox's test module replaces the last module of its slot type, or fills the next slot.
  - Projectiles pass through entities their shooter can't hurt, and through Incapacitated and dragged ones.

### Stubs
- **Charming:** `ICharmable` is the conversion hook, for Milestone 6's AI. Stubborn citizens' roll also comes in Milestone 6.
- **Rescue and capture count:** `ModuleWorld.OnExtracted` is Evac Balloon's hook, for Milestone 9.
- **Citizens (Milestone 6):** Evac Balloon on standing citizens, First Aid Spray making revived citizens Scared, and Megaphone's citizen effects.
- **DoT ticking:** the status system holds DoTs, but ticks need an interval that isn't in the data yet. It comes with the first DoT module (Burn, in 4b or Milestone 5).
- **Taunted and Hypnotized:** applied and shown on the label, but only AI reads them (Milestone 6).

### Editor steps for the user
1. **Let Unity compile.** If the Console says a type is missing although its file exists, it was skipped (the known issue). Tell Claude, which recreates it under a new file name.
2. **Run Zenthium > Data > Import Design CSVs.** Expect 158 modules, 9 factions, 50 entities, 8 heroes, 5 props, 10 prices, 0 warnings.
3. **Run Zenthium > Setup > Create Player Sandbox Scene** and choose Replace. The Player prefab still holds the deleted Kick Action, so it must be rebuilt. This also creates `Prefabs/Projectile.prefab`.
4. **Run Zenthium > Setup > Create Modules Sandbox Scene.** It creates `Assets/_Project/Scenes/Sandbox_Modules.unity` (not in the build list), with:
   - the Player sandbox's room;
   - a turret dummy (Grey with Energy Blaster) that fires at the player every 2 s, in the top-right corner;
   - a civilian dummy in the bottom-left corner;
   - the hero select panel at the top right, and the MoreInfo panel at the right.
5. **Press Play in `Sandbox_Modules`.**

### What to check
- [ ] The Console has no errors after compiling, importing and running both commands.
- [ ] Run the whole EditMode suite, since shared code changed (`Entity`, `DamagePipeline`, `EntityRules`, the importer).
  - New classes: `StatusEffectsTests`, `ResistRulesTests`, `ModuleSlotsTests`, `ModuleActivationTests`, `TimePowersTests`, `StatFormulaTests`, `MoveSpeedTests`.
  - Changed classes: `DamagePipelineTests`, `KnockbackResolverTests`, `DragRulesTests`, `SprintToggleTests`, `ResourcePoolsTests`, `DesignDataParserTests`, `DefianceTests`.
- [ ] **Hero select:** pick Rush Hour with "Unlock all slots" on, and press Apply.
  - Hold MoreInfo (`controls.md`): every module is listed, with Kick's `Damage = (10 + Strength(20) x 2) x Normal(1) = 50`.
  - Untick "Unlock all slots" and press Apply: only Speed Punch, Sprinter and First Aid Spray are unlocked.
- [ ] **Power 1 (Speed Punch):** 45 damage to a dummy (50 minus its 5 Armor), no knockback, 1 s cooldown.
- [ ] **Power 4 (Uppercut):** the same damage; the dummy flies 15 m; it costs 15 Stamina.
- [ ] **Power 2 (Dash):** 8 m along the aim, stops at walls, and you're Invulnerable during it.
- [ ] **Power 3 (Time Slow):** for 10 s the dummies, the turret's shots and its firing run at half speed, but you don't. The cooldown is 30 s in real time.
- [ ] **Time Stop:** pick it as the test module and Apply.
  - The turret's shots freeze in the air, and you can walk through them.
  - Frozen dummies still take Kick damage, and fly when time resumes.
- [ ] **Turret:** it fires within 15 m (Energy Blaster's range), so walk toward it. Its shots can also hit dummies in the way (enemy friendly fire, the dev toggle).
- [ ] **Sprinter:** Sprint (Left Shift) is +100% instead of +50%.
- [ ] **Weather the Storm:** each turret hit spends 5 Stamina (if you have it), does 25% less damage, and doesn't push you.
- [ ] **Gear 1 (First Aid Spray):** heals 25% of max HP, with a 20 s cooldown. Next to the defeated civilian dummy, it gets it up at 25% HP, free.
- [ ] **Gear 2 (Evac Balloon):** next to a defeated dummy or the defeated civilian, it floats them away.
- [ ] **Gear 3 (Padded Suit):** Armor +30 (shown in MoreInfo).
- [ ] **Gear 4 (Megaphone):** dummies within 10 m are Stunned for 3 s (on their labels), or resist it, about half the time at equal Toughness.
- [ ] **Express Delivery:** you can drag the defeated civilian plus one defeated dummy. Strength 20 alone allows one.
- [ ] **Charming:** walking within 8 m of a dummy marks it Charmed on its label, once.
- [ ] **Major Overclock and Rune:** most of their modules show "(not built yet)" in MoreInfo and warn once in the Console. Those come in 4b.

### Open questions and findings
- **Design mirror:** `modules.csv`, `heroes.csv` and `constants.csv` now differ further from the design branch. Push them when you're ready.
- **DoT tick interval:** not in the data. It's needed before Burn or Infected can deal damage.
- **Shell reads of `Logs/`** are still refused; Claude reads the Editor log with its search tool.

### First run (read by Claude from the log and test results, 2026-11-24)
- The code compiled. The import, both scene commands and Play ran with no errors or exceptions in the log.
- The EditMode suite: 242 tests, all passed (`TestResults.xml`).
- The only warnings are the expected "doesn't work yet" ones for Major Overclock's and Rune's modules.
- **Finding:** Claude can read test results: the Test Runner saves the last run to `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Zenthium-Mission-Zero/TestResults.xml`. `Docs/BuildPlan.md` step 9 now says so.

### Additions after the first run (asked by the user, 2026-11-24)
The sandbox was hard to test without knowing the powers, cooldowns and controls, and the real HUD only comes in Milestone 10. So:
- **Sandbox HUD** (`Sandbox/SandboxHud`, bottom left), a plain-text test tool. It shows:
  - HP and each resource (current / max and regen);
  - Kick, Drag, Sprint (on or off) and every slot, each with its keyboard / gamepad button, read from the input actions;
  - each module's state: READY, seconds of cooldown left, locked, disabled, Silenced, Stunned, not enough resource, or not built yet;
  - each module's cooldown and cost, and the MoreInfo, friendly fire and checklist keys.
- **Test checklist overlay** (`Sandbox/SandboxChecklistView`, center), toggled with **Caps Lock**. It shows `Assets/_Project/Sandbox/MilestoneChecklist.txt`, which now holds this milestone's tests. From now on, every milestone rewrites that file: `BuildPlan.md` step 8 (added at the user's request, so the later steps are renumbered).
- **Input:** a new **Sandbox** action map with **ToggleChecklist** on Caps Lock (keyboard only), in `InputSystem_Actions.inputactions`. Unity regenerates `InputSystem_Actions.cs` from it.
- `PlayerController.IsSprintOn`. The Sandbox asmdef now references `Unity.InputSystem`.
- **Editor:** `SandboxHudSetup` adds both overlays; `PlayerSandboxSceneCreator.TryBuildRoom` calls it, so `Sandbox_Player` and `Sandbox_Modules` both get them.

### Editor steps (for the additions)
1. **Let Unity compile.** If the Console says `PlayerActions` or `InputSystem_Actions` has no `Sandbox`, the input class hasn't regenerated yet: select `Assets/InputSystem_Actions.inputactions` and choose **Reimport**.
2. **Run Zenthium > Setup > Create Modules Sandbox Scene** again and choose Replace. Run **Create Player Sandbox Scene** too if you use that scene.
3. **Press Play in `Sandbox_Modules`.** The checklist shows in the middle of the screen; Caps Lock hides it and shows it again.

### Second run: the user's checklist results (2026-11-24)
- **Passed:** 1-6, 8, 10, 13, 14 and 16. Item 12 also passed: Padded Suit cut turret shots from 10 to 1, the 5% minimum.
- **Item 7 (Weather the Storm): the checklist was wrong.**
  - Energy Blaster has no knockback tier, so turret shots never push anyone.
  - With every slot unlocked, Padded Suit's 30 Armor floors each shot at the 5% minimum, which hides Weather's 25%.
  - It did spend 5 Stamina per hit.
- **Item 9 (Charming): expected behavior.**
  - The three dummies start within 8 m of the player, so they're marked at once.
  - The Charmed mark records that a goon rolled, so every goon in range gets it. The 5% conversion has no visible effect until Milestone 6.
- **Item 11 (Evac Balloon):** it works, but from the overhead camera only the shadow seems to move.
- **Item 15 (turret): the checklist was wrong.** The turret fires every 2 s wherever the player is; the 15 m is how far each shot flies.
- **UI:** the debug panel, HUD, checklist, hero select and MoreInfo panels overlapped and were hard to read.

### Fixes after the second run (2026-11-24)
- **Layout:**
  - Nothing overlaps at the 1920 x 1080 reference. The left column (500 wide) holds the combat debug panel at the top and the HUD at the bottom. The right column (500 wide) holds hero select at the top and MoreInfo below it. The checklist sits in the middle.
  - Smaller text: 15 pt on the panels and HUD, 17 pt on the checklist.
  - Every sandbox canvas scales with **Shrink**, so the whole layout fits any Game view aspect (it overflowed at the bottom before).
  - The combat debug panel's smaller size also applies to `Sandbox_Combat` the next time it's built.
- **Hero select:** the "Unlock all slots" toggle is now an **Unlocked slots** dropdown, 1 to 4, starting at 4. It feeds `EntityBehaviour.SetHero(hero, unlockedSlots, testModule)`. The Inspector field is now **Unlocked Slots** (0 means the preset's starting slots).
- **Turret:** it Kicks the target when it's within Kick's range, and otherwise fires Energy Blaster. That gives a knockback to test Weather the Storm against.
- **Evac Balloon:** the body also shrinks to 20% while it floats away (a feel value on Module World).
- **Checklist:** items 7, 9, 11, 12, 14 and 15 are rewritten to match the above.

### Editor steps
1. Let Unity compile.
2. Run **Zenthium > Setup > Create Modules Sandbox Scene** and choose Replace, then press Play.

### Third run (2026-11-24)
- Items 7, 9 and 11 passed.
- **Evac Balloon (asked by the user):** a shrinking body reads as moving away from an overhead camera, so it now grows to 2x as it rises toward the camera (`extractionEndScale`, a feel value from 1 to 5 on Module World). The scene saved the old 0.2, so rebuild `Sandbox_Modules` or set **Extraction End Scale** to 2 on Module World.
- The user checked the rest; Milestone 4a was committed as `8f9e835`.

---

## Milestone 4b: Modules and statuses, Major Overclock and Rune (implemented, not yet checked, 2026-11-24)

### Done
- **No data, package or assembly changes.** Every number comes from the existing rows of `modules.csv` and `entities.csv`.
- **Shared parts:**
  - **`ModuleAim`:** an aim direction plus how far away the aimed point is. It replaces the plain direction in every module's activation (`TryActivate`, `CanActivateNow`, `Activate`, `EntityModules.TryKick` and `TryActivateSlot`). `PlacementPoint` clamps a placed module to its range.
    - `PlayerAim.PointDistance` gives the mouse's distance on the ground; the stick has none, so placed modules use their full range.
    - `EntityModules.SetAim` keeps the live aim on the module context for effects that follow it (shields). The player sets it every frame.
  - **Summons:**
    - `Entity.Summoner` (through `EntitySetup.Summoner`).
    - `EntityBehaviour.ConfigureSummon` sets a spawned body up before it activates.
    - `ModuleWorld.SpawnSummon` builds the body under an inactive holder, then activates it. A player's summons are allies.
    - `PlaceholderSummonBrain`, until Milestone 6: chase the nearest opponent within 15 m, use the first active slot in reach, otherwise follow the summoner within 10 m (feel values).
  - **Module World:**
    - An entity lookup (`TryGetBehaviour`), registered by `EntityModules`.
    - Spawning glyphs, smoke clouds, charges, shields and summons.
    - Active shields, which projectiles check.
    - A brief beam visual.
  - **Defiance:** `AddChanceBonus` / `RemoveChanceBonus`, added to the chance from Will.
  - **`HitEffects`:**
    - Can scale damage with the summoner's stat (Ink Lash).
    - Adds `SpellDamageBonus`: equipped modules' `magic_hit_damage`, on the user's Magic hits and on all its summons' hits, into the same hit.
  - **Other helpers:**
    - `Opponents`: player and allies against enemies and neutrals; civilians on neither side.
    - `SweepHit`: shared by projectiles and beams.
    - `EntityMotor.IsKnockedBack`.
    - `DragAction.CarriedCount` and `ReleaseAll(list)`.
    - `IBreachable`, a stub for Milestone 5's walls and doors.
- **Major Overclock:**
  - **Taunt** (`TauntModule`): enemies within vision range (100 m) and line of sight (walls block, entities don't) get Taunted and Marked for `duration_s`, with one Will resist roll for both. For that time the user takes `self_damage_reduction` less, and anyone who hits them is knocked back with Taunt's tier (Normal).
  - **Shockwave Stomp:** an area damage strike.
  - **Power Throw** (`PowerThrowModule`): releases everything dragged, starts it 1.2 m in front, and throws it the range tier (15 m) as a swept knockback. No damage until props.
  - **Energy Shield and Barrier Shield** (`ShieldModule`, `ShieldZone`): a crescent following the aim for the duration tier, with a 1.5 m reach (feel value). It stops projectiles from anyone who could hurt the user and pushes opponents in its arc with the knockback tier.
  - **High-Capacity Battery, Immovable:** data only.
  - **Reactive Plating:** a layer of +`armor_per_hit` Armor per hit that lands, up to `armor_max`, through the Intensity status.
  - **Overclocked:** +`damage` dealt while Charge is at least `charge_above` of its max.
  - **Repulsor Blast** (`BeamModule`): an instant beam with a 0.25 m sweep radius, to the first entity the user can hurt or a wall.
  - **Breaching Charge** (`BreachingChargeModule`, `TimedCharge`): placed up to its range ahead. Its fuse runs on game time, then it hits everything in the radius and calls `IBreachable` on anything it finds.
  - **Stasis Cuffs:** `ExtractionModule`, enemies only. `EvacBalloonModule` was renamed `ExtractionModule` (`.meta` moved) and handles both.
- **Rune:**
  - **Ink Beast** (`SummonModule`): 1 + `extra_beasts` summons of the Ink Beast entity, attacking with `summon_attack` (Ink Lash), for `summon_lifetime_s`. A recast replaces the ones out.
  - **Ink Lash:** a melee strike using the summoner's Will.
  - **Glyph Trap** (`GlyphTrapModule`, `PlacedGlyph`): at the aim point, up to its range; roots every opponent inside when the first steps in, then disappears. One at a time.
  - **Smoke Bomb** (`SmokeBombModule`, `SmokeCloud`): a cloud at the aim point. It stuns opponents while inside (the "while inside" status), with one Toughness resist roll each on entering.
  - **Deathly Rage:** the Defiance bonus while equipped; on a proc, the Deathly Rage status with Strength and Toughness x3.
  - **Second Wind, Fresh Ink:** `HealOnTriggerPassive`.
  - **Living Ink, Force Spell Tome:** data only, read by Ink Beast and `HitEffects`.
  - Speed Punch, Padded Suit and First Aid Spray were already built in 4a.
- **The catalog** (`ModuleCatalog.TryCreate`) now takes the database (for the summon's entity), and each module is one line calling a builder.
- **Editor:**
  - `ModulePrefabBuilder` builds the module prefabs in `Prefabs/Modules/` and flat-colored materials in `Prefabs/Materials/`: Summon, Glyph, SmokeCloud, Charge (red circle + countdown via `Sandbox/ChargeView`), Shield, BeamVisual. Visuals have no colliders. `TryBuildRoom` wires them to Module World.
  - In `Sandbox_Modules` the combat debug panel now inspects and hits the player, with the turret as attacker (`CombatSandboxSceneCreator.BindDebugPanel`), so Defiance, Taunt and Armor can be tested on yourself.
- **Checklist:** `MilestoneChecklist.txt` holds 4b's tests.
- **Tests (EditMode):**
  - New `ModuleRulesTests`: placement point, summon count, spell damage bonus, Overclocked's threshold.
  - New case in `DefianceTests`: a full chance bonus guarantees one proc, then the roll stack cancels it.

### Decisions (approved by the user)
- Only Ink Lash uses its summoner's stat, for now.
- Force Spell Tome's +15 goes into the same hit, on the user's Magic hits and all its summons' hits.
- Smoke Bomb rolls each enemy's resist once, on entering.
- The shield's reach is a feel value on the shield prefab.
- Taunt uses the 100 m vision range with line of sight, and one Will roll for both statuses.
- Power Throw throws everything dragged.
- Breaching Charge follows the user's friendly fire rules.
- An Ink Beast recast replaces the beasts already out.

### Choices made in code (not asked)
- **Which side is the enemy:**
  - Area and sight modules (Taunt, Glyph, Smoke, the shield push, the summon brain) target `Opponents`: the other side, never civilians.
  - Damage still follows friendly fire.
- **Taunt's push on attackers:** goes through `KnockbackResolver`, so KnockbackResistant attackers stay put.
- **Placed modules** (Glyph Trap, Smoke Bomb, Breaching Charge): land at the mouse's point on the ground plane at the user's height, clamped to their range.
- **Breaching Charge:** goes 2 m ahead (its Melee range), or nearer if the mouse is nearer.
- **Summons:** spawn in a ring 1.5 m around the user. Their lifetime runs on the summoner's game time. They're removed when the module is unequipped or the user is reset.
- **Fresh Ink:** counts opponents only.

### Stubs
- `IBreachable`: nothing implements it until Milestone 5.
- `PlaceholderSummonBrain`: replaced by Milestone 6's AI.
- Taunted: applied, but only AI reads it (Milestone 6).
- Smoke Bomb's line-of-sight block and awareness reset: Milestone 6.
- Power Throw's damage and prop size limits: Milestone 5.

### Editor steps for the user
1. **Let Unity compile.** If the Console says a type is missing although its file exists, tell Claude.
2. **Run Zenthium > Setup > Create Modules Sandbox Scene** and choose Replace. It now also builds `Prefabs/Modules/` and `Prefabs/Materials/`.
3. **Press Play in `Sandbox_Modules`** and work through the checklist (Caps Lock).

### What to check
- [ ] The Console has no errors after compiling and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`Entity`, `Defiance`, `HitEffects`, the module signatures). New: `ModuleRulesTests`; changed: `DefianceTests`.
- [ ] Every item on the in-game checklist: 8 for Major Overclock, 7 for Rune.

### First run (2026-11-24)
- All 15 checklist items passed. 254 EditMode tests passed, and the log shows no errors.
- **Stasis Cuffs** floated its target away like Evac Balloon. The user decided it captures in place instead.

### Captured state (the user's design decision, 2026-11-24)
- **The rule:** Stasis Cuffs gives a downed enemy a permanent **Captured** state. It lies where it is, and nothing affects it any more: it can't be hurt, knocked back, dragged, revived or given statuses, and its statuses end.
  - Like other local design decisions, it isn't in `statuses.md` yet. It can be added there, or pushed to the design branch, when you like.
- **Code:**
  - `Entity.Capture()`, `IsCaptured` and `OnCaptured`. Only an Incapacitated entity can be captured, so captured entities also take no damage (the pipeline already refuses Incapacitated targets).
  - `StatusEffects` refuses new statuses on captured entities; `ClearAll` ends the existing ones.
  - `Entity.Revive`, `KnockbackResolver` and `EntityDragTarget` refuse captured entities. A carrier lets go when its target is captured.
  - `ModuleWorld.Capture`. The extraction event is renamed `OnRescuedOrCaptured`, and Milestone 9 counts it.
  - `ExtractionModule` takes an `ExtractionMode`: Evac Balloon floats a citizen or enemy away; Stasis Cuffs captures in place. Neither picks a captured target.
  - The debug label shows "Captured".
- **Tests:** new `CaptureTests`: only Incapacitated entities can be captured; capture ends statuses; no new statuses, revive or knockback afterwards. The knockback case would fail without the fix.
- **Checklist:** item 8 is updated.
- **Later milestones:** Milestone 6's AI and Milestone 9's mission goals must treat captured entities as out of play.

### Editor steps
1. Let Unity compile, then press Play in `Sandbox_Modules` (no rebuild needed).
2. Run the whole EditMode suite; the new class is `CaptureTests`.

### Friendly fire on V (asked by the user, 2026-11-24)
- **Why:** in the Editor, Left Ctrl with 1-4 opens Editor windows, so friendly fire couldn't be held while using gear.
- **Change:** the keyboard binding moved to **V** in `InputSystem_Actions.inputactions`, and in the JSON embedded in the generated `InputSystem_Actions.cs`. The gamepad stays on B. The sandbox HUD reads the new key from the asset.
- `controls.md` still says Left Ctrl. Decide whether V stays for good (and update the doc), or goes back to Left Ctrl for builds, where the Editor shortcuts don't apply.
- The user checked the Captured state in play.

---

## Milestone 5a: World, structures, props and buildings (implemented, not yet checked, 2026-11-24)

Milestone 5 is split, as approved. **5a** (this entry) builds walls, doors, windows, props, vehicles, the building structure, the Heavy Turret and `Sandbox_World`. **5b** builds DoT ticks, fire and Valuables.

### Done
- **Data (local edits, approved; the design branch isn't updated):**
  - `constants.csv`: a new `world` group:
    - `material_hp.<Material>`: placeholder HP for Glass 10, Wood 100, Stone 300, Metal 600, Crystalline 1000.
    - `impact_structure_damage_tier` (Heavy) and `impact_entity_damage_tier` (Light).
    - `building_impact_multiplier` and `demolition_multiplier` (10 each).
    - `vehicle_explosion_delay_s` (1) and `building_neighbor_radius_m` (20), from world.md.
  - `modules.csv`: Breaching Charge gets the new `Demolition` tag. Heavy Gun gets `damage_type` Physical (an assumption: confirm it).
  - Code: `WorldConstants` (`GameDatabase.WorldConstants`), `StructureMaterial`, `LocationType` (maps.md's list) and `ModuleTag.Demolition`. The constants parser reads the `world` group; every material needs an HP.
- **Combat:**
  - `DamagePipeline.ApplyToStructure`: the source's rank bonus and dealt modifiers, then Armor, Damage Reduction and the 5% minimum, the same math as entities. DoTs skip the defenses.
  - `Hit` can be marked as impossible to dodge (`canBeDodged`), for wall impacts.
  - `KnockbackResolver`: entities with the new `EntityTag.Immobile` never move.
- **Entities:** `EntityBehaviour` has an Inspector list of entity tags, used for the turret until `entities.csv` carries tags.
- **Movement:** `EntityMotor.ApplyKnockback` takes a `KnockbackCause` (who, and their stat), passed on in `KnockbackObstacleHit`.
- **Modules (hooks, so Modules never references World):**
  - `IDamageableStructure`, `StructureHit` and `StructureHitKind` (Direct, Impact, Demolition). `ModuleWorld.CollectStructures` and `TryGetStructure`.
  - Every damaging delivery hits structures: melee and area strikes, projectiles, beams and charges (`HitEffects.ApplyToStructure`). `SweepHit` reports what solid thing stopped it.
  - `KnockbackImpacts` (on every entity body): knocked into a wall, door, window or prop, the thing hit takes the Heavy tier and the entity the Light tier, from the knocker's module stat (Strength for a module without one). The entity's damage can't be dodged. Entities hitting entities do nothing.
  - `ImpactDamage`, the impact numbers and the knockback cause. Knockbacks from hits, Taunt, shields and Power Throw carry a cause.
  - `KickModule`: Kick throws the first dragged prop instead of striking, as far as Kick's knockback tier, using Kick's cooldown.
  - `IThrowable` and `ThrowInfo`. `PowerThrowModule` throws props through it; thrown entities still deal nothing.
  - `DragAction.TryReleaseFirstThrowable`. `EntityDragTarget` refuses Immobile entities.
  - `LineOfSight` with the `SeeThrough` marker (windows). Taunt uses it.
  - Heavy Gun is in the catalog as a projectile module.
  - `IBreachable` is deleted (with its `.meta`): Breaching Charge now damages structures, and its Demolition tag multiplies the damage to buildings.
- **World (new `Zenthium.World` assembly, `Scripts/World/`, approved):**
  - `StructureHealth` (plain C#) and `Structure`: HP, Armor, Damage Reduction, `OnBroken`; not an entity.
  - `StructureDamageRules`: the impact and Demolition multipliers, for walls, doors and windows only.
  - `BuildingPart`: a wall, window or door panel; its material sets its HP. Broken, its colliders, visuals and NavMesh carving go.
  - `Door`: a trigger box around the doorway with its panel as a child. Unlocked, it opens while an entity is in the box. Locked, it stays solid until broken.
  - `PropBody` (`IDragTarget`, `IThrowable`) with `PropRules`: the Strength gate, thrown damage (Kick x `thrown_x_kick`, to the target and to itself), Small props breaking when thrown. Props are kinematic, so nothing shoves them, and fly with a swept box that can't tunnel.
  - `VehicleExplosion`: `vehicle_explosion_delay_s` of game time after destruction, `explosion_damage` to every entity, wall and prop within `explosion_radius_m` (environmental and Physical). Explosions chain.
  - `Building`, `InteriorZone` (live lists of the entities and props inside), `MapSocket` (weighted `SocketContent` options), `BuildingRegistry` (IDs, and neighbors computed once on Start) and `BuildingNeighbors` (plain C#, edge-to-edge distance between interior zones).
- **Sandbox:** `StructureDebugLabel` (HP labels that follow their target) and `ExplosionRingView` (the red ring). The Sandbox asmdef references World.
- **Editor:**
  - New **Zenthium > Setup > Create World Sandbox Scene** (`WorldSandboxSceneCreator`), on a 60 m room. It holds:
    - a Wood house with an unlocked door and a window;
    - an L-shaped Stone shop with two interior zones, a locked Metal door and a window, 19 m from the house (neighbors);
    - arson and Valuables sockets;
    - a sample wall of each material;
    - one prop of each props.csv row;
    - a Heavy Turret (Neutral, Hackable, Immobile, Heavy Gun) that shoots a dummy next to it, saved as `Prefabs/HeavyTurret.prefab`;
    - the hero select and MoreInfo panels, with the debug panel on the player;
    - a NavMesh baked from the room only (`Scenes/Sandbox_World_NavMesh.asset`). Building walls and windows carve it at runtime; doors don't.
  - `TryBuildRoom` takes a room size. Every entity body gets `KnockbackImpacts`. `ModulePrefabBuilder.SaveMaterial` and `ModulesSandboxSceneCreator.CreateModulesCanvas` are internal, to share them.
  - The Editor asmdef references World and `Unity.AI.Navigation`.
- **Checklist:** `MilestoneChecklist.txt` holds 5a's tests.
- **Tests (EditMode; the test asmdef references World):**
  - New classes: `StructureHealthTests`, `StructureDamageRulesTests`, `PropRulesTests`, `BuildingNeighborsTests`, `ImpactDamageTests`.
  - New cases (no assertions changed): `DamagePipelineTests` (structure mitigation, rank bonus, DoTs, an undodgeable hit), `KnockbackResolverTests` (Immobile), `DesignDataParserTests` (world constants, a missing material, an unknown material).
  - Fixture: `DesignDataTestCsv` has the `world` rows and `damage.Heavy`, and `Parse` can take a whole constants file.

### Decisions (approved by the user)
- The split into 5a and 5b; the new assembly and asmdef references; the constants above.
- Material HP is placeholders, HP only (no Armor).
- Sourceless damage (wall impacts, explosions, fire) is Physical.
- Structures aren't entities. Everyone can damage them, and attackers' damage bonuses apply.
- Wall impacts: Heavy to the structure and Light to the entity, from the knocker's stat. Walls, doors and windows take 10x impact damage on top, so knockback and throws break them easily.
- Thrown props: a Kick throw goes Kick's knockback tier, Power Throw its range tier. They damage the first thing hit, with no knockback, and take the same. A prop that lands on nothing takes nothing, except Small props.
- Vehicle explosions hurt every entity, wall and prop in range, with Armor and Damage Reduction applying.
- Breaching Charge: a Demolition tag gives 10x damage to buildings, instead of destroying them outright.
- Heavy Turret: never walks, is never knocked back, can't be dragged (the `Immobile` tag); Heavy Gun set by the sandbox.
- Neighbors are measured between interior zones' edges.
- Locked doors are an Inspector flag until Milestone 9 picks door materials by rank.

### Choices made in code (not asked)
- **What counts as a building part:** anything with `BuildingPart`, including the free-standing sample walls, takes the building multipliers. Props never do.
- **Impacts:** only knockbacks with a source deal impact damage. The impact's damage to the entity goes through the pipeline as a hit from the knocker, so friendly fire applies. Walls of the room's outer boundary (not structures) still hurt the entity.
- **Thrown props:** fly at 20 m/s (a feel value). They pass through their thrower and through entities the thrower can't hurt, like projectiles. "Takes the same damage" means the same hit, through the prop's own Armor and Damage Reduction.
- **Kick:** a prop throw uses Kick's cooldown. One prop per Kick, the earliest grabbed first.
- **Doors:** only entities open them, not props. A door stays open while anyone is in its trigger box, so it never closes on someone. An open door's panel has no collider, so modules pass through it.
- **Windows:** full wall height, so they block movement and projectiles but not sight.
- **Dragged props** can't be hurt (modules.md: dragged targets can't be hurt).
- **The turret** shoots a dummy, not the player, so it doesn't wear the player down while testing.

### Stubs and deferred
- **5b:** DoT ticks, building fire state and burn ticks, `Ignites` / `Extinguish`, spreading, Burn on props (x10), vehicles set on fire by blasts, burning interiors blocking enemy paths, and Valuables.
- `InteriorZone` lists and `Building.Neighbors` have no reader until 5b's fire.
- `MapSocket` options are filled by missions in Milestone 9; Heavy Turret spawning by rank, too.
- Turret targeting ("never civilians", enemies.md) is Milestone 6's AI. The friendly fire rules still let Neutrals hurt civilians.
- Entity tags come from an Inspector list until `entities.csv` has a tags column.
- **Dev options menu (asked by the user, for later):** expose the wall damage multipliers (`building_impact_multiplier`, `building_throw_multiplier`, `demolition_multiplier`) and the fire wall-break interval (`fire_break_interval_s`, Milestone 5b) in a dev options menu, so they can be tuned in play. No such menu exists yet.

### Editor steps for the user
1. **Let Unity compile.** If the Console says a type is missing although its file exists, it was skipped: tell Claude.
2. **Run Zenthium > Data > Import Design CSVs.** Expect 0 warnings.
3. **Run Zenthium > Setup > Create World Sandbox Scene** and choose Replace. It rebuilds the shared prefabs (now with `KnockbackImpacts`) and creates `Sandbox_World.unity`, its NavMesh asset and `Prefabs/HeavyTurret.prefab`.
4. **Rebuild the other sandboxes** you use (Create Player / Modules Sandbox Scene), since the shared prefabs changed.
5. **Press Play in `Sandbox_World`** and work through the checklist (Caps Lock).

### What to check
- [ ] The Console has no errors after compiling, importing and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`DamagePipeline`, `Hit`, `KnockbackResolver`, `EntityMotor`, `HitEffects`, `SweepHit`, the constants parser).
  - New classes: `StructureHealthTests`, `StructureDamageRulesTests`, `PropRulesTests`, `BuildingNeighborsTests`, `ImpactDamageTests`.
  - Changed classes: `DamagePipelineTests`, `KnockbackResolverTests`, `DesignDataParserTests`.
- [ ] Every item on the in-game checklist (12).

### Open questions and findings
- **Heavy Gun's damage type** wasn't in `modules.csv`; it's set to Physical. Confirm or change it.
- **Neighbor cost** (asked by the user): the neighbor lists are worked out once when the map is assembled, comparing every pair of buildings' zones. Even a Large map with a few hundred buildings is tens of thousands of rectangle checks, well under a millisecond, and nothing is searched at runtime.
- **Design mirror:** `constants.csv` and `modules.csv` differ further from the design branch.

### First run (2026-11-24)
- The user passed checklist items 1-12.
- **Throw multiplier (asked by the user):** 10x was too much for thrown props, so they get their own multiplier, 2x. Knockback impacts stay at 10x.
  - Data: `building_throw_multiplier` (2) in `constants.csv`'s `world` group; `WorldConstants.BuildingThrowMultiplier`.
  - Code: a new `StructureHitKind.Thrown`, used by `PropBody` for what it hits and for itself; `StructureDamageRules` applies the new multiplier to building parts only.
  - Tests: two new `StructureDamageRulesTests` cases (thrown prop on a wall, and on a prop). The fixtures (`DesignDataTestCsv`, `ImpactDamageTests`, `StructureDamageRulesTests`) have the new constant; no assertions changed.
  - Checklist item 7 is updated.

### Editor steps
1. Let Unity compile, then run **Zenthium > Data > Import Design CSVs** (the new constant is required).
2. Press Play in `Sandbox_World` (no rebuild needed) and throw the bench into a Wood wall: 100, and it breaks.
3. Run `StructureDamageRulesTests`, `DesignDataParserTests` and `ImpactDamageTests`.

---

## Milestone 5b: DoT ticks, fire and Valuables (implemented, not yet checked, 2026-11-24)

### Done
- **Data (local edits, approved):**
  - `constants.csv`: `dot_tick_s` (0.5, formula group). In the `world` group, from world.md and statuses.md: `fire_tick_s` 5, `fire_burn_damage` 20, `fire_burn_s` 4, `burn_out_s` 60, `fire_spread_interval_s` 15, `fire_spread_chance` 0.25, `prop_burn_duration_multiplier` 10, `carrying_treasure_slow_per_valuable` 0.05, `carrying_treasure_slow_cap` 0.5, `valuable_icon_growth_per_valuable` 0.05, `valuable_icon_growth_cap` 0.5, `valuable_pickup_s` 5.
  - `modules.csv`: Water Cannon's `damage_type` is Physical.
  - Code: `ModuleConstants.DamageOverTimeTickSeconds`; new `FireConstants` and `ValuableConstants` (`GameDatabase.FireConstants`, `ValuableConstants`), read by the constants parser.
- **DoT ticks:**
  - `DamageOverTime` (plain C#): a DoT's total damage in equal ticks on game time, never past its end.
  - `StatusApplication` carries a DoT's total damage and damage type. `StatusEffects` raises `OnDamageOverTimeTick` (after its update, since the damage can trigger a Defiance cleanse).
  - `DamageOverTimeTicks` (Modules, on every entity body) deals each tick through the pipeline as a DoT hit.
  - `StatusEffects.Remove(type)` (for Extinguish) and `SetLayers` (Carrying Treasure follows the bag count).
- **Fire:**
  - `BuildingFire` (plain C#): unburned, burning, burned out; ticks every 5 s and spread rolls every 15 s from ignition; burned out at 60 s, with nothing at that moment; Extinguish makes it unburned and its clock starts over.
  - `Building` runs it: each tick, Burn on every entity inside and on every prop inside (each once, even across two zones), and one random standing wall, window or door broken (`FireTargets`). Spread rolls ignite unburned neighbors when `BuildingRegistry.IsSpreadOn` (on in the sandbox; Milestone 11 sets it by rank). Burned out, the loose bags in its zones melt.
  - `InteriorZone`: tracks loose bags, and carves its floor out of the NavMesh while its building burns.
  - `StructureHit` carries `Ignites` and `Extinguishes` from the module's tags; `Structure.OnHit` passes them on. Any building part's hit ignites or puts out the building.
  - `PropBurning`: Burns on any prop (your answer 2), 10x as long at the same damage per tick, ignoring Armor; nothing while dragged; Extinguish clears them. A vehicle blast sets the other vehicles in it on fire.
  - `HitEffects`: Extinguish removes Burn from entities a hit lands on.
- **Valuables:**
  - `ValuableBag`: a trigger collider (never blocks anyone), found by Drag as a `Valuable` target and picked up at once (`IPickup`, new in Modules; `DragAction` now includes triggers in its reach check). Launched, it flies like a swept knockback and deals the thrower's Kick damage to the first thing it hits (walls take the throw multiplier), then lands there.
  - `ValuableCarrier` (on every entity body, `ICarriedLoot`): the count, Carrying Treasure layers, dropping everything on defeat (`OnDroppedAll`), and the player's Kick launch.
  - `ValuableService` (scene service): spawns, scatters (1.5 m drop radius, launch spread of ±25% distance and ±15°, feel values) and keeps `LostCount`.
  - `ValuableRules` (the slow layers and icon scale) and `ValuablePickupTimer` (5 s, reset by damage; Milestone 6's AI uses it).
  - `KickModule`: a dragged prop first; otherwise the player's bags; otherwise the strike.
- **Test modules:** Fire Wand (projectile, Ignites) and Water Cannon (beam, Extinguish) are in the catalog.
- **Sandbox:** `ValuableCarrierView` ("$N" icon and "KA-CHING!"), `BuildingFireView` (orange, black), `BuildingDebugLabel`, `LostValuablesLabel`; `StructureDebugLabel` shows "Burning" on props.
- **Editor:**
  - `TryBuildRoom` creates a Valuable Service and `Prefabs/ValuableBag.prefab` in every sandbox; every entity body gets `DamageOverTimeTicks` and `ValuableCarrier`; placed entities get their carrier's service and view.
  - `Sandbox_World`: spreading on; zones carve while burning; props can burn; three bags in the Wood house and two by the player; a dummy carrying three; building labels and fire colors; the lost counter at the top.
- **Checklist:** `MilestoneChecklist.txt` holds 5b's tests.
- **Tests (EditMode):**
  - New classes: `DamageOverTimeTests`, `BuildingFireTests`, `FireTargetsTests`, `ValuableRulesTests` (with the pickup timer).
  - New cases (no assertions changed): `StatusEffectsTests` (Burn ticks, Time Stop, `Remove`, `SetLayers` and its cap), `DesignDataParserTests` (the new constants).
  - Fixtures: `DesignDataTestCsv` and `CombatTestSetup` have the new constants.

### Decisions (approved by the user)
- DoT ticks every 0.5 s; the world.md numbers in `constants.csv`; Water Cannon is Physical.
- Every prop can burn (not only vehicles), 10x as long at the same damage per tick.
- Launched bags fly Kick's knockback tier with a small random spread, deal Kick damage to the first thing hit (walls x2, like thrown props) and land there.
- Extinguish removes Burn only on hits that land (so not on allies for now).
- Dropped bags scatter within 1.5 m; spread rolls every 15 s from ignition; only loose bags melt.

### Choices made in code (not asked)
- A DoT whose duration isn't a whole number of ticks loses the last part-tick.
- Building fire Burns have no source (environmental), so they hurt everyone inside, the player included.
- The building's Burn reaches each entity once per tick even if it stands where two zones overlap.
- An `Ignites` hit on an entity does nothing by itself; Burn on entities comes from burning buildings now, and from modules with `burn_damage` when those are built.
- Only the player's Kick launches bags (world.md: "The player can press Kick").
- Launching bags uses Kick's cooldown.
- Bags in flight inside a building at burn-out melt too.

### Stubs and deferred
- `ValuablePickupTimer` has no caller until Milestone 6's AI; goons looting, handing bags to a Leader and loot destinations come then.
- Civilians carrying and dropping Valuables, and Valuable placement in sockets: Milestones 6 and 9. The lost count's mission goal: Milestone 9.
- Modules with `burn_damage` (Fire Ring, Spectrum Bolt, Fire Spell Tome, Hoop of Fire): later milestones.
- Gel Coat and Fireproof Body: later.

### Editor steps for the user
1. **Let Unity compile.** If the Console says a type is missing although its file exists, tell Claude.
2. **Run Zenthium > Data > Import Design CSVs.** The new constants are required.
3. **Run Zenthium > Setup > Create World Sandbox Scene** and choose Replace. It also creates `Prefabs/ValuableBag.prefab`.
4. Rebuild the Player and Modules sandboxes if you use them (the shared prefabs changed).
5. **Press Play in `Sandbox_World`** and work through the checklist (Caps Lock).

### What to check
- [ ] The Console has no errors after compiling, importing and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`StatusEffects`, `StatusApplication`, `HitEffects`, `DragAction`, the constants parser).
  - New classes: `DamageOverTimeTests`, `BuildingFireTests`, `FireTargetsTests`, `ValuableRulesTests`.
  - Changed classes: `StatusEffectsTests`, `DesignDataParserTests`.
- [ ] Every item on the in-game checklist (13).

### First run (2026-11-24)
- The user passed checklist items 1-13.
- **Item 2:** Burn (4 s) ends before the next fire tick (5 s), so it stops and starts. world.md's numbers; left as they are, as a question below.
- **Item 3:** nothing turned black at burn-out, because the 5 s breaks had destroyed every wall, window and door by then.

### Changes after the first run (asked by the user, 2026-11-24)
- **Walls break every 10 s** (a local design decision; world.md says one per fire tick): a new `fire_break_interval_s` (10) in the `world` group, `FireConstants.BreakIntervalSeconds`, and `FireStep.PartBreaks`. Burn still comes every `fire_tick_s` (5). Over 60 s that's 5 breaks, so most of a building is left to turn black.
- **Dev options menu (for later):** add `fire_break_interval_s` to the dev options list, with the wall damage multipliers.
- **Reset scene button** in every sandbox (bottom right, `SandboxSceneReset`, added by `SandboxHudSetup`): reloads the scene in Play mode. The HUD canvas now has a raycaster so the button takes clicks; the HUD and checklist panels still catch none.
- **Tests:** `BuildingFireTests` gets a new case for breaks every 10 s, and its 60 s case also checks the 5 breaks (renamed to `Advance_To60Seconds_BurnsOutAfter11Ticks5BreaksAnd3Rolls`). `DesignDataParserTests` checks the new constant. The fixtures have it.
- **Checklist:** items 2 and 3 are updated, and the header mentions Reset scene.

### Editor steps
1. Let Unity compile, then run **Zenthium > Data > Import Design CSVs** (the new constant is required).
2. Run **Create World Sandbox Scene** (Replace) for the button; the Player and Modules sandboxes get it when rebuilt.
3. Run `BuildingFireTests` and `DesignDataParserTests`.

### Open question
- **Burn gap (decided by the user):** the building's Burn now lasts 5 s (`fire_burn_s` 5, was world.md's 4), as long as the tick interval, so it never lapses: 20 damage over 5 s, 2 per 0.5 s tick. On props that's 50 s. world.md still says 4 s; update it or push the change to the design branch when you like. Checklist items 2, 5 and 6 have the new numbers. No code or test changes; re-import the CSVs.

### Test fix (approved by the user, 2026-11-24)
- The EditMode run had 1 failure in 339: `StatusEffectsTests.SetLayers_ThreeValuables_Slow15Percent` expected 4.2 m/s, but three Valuables (15% slow on 6 m/s) give 5.1, which the code returned. The test's literal was miscalculated when it was written; with the user's approval its expected value is now 5.1. The game code is unchanged.

---

## Milestone 6a: Enemy AI, noticing and fighting (checked by the user, 2026-11-25)

Milestone 6 is split, as approved. **6a** (this entry) builds detection, awareness and alerts, the Idle, Combat and Searching states, module choice, targeting, NavMesh movement, telegraphs, the friendly fire toggle, the Taunted, Hypnotized and Smoke Bomb reads, the Heavy Turret's AI, allies following and `Sandbox_AI`. **6b** builds Looting, Objective (carrying loot, fleeing the map), cowardice, citizens and `ICharmable`.

### Done
- **Data (local edits, approved):**
  - `constants.csv`: a new `ai` group from enemies.md: `alert_delay_s` 0.5, `alert_radius_m` 10, `search_s` 10, `awareness_drain_s` 3, `ranged_backoff_m` 3, `ally_follow_m` 10, `telegraph_s` 0.5, `patrol_speed_fraction` 0.5. The 6b numbers (cowardice, fear range, Stubborn) aren't added yet.
  - `modules.csv`: Cannons gets `damage_type` Physical (an assumption, like Heavy Gun: confirm it), so it can be built as a projectile.
  - Code: `AiConstants` (`GameDatabase.AiConstants`), read by the constants parser.
- **New `Zenthium.AI` assembly (`Scripts/AI/`, approved):**
  - Plain C#:
    - `Awareness`: the bar fills at Detectability divided by `AwarenessFillSeconds(distance)` per second (the existing formula: instant at 10 m, 10 s at 100 m). It drains over 3 s with no one in view. Then come the 0.5 s wait and the alert, forgetting over 3 s, and a reset when defeated (so no alert goes out).
    - `AlertRules`: who an alert wakes.
    - `AiSides`: who each team fights and notices, preferences, and Hypnotized.
    - `TargetSelector`: who to attack.
    - `ModuleChooser`: which module to use, and its reach.
  - `AiBrain`:
    - Looks around every 0.2 s of game time: sight to 100 m, through `ModuleWorld.CanSee`.
    - Runs the Idle, Combat and Searching states, and alerts.
    - Uses the first ready module in reach (actives, then gear).
    - Telegraph modules wind up for 0.5 s first, with the aim locked.
    - Melee attackers chase; ranged attackers stop at range and back off within 3 m. A locked door in the way is attacked.
    - Turns on whoever hit it, follows Taunt, and searches the last known position, then forgets.
    - Allies follow the player.
    - Raises `OnNoticed` and `OnTelegraphStarted`. Implements `IAwarenessReset` for smoke.
  - `AiNavigator`: NavMesh paths (no NavMeshAgent) steering the `EntityMotor`. Reports a locked door it's walking into. Starting on a carved-out floor (a burning building), it walks out first.
  - `AiSquad`: patrols between its `MapSocket`s, members in a ring, waiting 3 s once everyone on patrol has arrived.
- **Shared code:**
  - `EntityTag.SparesCivilians` (the Heavy Turret).
  - `StatusEffects.TryGetSource` (who Taunted or Hypnotized it).
  - `ModuleWorld.CanSee` (line of sight, plus smoke) and `CollectAll`.
  - `SmokeCloud` blocks sight through its circle and, when it spreads, empties the awareness bars of opponents inside it (`IAwarenessReset`, new in Modules). Taunt's sight check uses `CanSee`, so smoke blocks Taunt too.
  - Cannons is in the catalog as a projectile.
- **Summons and the turret:**
  - `PlaceholderSummonBrain` is deleted (with its `.meta`). The summon prefab has `AiNavigator` and `AiBrain`: it follows the player and fights.
  - `HeavyTurret.prefab` gets the AI and `SparesCivilians`. In `Sandbox_World`, the placed turret's AI is switched off and it keeps firing at its dummy (`SandboxAutoCaster`, kept as a test tool).
- **Sandbox:** `AwarenessView` (bar, "!", grey alert circle, made see-through), `TelegraphFlashView` (red tint), `AiStateLabel`, and `EnemyFriendlyFireToggle` (the dev toggle). The Sandbox asmdef references AI.
- **Editor:**
  - New **Zenthium > Setup > Create AI Sandbox Scene** (`AiSandboxSceneCreator`), on a 60 m room. It holds:
    - the World sandbox's two buildings;
    - cover north and south of the player;
    - Squad A (3 Speed Punch goons) and Squad B (2 Energy Blaster goons and a Cannons goon), each with 3 patrol sockets;
    - a Heavy Turret with the AI, and a civilian dummy beside it;
    - the hero select and MoreInfo panels;
    - the Enemy FF button;
    - its own NavMesh asset (`Scenes/Sandbox_AI_NavMesh.asset`).
  - Shared helpers made internal or extracted:
    - `WorldSandboxSceneCreator`: `BuildTestBuildings`, `PlaceTurret` (now takes a position and an optional auto-cast target) and `BakeNavMesh(geometry, path)`;
    - `PlayerSandboxSceneCreator.CreateBlock`;
    - `SandboxHudSetup.CreateButton`.
  - The Editor and test asmdefs reference AI.
- **Checklist:** `MilestoneChecklist.txt` holds 6a's tests (14).
- **Tests (EditMode):**
  - New classes: `AwarenessTests`, `AlertRulesTests` (including a chain test), `ModuleChooserTests` and `TargetSelectorTests` (with the AiSides rules and Hypnotized).
  - New case (no assertions changed): `DesignDataParserTests.Parse_AiConstants_AreRead`. The fixture `DesignDataTestCsv` has the `ai` rows.

### Decisions (approved by the user)
- The 6a/6b split, the new assembly and the `ai` constants.
- **Awareness bar:** one per entity, filled by whoever it sees that fills it fastest. It drains over 3 s while it sees no one.
- **Who gets noticed:** enemies notice only the player and allies. Other teams notice whoever they fight. The turret's `SparesCivilians` tag keeps it off civilians.
- **Target order:**
  1. the taunter;
  2. the last attacker, if on another side and still in view;
  3. the nearest player or ally;
  4. civilians;
  5. neutrals.
- **Telegraph:** a 0.5 s wind-up with the aim locked at the start. Cannons is the test module.
- **Searching:** wander about 6 m around the last known position. Seeing a target again goes straight back to Combat.
- **Hypnotized:** changes only whom it targets, not the damage rules.
- **For 6b:**
  - Leaderless goons carrying Valuables try to leave the map (world.md line 107 wins over line 101).
  - Scared citizens drop their Valuables once, when they start fleeing, and flee straight away from the nearest enemy within 20 m.
  - Coward stays an Inspector tag until Milestone 7.

### Choices made in code (not asked)
- **Detectability:**
  - It scales the fill speed: double Detectability fills twice as fast.
  - Within 10 m it's instant at any Detectability above 0.
- **Smoke:** it empties the bars of unaware entities only. Aware ones inside lose sight through it and search.
- **Taunt:** a Taunted entity that was unaware notices at once, and goes for the taunter even out of sight.
- **When it notices:**
  - Being hit makes it notice only if the attacker is on another side, so friendly fire between enemies never alerts or turns them.
  - An alerted entity searches where its alerter last saw the target.
- **Searching:** the 10 s counts from the start of the search, so an unreachable last known position can't stall it.
- **Module order:** actives in slot order, then gear actives, since slots don't keep a cross-type list order. A module that still refuses (a reason of its own) is skipped for the next.
- **Reach:**
  - A module's reach is its range tier. With no range, it's its radius tier; with neither (self-buffs), it reaches anywhere.
  - Attack range (for movement) is the longest reach among its Melee and Ranged modules, and it fights at range if any has `Ranged`.
- **Feel values (Inspector):** think 0.2 s, stop at 85% of attack range, back-off step 2 m, ally follow slack 3 m, squad wait 3 s, NavMesh sample radius 20 m, repath 0.5 s.
- **Idle:** allies follow the player only when Idle; an Immobile entity never moves.
- **Defeated:** it forgets everything and is unaware if revived.
- **Turret:** the shared `HeavyTurret.prefab` has the AI; the World sandbox turns it off on its instance.

### Stubs and deferred
- **6b:**
  - Looting (`ValuablePickupTimer`), carrying loot to a Leader, a `LootDestination` or the map edge, fleeing and escaping.
  - Cowardice; citizens (Scared, Incapacitated, Stubborn) and their drops; grabbing standing citizens.
  - First Aid Spray getting citizens up Scared; Charming on Stubborn citizens; `ICharmable`.
  - The 6b constants.
- **Stubborn citizens (the user's answer, 2026-11-24):** "While you're dragging them, they slow you down by 20%". missions.md says 25%. Confirm in 6b which applies, and whether "much slower to grab" then needs no grab time.
- **Invasion** (enemies attack civilians outside Combat), reinforcement waves and Leaders' special behaviors: Milestones 7 and 9.
- **Allies:** they don't notice or fight neutrals differently from enemies; Hypnotic Gaze and Card Throw, which apply Hypnotized, come later.
- **Dev options menu:** add the enemy friendly fire toggle to it when it exists.

### Editor steps for the user
1. **Let Unity compile.** If the Console says a type is missing although its file exists, tell Claude.
2. **Run Zenthium > Data > Import Design CSVs.** The `ai` constants are required, and Cannons changed.
3. **Run Zenthium > Setup > Create AI Sandbox Scene** and choose Replace. It rebuilds the shared prefabs (the summon now has the AI, and so does `HeavyTurret.prefab`), then creates `Sandbox_AI.unity` and its NavMesh asset.
4. **Rebuild the other sandboxes** you use (Create Player, Modules and World Sandbox Scene), since the shared prefabs changed.
5. **Press Play in `Sandbox_AI`** and work through the checklist (Caps Lock).

### What to check
- [ ] The Console has no errors after compiling, importing and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`StatusEffects`, `ModuleWorld`, `SmokeCloud`, `TauntModule`, the constants parser).
  - New classes: `AwarenessTests`, `AlertRulesTests`, `ModuleChooserTests`, `TargetSelectorTests`.
  - Changed class: `DesignDataParserTests`.
- [ ] Every item on the in-game checklist (14).

### Open questions and findings
- **Cannons' damage type** wasn't in `modules.csv`; it's set to Physical. Confirm or change it.
- **Design mirror:** `constants.csv` and `modules.csv` differ further from the design branch.

### First run (2026-11-25)
- The user passed checklist items 1-3, 5-8, 10, 11 and 14. Item 13 is not tested yet.
- **Item 4:** the user couldn't sneak up on a goon, even with Time Slow. Enemies see in all directions (stats.md), and the 10 m instant range made sneaking impossible. The new awareness numbers below fix that.
- **Item 9:** couldn't be tested. Goons that can't see you never pathed to the door; they searched, then went back to Idle.
- **Item 12:** too hard to test with the old fill rates.

### Changes after the first run (asked by the user, 2026-11-25)
- **Awareness numbers** (a local design decision; stats.md says instant at 10 m and 10 s at 100 m):
  - `constants.csv`: `awareness_instant_range` 1 and `awareness_max_time` 4.67 (about 3 s at 10 m).
  - A new `awareness_range` of 15 in the `formula` group: an unaware entity notices no one past 15 m.
  - `vision_range` stays 100 m: how far an aware entity sees, and how far Taunt reaches.
  - Code: `FormulaConstants.AwarenessRange`. `BalanceFormulas.AwarenessFillSeconds` runs from the instant range to the awareness range, not to the vision range.
- **Locked-door test:**
  - `BuildTestBuildings` takes the shop door's material: Metal in `Sandbox_World`, Wood (100 HP) in `Sandbox_AI`.
  - `Sandbox_AI` places a "Door Test Goon" in the Stone shop's back room, by the west window. It sees you through the glass, and its only way out is the locked door.
- **Searching attacks locked doors:** a goon that loses sight on its way still breaks a locked door between it and the last known position. Before, only Combat did.
- **Tests:**
  - `BalanceFormulasTests` gets a new parameterized case for the new numbers: instant at 1 m, about 3 s at 10 m, nothing past 15 m.
  - `DesignDataParserTests.Parse_AiConstants_AreRead` also checks `awareness_range`.
  - The fixtures (`BalanceFormulasTests`, `CombatTestSetup`, `DesignDataTestCsv`) have the new constant. The existing fixtures keep awareness range 100, so no existing assertions changed.
- **Checklist:** items 2, 4 and 9 are updated, and the header mentions the door test goon.

### Editor steps
1. Let Unity compile, then run **Zenthium > Data > Import Design CSVs** (the new constant is required).
2. Run **Create AI Sandbox Scene** (Replace), and **Create World Sandbox Scene** if you use it (its builder changed; its door is still Metal).
3. Run `BalanceFormulasTests`, `AwarenessTests` and `DesignDataParserTests`, then retest checklist items 2, 4, 9, 12 and 13.

### Second run (2026-11-25)
- The user passed the retested items 2, 4, 9, 12 and 13. All 14 checklist items now pass.
- Still open: Cannons' damage type (Physical, an assumption), and the Stubborn drag slow for 6b (the user chose 20%; missions.md says 25%).

## Milestone 6b: Enemy AI, looting, cowardice and citizens (checked by the user, 2026-11-26)

The second half of Milestone 6, as split in 6a: Looting, Objective (carrying loot, fleeing the map), cowardice, citizens, `ICharmable`, and the sandbox for them.

### Done
- **Data (local edits, approved):** four new rows in the `ai` group of `constants.csv`, read into `AiConstants`:
  - `coward_hp_fraction` 0.2
  - `coward_chance` 0.25
  - `citizen_fear_range_m` 20
  - `stubborn_drag_slow` 0.2 (the user's 20%; missions.md says 25%)
- **AI (`Scripts/AI/`):**
  - `AiState` gets `Looting` and `Objective`.
  - `AiBrain` decides in this order:
    1. A panicked coward runs for the map edge and escapes there.
    2. An enemy with no player or ally it can see inside its attack range loots, or delivers what it carries.
    3. Otherwise it behaves as in 6a: Combat, Searching, Idle.
    - Taunt still overrides looting.
    - New Inspector fields: `mapBounds`, pickup reach 1 m, hand-over reach 2 m.
    - It reacts to a team change (Charming): it forgets the fight and leaves its squad.
  - `AiLooter` (component): finds the nearest loose Valuable it can see and runs the existing `ValuablePickupTimer`. Damage that lands restarts the pickup.
  - Plain C# classes:
    - `LootRules`: who loots, when, and where the loot goes.
    - `Cowardice`: the single roll.
    - `CitizenFear`: when a citizen flees, and the drop that happens only once.
  - `CharmConversion` (component, `ICharmable`): an enemy goon joins the charmer's side as an Ally and hands over its Valuables.
  - `CitizenBrain` (component, `ICharmable`) and `CitizenKind`:
    - Scared: flees from enemies it can see within 20 m.
    - Incapacitated: starts down, with its Valuables dropped beside it. When revived it gets up Scared.
    - Stubborn: never moves, sets the drag slow, and Charming can turn it Scared.
- **Shared code:**
  - `Entity`:
    - `Escape()`, `IsEscaped` and `OnEscaped`.
    - `Incapacitate()`: down with no damage and no Defiance roll.
    - `ChangeTeam()` and `OnTeamChanged`: `Team` is no longer read-only.
  - `EntityTag.LootDestinationMapEdge`, added at the end because tags are stored by index.
  - `ICharmable`:
    - Now `TryGetCharmTarget(out CharmTarget)` and `Charm(Entity)`.
    - `CharmingPassive` rolls `goon_convert` or `stubborn_to_scared`, once per entity, and `ModuleCatalog` requires both keys.
  - Drag:
    - `EntityDragTarget` lets standing citizens be grabbed, and getting up no longer frees a dragged citizen.
    - `EntityDragTarget` has `SetCarrierSlow`.
    - `DragAction` slows the carrier by the sum of what it drags (`MoveSpeed.AddSlow`), checked every physics step.
    - `IDragTarget.GrabSeconds` is removed, since every grab stays instant (the user's decision).
  - `ValuableService.Bags`: the loose bags register while enabled.
  - `ValuableCarrier`:
    - `GiveAllTo()`.
    - `DropAll()` is now public.
    - Escaping counts what it carries as lost.
  - World: `MapBounds` (component) and `MapEdge` (plain geometry).
  - First Aid Spray needed no change: `CitizenBrain` listens for the revive.
- **Sandbox:**
  - `AiStateLabel` shows the pickup percentage and "(panic)".
  - New `CitizenStateLabel`.
  - `AiSandboxSceneCreator` adds:
    - `MapBounds` (60 m);
    - looting and Charming on every goon;
    - the Coward tag on all goons except the Leader and the locked-in goon;
    - a Leader (GleebPeep) in Squad A;
    - `LootDestinationMapEdge` on Ranged Goon 2;
    - two stashes (3 bags by Squad B, 4 east of the start) and the lost counter;
    - three citizens: Scared with 2 Valuables, downed with 1, and Stubborn by the turret, which replaces 6a's civilian.
  - `WorldSandboxSceneCreator.CreateLostCounter` is now internal.
- **Checklist:** `MilestoneChecklist.txt` holds 6b's tests (9).
- **Tests (EditMode):**
  - New classes: `CowardiceTests`, `LootRulesTests`, `CitizenFearTests`, `MapEdgeTests` and `EntityTests`.
  - `DesignDataParserTests.Parse_AiConstants_AreRead` gets four more asserts.
  - The fixtures `DesignDataTestCsv` and `AwarenessTests.CreateConstants` have the new constants.
  - No existing assertions changed.

### Decisions (approved by the user, 2026-11-25)
- The Stubborn drag slow is 20%, and Stubborn citizens are grabbed instantly.
- Unaware, patrolling goons loot too.
- "Outside their attack range" counts the player and the player's allies.
- The Scared fear range needs line of sight.
- Carried over from 6a: leaderless goons carry loot off the map; Scared citizens drop their Valuables once, when they start fleeing.

### Choices made in code (not asked)
- **Spotting Valuables:** unaware, a goon spots them only within the 15 m awareness range; aware, out to vision range. Otherwise idle goons would empty a bank across the map.
- **Who loots:** every Enemy-team entity that can move, Leaders included. Leaders keep what they hold; their fleeing comes with the Valuables goal.
- **Hand-over:** a goon carrying Valuables that sees no more takes them to the nearest standing Leader on its team (which must have a Valuable carrier). It goes off the map if it has the map-edge tag or no Leader is standing.
- **Leaving the map:** the goon heads for the nearest edge of `MapBounds` and escapes within its 2.5 m margin. The entity escapes and its GameObject is switched off.
- **A panicked coward** ignores everything, Taunt included, until it escapes. Without `MapBounds` it doesn't panic-run.
- **Cowardice** doesn't roll on a hit that takes it straight to 0 HP. A revived coward that panicked keeps running.
- **Converted goons** become `Team.Ally` (the charmer's team if that isn't the player). A Leader or Champion can't be converted.
- **Citizens:**
  - Only `Team.Enemy` scares them, not neutrals.
  - A fleeing citizen aims 6 m directly away from the nearest enemy each 0.2 s.
  - Any revive gets a citizen up Scared.
- **Drag slow:** two Stubborn citizens slow by 40% (the sum, capped at 100%).
- **Charmed marker:** a Stubborn citizen that rolls keeps the hidden Charmed marker, so it rolls only once.

### Stubs and deferred
- **Milestone 7:**
  - `LootDestination:CropCircle` and Repo Agency Tow Beam/Drag.
  - Enemies with Drag taking citizens to their destination, even while Idle.
  - Coward on goons by default (entities.csv).
- **Milestones 8 and 9:**
  - The map builder sizes `MapBounds` and wires it to spawned goons.
  - The 10% chance for a civilian to carry a Valuable, and citizens' kinds from the mission spawner.
- **Milestone 9:**
  - The Leader fleeing once it holds enough to fail the Valuables goal.
  - Escaped enemies counting toward "defeat every enemy".
- **Not built:** Infected citizens turning into zombies.

### Editor steps for the user
1. **Let Unity compile.**
2. **Run Zenthium > Data > Import Design CSVs.** The four `ai` constants are required.
3. **Run Zenthium > Setup > Create AI Sandbox Scene** and choose Replace.
4. **Press Play in `Sandbox_AI`** and work through the checklist (Caps Lock). The default hero, Rush Hour, has Charming and First Aid Spray.

### What to check
- [ ] The Console has no errors after compiling, importing and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`Entity`, `ICharmable`, `CharmingPassive`, `ModuleCatalog`, the drag code, the Valuable code, the constants parser).
  - New classes: `CowardiceTests`, `LootRulesTests`, `CitizenFearTests`, `MapEdgeTests`, `EntityTests`.
  - Changed class: `DesignDataParserTests`.
- [ ] Every item on the in-game checklist (9).

### Open questions and findings
- **Design mirror:** `constants.csv` now differs further from the design branch (the 6b `ai` rows).
- Still open from 6a: Cannons' damage type (Physical, an assumption).

### First run (2026-11-26)
- Unity was still in Play mode when the scripts recompiled. Thousands of NullReferenceExceptions followed in `PlayerController` and `SandboxChecklistView`, which had lost their input actions in the reload. It wasn't a 6b bug. After leaving Play mode, the user rebuilt `Sandbox_AI`.
- The user passed checklist items 1-8.
- **Item 9:** mostly passed. A converted goon handing over its Valuables is hard to set up at Charming's 5% rate, so it's not tested.
- The EditMode suite hasn't been run since 6b; the last saved results are from 6a.

### Camera zoom (asked by the user, 2026-11-26)
- **Input:** a new `View` action map in `InputSystem_Actions.inputactions`, with one action, `Zoom`, bound to Alt + mouse wheel (a OneModifier composite). It has no gamepad binding.
- **`OverheadCameraRig`:** `Zoom(steps)`. Each step changes the view by 15%, between an orthographic size of 6 (closest) and 32 (farthest), starting at 12. It eases there over 0.08 real seconds. All of these are Inspector values.
- **`CameraZoomInput`** (new, Player): reads the action and zooms one step per frame the wheel turns. The Player, Combat and Core scene builders add it beside the rig.
- **Not in the design docs:** controls.md doesn't list zooming.
- **Checked by the user (2026-11-26):** zoom works.
- **Editor steps:**
  1. Let Unity reimport the input actions, which regenerates `InputSystem_Actions.cs`, and compile.
  2. Rebuild the sandboxes you use.
  3. Scenes that already exist and aren't rebuilt (Boot, HQ and Mission) need `CameraZoomInput` added to their camera by hand.

### Test run (2026-11-26)
- 425 of 426 EditMode tests passed. `CowardiceTests.Check_AtOrAboveThreshold_DoesNotRoll(20)` failed: a coward at exactly 20% HP rolled.
- **Cause, in game code:** `Cowardice` compared `currentHp >= maxHp * hpFraction`, and `100 * 0.2f` rounds to slightly above 20.
- **First fix:** compare the HP fraction instead (`currentHp / maxHp >= hpFraction`). It still failed: Mono can keep float math at higher precision, where 20 / 100 is exactly 0.2 but the float 0.2f is slightly above it.
- **Second fix:** an explicit cast, `(float)(currentHp / maxHp) >= hpFraction`, which C# rounds to float precision. The test is unchanged.
- **Rerun:** all 426 EditMode tests pass.

## Milestone 7a: Galactic Repo Agency, faction data, crop circles and the first four entities (checked by the user, 2026-11-26)

The first half of Milestone 7, split as approved: faction data, crop circles, the loot destination, dragging citizens, and GleebPeep, Grey, Tractor Drone and Shield Bearer with their modules. 7b does Auditor, Notary, Tow Saucer (Audit, Notarized, Tow Beam, the crash), Mothership Strike and `Docs/HowTo_AddFaction.md`.

### Done
- **Data (local edits, approved):**
  - `entities.csv` gets three columns before `notes`: `modules` (AI priority order), `locked_until` (`Module:Rank; ...`) and `tags`. Only the Repo Agency's rows are filled, from `Factions/GalacticRepoAgency.md`.
  - New `factions.csv`: `faction, tags, arrival, mission_weights, location_weights, values, notes`. One row: the Repo Agency, with `LootDestination:CropCircle`, arrival `Beam`, the Mission and Location Weights, and `crop_circles_base=1; crop_circles_per_rank=1; crop_circles_max=5; crop_circle_radius_m=3`. A faction without a row has no tags, arrival or weights.
  - `modules.csv`:
    - Tractor Pull gets knockback tier Heavy (8 m) and Tractor Beam Normal (4 m), used as the pull distance (the user's choice).
    - Tablet Slap gets `damage_type` Physical, an assumption like Cannons in 6a: **confirm it**.
- **Importer:** reads the new columns and file into `EntityData` (modules, `ModuleLock`s, tags) and `FactionData` (tags, `ArrivalTag`, weights, values). It fails on:
  - unknown modules or tags;
  - a module listed twice;
  - a lock on a module that isn't in the list;
  - a faction row with no entities;
  - a weight that isn't a number of 0 or more.
- **Shared code:**
  - `EntityTag.cs` moved (with its `.meta`) from `Entities/` to `Data/`, namespace `Zenthium.Data`, so `EntityData` can hold tags. The values keep their order.
  - New tag at the end: `LootDestinationCropCircle`.
  - New `ArrivalTag` and `RankRules.HasReached` (mission level against a `Rank`).
  - `EntitySetup.ForFactionEntity`:
    - adds the data modules in order, minus the ones locked at the mission rank;
    - adds the entity's tags and its faction's;
    - sets `MinimumSlotsPerType`, so a list longer than 4 of one type fits (GleebPeep has 5 actives).
  - `EntityBehaviour`:
    - a non-empty Inspector module list replaces the data list (sandbox test kits, summons);
    - Inspector tags add to the data tags;
    - `FactionEntity`, `DifficultyLevel` and `ConfigureFactionEntity`.
  - `Entity.Abduct()`, `IsAbducted` and `OnAbducted`: taken for good, Incapacitated or not.
  - `ValuableCarrier.LoseAll()` is public and runs on abduction too.
  - `DragAction`: `Reach` and `IsDraggingCitizen`.
  - `HitEffects`: optional status modifiers (Marked), and a maximum knockback distance, so a pull stops at its point.
  - `ModuleWorld`:
    - loot destinations register with it (`ILootDestination`, `TryFindLootDestination`);
    - `SpawnFactionEntity` through a scene `EntitySpawner`.
- **World:**
  - `CropCircle` (component, `ILootDestination`): each physics step, every entity whose center is inside loses its Valuables (counted as lost), and a citizen is abducted and switched off. Heroes and enemies aren't abducted.
  - `CropCircleRules`: the count per rank and the radius, from the faction's values.
- **AI:**
  - `LootRules`:
    - `Delivery` gets a `CropCircle` result, which takes the Leader's loot too. A crop circle tag with no crop circle on the map falls back to the 6b rules.
    - `CitizenDelivery`: only an entity with a loot destination drags citizens.
    - `PrefersCitizen`: the nearer of a Valuable and a citizen wins.
  - `AiCitizenSnatcher` (component): an entity whose data lists Drag finds the nearest grabbable citizen it can see and drags it.
  - `AiBrain`, while free to loot (no opponent in attack range, like looting):
    1. delivers a dragged citizen;
    2. otherwise goes for the nearer of a Valuable or a citizen;
    3. then delivers its Valuables, to a crop circle, its Leader or off the map.
    - Leaving the map at the edge takes the citizens it drags along.
    - `Configure(MapBounds)` is for spawned entities.
  - `FactionEntitySpawner` (an `EntitySpawner`): builds a spawned entity from one AI body prefab, inactive, and gives it the clock, Module World, map edge and Valuable service.
  - `ArrivalStub`: arrival tags do nothing until Milestone 9.
- **Modules (in `ModuleCatalog`):**
  - Tablet Slap: the melee strike, with its knockback.
  - Final Notice: a beam that Marks the first entity it hits with its CSV values (Taunt's keys), resisted by Will.
  - Repossess: a beam that applies Slot Disable (a gear slot) for the Medium duration, resisted by Intelligence.
  - Tractor Pull: `PullModule`, which pulls the first entity hit toward the nearest crop circle, or toward the drone if there's none.
  - Tractor Beam: `PullModule`, which pulls every opponent in its path toward GleebPeep, stopping 1.5 m short of it. Walls stop the beam.
  - Call for Backup: `CallForBackupModule` and `BackupRoster`. `goons` and `champions` are drawn at random from the faction's entities that spawn at the mission's rank, and appear in a ring round the user. They stay until defeated.
- **Sandbox:** `Zenthium > Setup > Create Repo Agency Sandbox Scene` builds `Sandbox_Repo` on the shared room:
  - the Repo squad (GleebPeep, 2 Greys, Tractor Drone, Shield Bearer) from data, at rank B;
  - 2 crop circles;
  - downed, Stubborn and Scared citizens;
  - a stash of 3 Valuables and the lost counter;
  - `MapBounds`;
  - the spawner and its `AiEntity.prefab`;
  - the hero select and MoreInfo panels, and the NavMesh.
  - `AiSandboxSceneCreator`'s goon, citizen, squad, stash and visual helpers are internal now. `PlaceGoon`'s AI part is `AddGoonAi`.
- **Checklist:** `MilestoneChecklist.txt` holds 7a's tests (9).
- **Tests (EditMode):**
  - New classes: `FactionDataParserTests`, `EntitySetupTests`, `CropCircleRulesTests` and `BackupRosterTests`, with the helper `TestFactions`.
  - New tests in `LootRulesTests` (crop circle delivery, `CitizenDelivery`, `PrefersCitizen`) and `EntityTests` (abduction).
  - Fixture: `DesignDataTestCsv` has the new entity columns, an empty `factions.csv`, and optional extra entity and faction rows.
  - No existing assertions changed.

### Decisions (approved by the user, 2026-11-26)
- The data goes in the new entities.csv columns and `factions.csv`. The crop circle values go in `factions.csv`, with a 3 m radius.
- The pull distances are the knockback tiers above.
- Tractor Beam pulls only opponents.
- Snatching citizens works like looting: only while no opponent is in attack range, and the nearer of a citizen or a Valuable wins.
- Call for Backup brings only entities that spawn at the mission's rank, and doesn't count toward enemy totals.
- Tractor Pull with no crop circle pulls toward the drone.

### Choices made in code (not asked)
- **Slots:** a faction entity gets as many slots of a type as its list needs. The design doesn't cap enemies at 4, and GleebPeep lists 5 actives.
- **Who drags citizens:** only an entity whose data lists Drag, with `AiCitizenSnatcher` and a `DragAction`. Base actions such as Drag are still outside the slots.
- **Inside a crop circle** means the entity's center is within the radius.
- **Backup** appears instantly (the arrival stub) and gets no squad. It fights where it lands.
- **Tractor Beam** doesn't pull props yet. That's left for 7b, with Tow Beam's props.
- **A Leader with Valuables** heads for a crop circle when it has the tag and one exists. Otherwise it keeps them, as in 6b.

### Side effects on older sandboxes
- Training dummies and sandbox citizens are built from Grey, so they now carry Grey's data kit (Energy Blaster) and tags (Coward, `LootDestination:CropCircle`). Without an AI they never use them.
- In `Sandbox_AI`, the locked-in goon is now a Coward too, since Grey's data has the tag. There's no crop circle there, so looting is unchanged.

### Stubs and deferred
- **7b:**
  - Auditor, Notary and Tow Saucer, with Audit, Notarized, Tow Beam and the crash.
  - Mothership Strike, built but switched off.
  - `Docs/HowTo_AddFaction.md`.
  - Tractor Beam pulling props.
  - Backup at rank B can draw a Tow Saucer, which has no behavior until then.
- **Milestone 8:** the map builder places crop circles at random, `CropCircleRules.Count` of them.
- **Milestone 9:**
  - Arrival tags (`ArrivalStub`).
  - The picker using Mission and Location Weights.
  - Abducted citizens counting against Rescue.

### Editor steps for the user
1. **Let Unity compile.**
2. **Run Zenthium > Data > Import Design CSVs.** `factions.csv` is new, and entities.csv and modules.csv changed.
3. **Run Zenthium > Setup > Create Repo Agency Sandbox Scene** and choose Replace.
4. **Press Play in `Sandbox_Repo`** and work through the checklist (Caps Lock).

### What to check
- [ ] The Console has no errors after compiling, importing and building the scene.
- [ ] Run the whole EditMode suite, since shared code changed (`EntityTag`'s move, `EntitySetup`, `Entity`, `EntityBehaviour`, `HitEffects`, `LootRules`, the importer).
  - New classes: `FactionDataParserTests`, `EntitySetupTests`, `CropCircleRulesTests`, `BackupRosterTests`.
  - Changed classes: `LootRulesTests`, `EntityTests`, and the `DesignDataTestCsv` fixture.
- [ ] Every item on the in-game checklist (9).

### Open questions and findings
- **Tablet Slap's damage type:** Physical is an assumption.
- **Design mirror:** `entities.csv`, `modules.csv` and the new `factions.csv` now differ from the design branch.

### Test run (2026-11-26)
- All 473 EditMode tests pass. The user passed the in-game checklist.
