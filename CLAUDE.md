# Project

## Overview
- Game: Zenthium SWIFT Heroes (working title). A top-down 2D, mostly comedic superhero game. Solo and mission-based: the player runs the hero organization SWIFT, and missions earn Zenny to spend at HQ.
- Unity 6000.6.10f1 (Unity 6.6), URP 17.6.0 with the 3D Universal Renderer.
- Target: WebGL. Controls: mouse and keyboard, and gamepad; no touch.
- Input System 1.20.1 is the only active input handler. Scripting: Mono, .NET Standard 2.1.
- UI: uGUI with TextMeshPro for menus, HUD, and in-world UI. Don't use UI Toolkit.
- Version control: Git, on GitHub at `Dehaku/Zenthium-Mission-Zero` (public), branch `main`. Binary assets (images, audio, models, fonts) go through Git LFS. Ignore rules are in `.gitignore`; LFS file types and line endings are in `.gitattributes`. The design docs come from a separate repo (`Docs/design/SOURCE.md`).
- Tests: no test setup yet; the Test Framework package isn't installed.

## Directory map
Planned layout; create folders as needed.
- `Assets/_Project/`: all project-owned content
  - `Scripts/<Feature>/`: gameplay code, one folder per system (e.g. `Entities`, `Modules`, `Combat`, `Statuses`, `Time`)
  - `Scripts/Editor/`: editor-only code, such as the data importer
  - `Data/Generated/`: assets generated from the design CSVs
  - `Prefabs/`, `Scenes/`, `Art/`, `Audio/`, `Tests/EditMode/`, `Tests/PlayMode/`
