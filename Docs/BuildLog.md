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