- `Assets/Settings/`: URP assets. `Assets/InputSystem_Actions.inputactions`: input actions.
- `Assets/TutorialInfo/`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene.unity`: template leftovers; ignore.
- Third-party and Asset Store folders: don't modify.
- `Docs/design/` (project root): design docs and data CSVs. `Docs/claude/`: detailed instructions for specific tasks.

## Architecture
Planned; these systems don't exist in code yet. The design docs specify the mechanics; this section says how code is structured around them.
- **One Entity type:** Heroes, enemies, and civilians are the same Entity, differing only in data: stats, slotted Modules, and entity tags. Don't write separate hero and enemy versions of shared logic.
- **Modules:** The code name for active powers, passive powers, and gear (players see "Powers" and "Gear"). All abilities come from slotted, data-driven Modules. Build a new ability from the shared Module parts (category, tags, triggers, effects, stat formula, resources, damage type, cooldown), not as one-off code. Each tier is a separate Module.
- **Tags over pairings:** Links between Modules, statuses, and entities go through tags (`Resist:<Stat>`, `KnockbackResistant`, `Hackable`), never hard-coded references to specific Modules or entities.
- **Shared rules:** Damage, damage reduction, knockback, resources, statuses, resists, and Defiance are each implemented once and used by every Module and Entity. Defiance is a core Entity rule, not a Module.
- **Time:** A global game timescale affects movement, timers, status durations, DoT ticks, and projectiles. Time Slow and Time Stop exempt the player, so they can't be a plain `Time.timeScale` change. Cooldowns run in real seconds, ignoring the game timescale, but stop while the game is paused. Only player heroes have time powers.
- **Stats:** Strength, Will, Intelligence, Finesse, and Toughness (SWIFT), plus the secondary stat Detectability. Use these exact names.
- **Space and physics:** Gameplay is 2D on the XZ plane, with Y up and an overhead camera. Use only 3D physics, colliders, and queries, never `Physics2D` or 2D colliders; the two can't interact. Rigidbodies freeze Y position and X and Z rotation. Sprites lie flat, facing the camera, and height is visual only. Pathfinding uses the AI Navigation NavMesh, with NavMeshObstacle carving for destructible walls and doors.
- **Data:** Per-item numbers (modules, entities, heroes, props, prices) and tier values come only from `Docs/design/data/*.csv`. An editor menu command imports them into ScriptableObjects in `Data/Generated/`, which code and prefabs use. Never read CSVs at runtime or edit generated assets by hand. Before building or changing the importer, read `Docs/claude/data-importer.md`.
- **Scenes and flow:** `Boot` → `HQ` → `Mission` → back to `HQ`. `Boot` creates long-lived services, then loads `HQ`. `HQ` starts a mission by handing a mission setup object to `Mission`, which assembles the map at runtime. `Sandbox_*` scenes are for testing single systems and are never in a build.
- TODO: saving and UI structure, as they're decided.

## Design docs
- `Docs/design/README.md` is the index; each topic has one owning file. Read it before implementing a mechanic. If `Docs/design/` doesn't exist yet, the docs are at github.com/Dehaku/Zenthium-Swift-, branch `claude/hero-game-design-brainstorm-zdo1or`.
- The docs are the source of truth. If they're silent, ambiguous, or list the point under `## Open`, ask instead of inventing a rule. Never invent numbers or copy them from markdown. Don't edit the docs unless asked.

## Quirks
- The legacy `Input` class throws at runtime because only the Input System is enabled. Read input through the generated C# class of `InputSystem_Actions.inputactions` (typed actions such as `Player.Kick`), not the `PlayerInput` component or action-name strings. The asset still holds the template's default actions; the intended bindings are in `Docs/design/controls.md`.
- Don't use deprecated APIs: use `linearVelocity` on rigidbodies (not `velocity`), and `FindFirstObjectByType` or `FindAnyObjectByType` (not `FindObjectOfType`).
- WebGL: no threads (`Task.Run` doesn't run in parallel), no runtime `System.IO` file access, and no blocking web requests. Keep per-frame allocations low; garbage collection pauses are more noticeable in browsers.

# Workflow
- Before writing a new system, manager, or helper, search for an existing one and extend it instead. If a new one is needed anyway, say why.
- For changes that span several systems, outline your plan and wait for approval before editing.
- Ask before adding packages, plugins, or third-party code.
- Don't commit or push unless asked. Commit message format: `Area: what changed` (e.g. `Modules: add Kick knockback`).

# Code guidelines

## How to apply these
- These are defaults, not absolute rules. If following one would make a specific piece of code harder to understand or modify, don't. "Test integrity" is the exception; those rules are firm.
- Write new code to these guidelines. Apply them to existing code only within the current task; don't refactor, rename, or reorganize unrelated code. Mention problems elsewhere in your summary instead.
- Where the codebase has its own convention (casing, accessor style, event pattern), follow the codebase.
- Before finishing, review your diff against these guidelines.

## Structure
- Each class does one thing: movement, audio, animation, and UI for the same entity are separate classes. If a function's accurate name needs "and", split it. Don't split so far that the pieces are harder to follow.
- Gameplay logic must not reference animators, audio, UI, or visual components. Presentation classes subscribe to the logic class's events (`OnHealthChanged`), so removing one never breaks the logic. Always unsubscribe when the listener is disabled or destroyed.

## State and access
- Fields are private by default. Expose access only when another class needs it, through properties or methods. Before reducing an existing member's visibility, update every usage.
- Each piece of state lives in exactly one class; others read it from there instead of keeping a copy. Derive values instead of storing them separately (`correctCode.Length`, not a `codeLength` field).

## Literals and identifiers
- Don't hardcode gameplay numbers or duplicate data values in serialized fields; they come from the generated data assets. Other tunable values, such as camera or UI feel, become serialized fields; fixed values become named constants. Obvious values like 0 and 1 are fine.
- Don't use strings to identify objects, states, types, or events; use enums, types, components, or direct references. When an API requires an identifier string, define it once as a constant.

## Comments and dead code
- Comment only the *why*: non-obvious decisions, ordering constraints, and workarounds a future reader might "fix" and break. Never restate the code.
- Remove code your change makes unused. Don't leave commented-out code.

## Tests
The user compiles the project and runs all tests. Never run tests, build, or launch Unity from the command line.

### When to write tests
- Only for logic worth protecting (game rules, calculations, state changes, data handling) that the current task added or changed. Not for visuals, audio, game feel, tuning values, or prototype code unless asked.
- Cover the main behavior and the edge cases most likely to break; exhaustive coverage is not the goal.
- When fixing a bug in testable logic, add one regression test that would fail without the fix.
- If the project has no test setup yet, don't create one; ask first.
- Before writing or editing tests, read `Docs/claude/writing-tests.md`.

### Test integrity
- Never change a test's assertions, or skip or disable a test, to make it pass. Change expected behavior only when the user asked for that behavior change, and say so in your summary.
- Updating tests for renamed or moved code is fine, as is deleting tests for removed features or tests that can't fail; list deleted tests in your summary.
- Never special-case game code to make a test pass.

### Handing off
- Your work is implemented but not verified, since you can't compile or run tests. Never say a change works, is fixed, or passes; say what you implemented and what still needs checking.
- Before finishing, re-read your changes for compile errors: missing `using` directives, mismatched signatures, and references to anything renamed.
- End your summary with what the user should check: the Unity Console, and the specific test classes to run, including tests for code that uses any shared API you changed. Recommend a full-suite run only when shared code changed.

### When the user reports a failure
- Fix the cause in the game code. If the test itself looks wrong, say so instead of editing it.
- If the failure is in code your change didn't touch, say it may be pre-existing; don't fix it unless asked.
- If the same test still fails after two fix attempts, stop and explain what you've found.

# Unity rules
- Expose fields to the Inspector with `[SerializeField] private`, not `public`.
- Don't use `?.`, `??`, or `is null` on `UnityEngine.Object` types; they bypass Unity's destroyed-object check. Use `== null` or `if (obj)`.
- Don't hand-edit `.unity`, `.prefab`, or `.asset` files, or GUIDs in `.meta` files; describe the change for the user to make in the Editor.
- Ask before changing `Packages/manifest.json`, `ProjectSettings/`, or `.asmdef` files. Never touch `Library/`, `Temp/`, or `Logs/`.
- When renaming a serialized field, add `[FormerlySerializedAs("oldName")]`, or its Inspector values are lost.
- A MonoBehaviour's class and file names must match, one per file. When renaming or moving a script, move its `.meta` file with it.
- Don't use `GameObject.Find`, `transform.Find`, or tags. Use serialized references, and marker components with `TryGetComponent`.
- Animator parameters: `private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");`
- Unity calls some code with no code references: message methods (`Awake`, `OnTriggerEnter`), Inspector-wired UnityEvents, animation events, and `SendMessage` targets. Never delete these as unused.
