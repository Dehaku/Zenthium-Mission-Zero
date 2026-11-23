# Zenthium SWIFT Heroes: Build Plan

Milestones for Claude Code to build the game in this Unity project.

- **Design:** `Docs/design/`, mirrored from GitHub `Dehaku/Zenthium-Swift-`, branch `claude/hero-game-design-brainstorm-zdo1or` (Milestone 0). In this plan, `design/x.md` means `Docs/design/x.md`.
- **Project rules:** `CLAUDE.md` at the project root. It wins over this plan, and the design docs win on mechanics. If this plan disagrees with either, follow them and point out the conflict to the user.

---

## How a milestone session works

1. **One milestone per session** (or one lettered part of it). Start with "Build milestone N from `Docs/BuildPlan.md`."
2. **Read** `CLAUDE.md`, this section, and the milestone's *Design refs*. Read nothing else from `Docs/design/` unless the work touches it.
3. **Outline first.** Every milestone spans several systems, so per `CLAUDE.md` post a short plan (classes, files, data, Editor setup you'll need from the user, questions) and wait for approval before editing.
4. **Ask, don't invent.** If the design is silent, ambiguous, or lists the point under `## Open`, ask. If a number is needed and isn't in `Docs/design/data/*.csv`, ask the user where it should go; don't copy it from markdown.
5. **Ask before** adding packages, changing `Packages/manifest.json`, `ProjectSettings/` or `.asmdef` files.
6. **Editor work is the user's.** Claude doesn't hand-edit `.unity`, `.prefab` or `.asset` files. Claude writes the code, then gives numbered Editor steps for the user (create this prefab, add these components, set these references). Keep the steps short and exact.
7. **Stubs:** if a piece needs a later milestone, stub it behind an interface and list it in the log entry.
8. **Hand-off, not verification.** Claude can't compile, run tests or launch Unity. Each milestone ends with:
   - what was implemented,
   - the Editor steps still to do,
   - what to check: the Console, the test classes to run, and the *You check* items below.
   Claude never says a milestone works. The user ticks the *You check* boxes after checking.
9. **Log:** add an entry to `Docs/BuildLog.md`: what was built, deferred items, open questions, and the Editor steps given.
10. **Architecture decisions:** when a milestone settles something `CLAUDE.md` lists as TODO (saving, UI structure, scene and mission flow), propose the wording for its Architecture section and let the user add it or approve the edit.
11. **Commits:** don't commit or push unless asked. Message format: `Area: what changed`.

### Fixed by CLAUDE.md (summary; `CLAUDE.md` is the authority)
| Area | Rule |
|---|---|
| Target | WebGL. No threads, no runtime `System.IO`, no blocking web requests, low per-frame allocations |
| Space | 2D gameplay on the XZ plane, Y up, overhead camera. 3D physics only; rigidbodies freeze Y position and X and Z rotation. Sprites lie flat; height is visual only |
| Pathfinding | AI Navigation NavMesh, with NavMeshObstacle carving for destructible walls and doors |
| Rendering | URP 3D Universal Renderer, as set up now |
| UI | uGUI with TextMeshPro |
| Input | The generated C# class of `InputSystem_Actions.inputactions` |
| Code | `Assets/_Project/Scripts/<Feature>/`; editor code in `Scripts/Editor/` |
| Data | `Docs/design/data/*.csv` → importer → ScriptableObjects in `Assets/_Project/Data/Generated/` (`Docs/claude/data-importer.md`) |
| Tests | Only for logic worth protecting, per `Docs/claude/writing-tests.md`. The user runs them |
| Version control | Git on GitHub (`Dehaku/Zenthium-Mission-Zero`), with Git LFS for binary assets |
| Template leftovers | `TutorialInfo`, `Readme.asset`, `SampleScene`: ignore them |

---

## Milestone overview
| # | Milestone | Playable result |
|---|---|---|
| 0 | Foundation | Design docs on disk, input bindings, test setup, the packages decided |
| 1 | Data pipeline | CSVs import into game data |
| 2 | Entity and combat core | Entities with stats, HP, damage, resources and Defiance |
| 3 | Player controller | Move, aim, sprint, Kick and Drag a dummy around a test room |
| 4 | Modules and statuses | The three starting heroes' full kits work against dummies |
| 5 | World: buildings, props, fire, Valuables | Break walls, throw cars, burn buildings, carry loot |
| 6 | Enemy AI | Goons notice you, alert each other, fight, loot and flee |
| 7 | First faction: Galactic Repo Agency | Greys and Champions with crop circles, in a sandbox |
| 8 | Map generation | A random Small, Medium or Large Blazithion map with fog of war |
| 9 | Missions (vertical slice) | **A full rank D mission of any of the 4 types** |
| 10 | HUD and feedback | The full HUD and comic feedback |
| 11 | HQ, economy and progression | **The core loop: HQ → mission → rewards → upgrades → harder ranks** |
| 12 | Story Arcs | Hostile Takeover plays as the first mission of the game |
| 13 | Remaining heroes | All 8 preset heroes playable |
| 14 | Remaining factions | All 6 factions and the Zenthium Fissure |
| 15 | Recruits, late arcs and S ranks | Headhunted, Create-a-Hero and Relic Fever; S2+ works |
| 16 | Polish and WebGL release | Art, audio, juice, options, a WebGL build |

Milestones 0-9 are the vertical slice. Stop for a user playtest after 9 and after 11.

---

## Milestone 0: Foundation
**Goal:** the design on disk, and the setup decisions made.

**Design refs:** `design/README.md`, `design/controls.md`, `design/maps.md` (Visibility).

**Claude does**
- [ ] Copy the design branch's `design/` folder to `Docs/design/` and `tools/balance.py` to `Docs/tools/balance.py` (its paths then resolve to `Docs/design/data/` unchanged). Add `Docs/design/SOURCE.md` with the repo, branch, commit hash and how to refresh it. Don't edit the docs.
- [ ] Create `Docs/BuildLog.md`.
- [ ] Ask the user to approve these package additions, one by one, with the reason for each:
  - Unity Test Framework (`CLAUDE.md`: no test setup yet, ask first).
  - AI Navigation (`com.unity.ai.navigation`), for the NavMesh.
  - TextMeshPro, if Unity 6.6 doesn't already include it with uGUI.
  - A fog of war asset (`design/maps.md` mentions one). Research options that work on WebGL with 3D physics on XZ, and present 2-3 choices with trade-offs. Integrating it comes in Milestone 8.
- [ ] Input bindings: the asset still has the template's actions. Propose the action map from `design/controls.md` (Move, Aim, Power1-4, Gear1-4, Kick, Drag, Sprint toggle, MoreInfo hold, FriendlyFire hold, Pause, SkipHold), for keyboard and mouse and for gamepad. Ask whether the user will set it up in the Editor from a step list, or wants Claude to edit the `.inputactions` JSON. Either way, the generated C# class must be turned on.
- [ ] Propose the scene list (`Boot`, `HQ`, `Mission`, plus `Sandbox_*` test scenes) and the first-pass scene and mission flow, for the Architecture TODO in `CLAUDE.md`.
- [ ] Propose a folder and assembly definition layout under `Assets/_Project/` (one assembly per feature folder, plus Editor and test assemblies). Create it only after approval.
- [ ] An overhead camera rig script: orthographic, looking down the Y axis at the XZ plane, following a target. Give the Editor steps to place it.

**You check**
- [ ] `Docs/design/` matches the design branch.
- [ ] The packages you approved are installed, with no Console errors.
- [ ] The input actions match `design/controls.md`, and the generated C# class compiles.
- [ ] The scenes and assembly definitions exist, and the project compiles cleanly.

---

## Milestone 1: Data pipeline
**Goal:** every per-item number lives in the CSVs and reaches the game as typed data.

**Design refs:** `Docs/claude/data-importer.md`, `design/balance.md`, `design/data/*.csv`, `Docs/tools/balance.py`, `design/modules.md` (Schema).

**Claude does**
- [ ] An importer, run from a menu command, following `Docs/claude/data-importer.md`:
  - Constants and standard tier tables from `constants.csv`.
  - One Module data asset per row of `modules.csv`, parsing the `values` column's `key=value; key=value` pairs.
  - Entity, hero, prop and price data from the other CSVs.
  - A generated constants class of IDs, so code never names an entry by string.
- [ ] Plain C# `BalanceFormulas` that port `design/balance.md` sections 1 and 4.
- [ ] A runtime lookup for the generated assets, built on direct references.
- [ ] Tests: the formulas against hand-picked expected values for the anchors in `design/balance.md` section 3. Take the literals from a `balance.py report` run, and ask the user to run it and paste the output if needed. Also test the importer's failure cases: unknown tier word, duplicate ID and missing reference.

**You check**
- [x] The menu import runs with no errors, and the assets appear in `Data/Generated/`.
- [x] Change a value in a CSV and re-import: the asset updates in place, and references to it survive.
- [x] Run the new EditMode test classes Claude names.

---

## Milestone 2: Entity and combat core
**Goal:** the rules every entity shares, as plain C# with thin MonoBehaviour wrappers.

**Design refs:** `design/stats.md`, `design/combat.md`, `design/time.md`, `design/balance.md` (sections 1-2).

**Claude does**
- [ ] One Entity type: team (Player, Ally, Enemy, Neutral, Civilian), rank tier, entity tags, and module slots (filled in Milestone 4).
- [ ] SWIFT stats and Detectability, with stacking exactly as in `design/stats.md`. Percentage boosts multiply, except the difficulty bonus, which adds to the innate rank boost.
- [ ] Derived values: HP, Armor, dodge and move speed.
- [ ] The damage pipeline: damage type, dodge, Armor then Damage Reduction (multiplied sources), the 5% minimum, DoTs ignoring both, and friendly-fire rules.
- [ ] Resources: pools built from module grants, +1/s regen, and `NoRegen`.
- [ ] Defiance as a core Entity rule: proc chance, guaranteed procs, Defiance Roll stacks, the cleanse and Invulnerable time. The presentation comes later, through events.
- [ ] The game clock (`CLAUDE.md`, Architecture: Time): a game timescale that exempts the player during Time Slow and Time Stop; cooldowns in real seconds that stop while paused.
- [ ] Logic events (`OnDamaged`, `OnHealthChanged`, `OnIncapacitated`, `OnDefianceProc`, `OnResourceChanged`) for presentation to subscribe to.
- [ ] Tests from the worked examples: Chad's Strength 90, the 63-damage hit, the 213% Defiance sequence, Damage Reduction sources multiplying (two 50% sources give 75%), and the 5% minimum.
- [ ] A small debug panel (uGUI) for `Sandbox_Combat` to damage, heal and change a dummy's stats, with Editor steps.

**You check**
- [ ] The tests pass.
- [ ] In `Sandbox_Combat`, the debug panel changes a dummy's HP, Armor and Defiance chance as expected.

---

## Milestone 3: Player controller
**Goal:** the hero feels good to move and fight with the base actions.

**Design refs:** `design/controls.md`, `design/modules.md` (Kick, Drag and Sprint rows), `design/combat.md` (Movement, Knockback), `design/missions.md` (Citizens: Stubborn drag slow).

**Claude does**
- [ ] Movement on XZ through a rigidbody (`linearVelocity`), aiming at the mouse via a ground-plane raycast, or with the right stick. Sprint toggle.
- [ ] Kick: Normal damage from Strength, Normal knockback from the tier, and the cooldown from data.
- [ ] Knockback as a fixed distance per tier that can't tunnel through walls. Hitting a wall raises an event that Milestone 5 uses for wall damage. `KnockbackResistant` blocks it.
- [ ] Drag: the target priority from the Drag row, carry capacity from Strength, dragged targets can't be hurt, and you can fight while dragging.
- [ ] Hold-to-enable friendly fire.
- [ ] A training dummy: a goon Entity with no AI.
- [ ] Editor steps for the player prefab, the dummy prefab and the sandbox room.

**You check**
- [ ] With keyboard and mouse, and with a gamepad: walk, sprint, aim, Kick a dummy across the room, and Drag it.
- [ ] About 3 Kicks from a Strength 20 hero defeat a rank D goon (the balance anchor).

---

## Milestone 4: Modules and statuses
**Goal:** the data-driven module system, proven on the three starting heroes.

**Design refs:** `design/modules.md`, `design/statuses.md`, `design/time.md`, `design/heroes.md` (Rush Hour, Major Overclock and Rune kits; Slots), `design/balance.md` (sections 4-5).

**Claude does**
- [ ] The shared Module parts from `CLAUDE.md`: category, tags, triggers (always-on, on hit, on damage taken, on Defiance proc, on timer, on activation, on approach), effects, stat formula, resources, damage type and cooldown. Each data Module ID links to its behavior.
- [ ] Reusable effect pieces, such as damage, knockback, apply status, projectile, area, dash, heal, summon and grant resource. One-off code only where a module is truly unique.
- [ ] Slots: 4 active, 4 passive and 4 gear, locked or unlocked; gear actives on their own buttons; no duplicate modules.
- [ ] Statuses: Refresh, Intensity and Independent stacking; multiplied slows; half-length crowd control on Leaders and the player; pulsing "while inside" effects; `Resist:<Stat>`; Invulnerable; disable statuses; `CannotBeDisabled`. Statuses link to modules through tags only.
- [ ] Time Slow and Time Stop, with frozen projectiles that have no collision.
- [ ] Holding MoreInfo shows the module formula text with live numbers. Plain text is fine for now.
- [ ] The full kits of Rush Hour, Major Overclock and Rune. Stub what needs Milestone 5, such as Breaching Charge opening walls.
- [ ] Tests: status stacking per rule, resist chance (equal stats give 50%), slow multiplication, and cooldowns ignoring the timescale.
- [ ] Editor steps for a hero select in `Sandbox_Combat`.

**You check**
- [ ] Each of the 3 heroes' modules does what its row in `design/modules.md` says, against dummies.
- [ ] Time Stop freezes enemies and projectiles while you keep moving.
- [ ] The tests pass.

---

## Milestone 5: World: buildings, props, fire and Valuables
**Goal:** a destructible, flammable, lootable city to fight in.

**Design refs:** `design/world.md`, `design/maps.md` (Sockets), `design/data/props.csv`.

**Claude does**
- [ ] Walls, doors and windows with HP by material, and NavMeshObstacle carving when they break. Doors open when touched; locked doors are solid. Buildings and props block line of sight.
- [ ] Props in 3 sizes with the Strength limits for Drag and Power Throw. Kick throws a dragged prop. Vehicles have Armor and Damage Reduction, and explode 1 s after being destroyed, with a red ring.
- [ ] The building structure from `design/world.md`: a building root, interior zones with live lists of what's inside, child walls, doors and sockets, and a building registry with neighbors.
- [ ] Fire: building states, burn ticks inside, a random wall or door destroyed per tick, burning out, `Ignites` and `Extinguish`, and spreading (switched on by rank in Milestone 11).
- [ ] Valuables: pickup, Carrying Treasure, the money bag icon, dropping on defeat, Kick-launching bags, melting at burn-out, and the lost count.
- [ ] The Heavy Turret as a neutral Entity. Its AI comes in Milestone 6.
- [ ] Tests for the plain-C# rules: fire tick targets, which Valuables melt at burn-out, neighbor lists, and Carrying Treasure's cap.
- [ ] Editor steps for two test buildings (one L-shaped) in `Sandbox_World`.

**You check**
- [ ] Break each wall material, throw a car into a wall, set a building on fire and put it out, and carry and launch Valuables.
- [ ] The tests pass.

---

## Milestone 6: Enemy AI
**Goal:** enemies that notice, fight, loot and flee as designed.

**Design refs:** `design/enemies.md` (AI, Limits), `design/stats.md` (Detection), `design/world.md` (Goon behavior), `design/missions.md` (Citizens).

**Claude does**
- [ ] Detection: vision range, line of sight with 3D raycasts, and an awareness bar that fills by distance and Detectability.
- [ ] Becoming aware: the "!", the 0.5 s delay, and the chained 10 m alert circle.
- [ ] States: Idle (patrolling sockets at half speed), Combat, Searching, Looting and Objective.
- [ ] Using modules in list order: the first one off cooldown, affordable and with a target in range.
- [ ] Targeting: the nearest valid target, Taunt override, turning on whoever hit it, side preferences and neutral rules.
- [ ] NavMesh movement: melee chases; ranged keeps range and backs off within 3 m. Pathing treats doors as open, but a locked door blocks, so the enemy attacks it until it breaks. Burning buildings are no-go areas.
- [ ] Telegraphs, cowardice, friendly fire between enemies without retargeting, and the dev toggle.
- [ ] Citizens: Scared, Incapacitated and Stubborn, plus their Valuables drop rules.
- [ ] Allies follow the player within about 10 m outside combat.
- [ ] Tests: awareness timing (instant at 10 m, 10 s at 100 m), alert chaining, module choice order, and the cowardice roll.

**You check**
- [ ] In `Sandbox_AI`, a squad patrols, notices you, chain-alerts, fights, loses you behind a building and searches, loots while you're out of range, and runs below 20% HP.
- [ ] The tests pass.

---

## Milestone 7: First faction: Galactic Repo Agency
**Goal:** a real faction, built as the template for the rest. It comes first because Hostile Takeover, the opening arc, uses only the Repo Agency.

**Design refs:** `design/factions.md`, `design/Factions/GalacticRepoAgency.md`, `design/Factions/_Template.md`, the Repo Agency modules in `design/modules.md`.

**Claude does**
- [ ] Faction data: entities, rank tiers, innate boosts, minimum ranks, locked modules, faction tags, arrival tag, and Mission and Location Weights. Reusable by every faction.
- [ ] Entities: GleebPeep, Tractor Drone, Shield Bearer and Grey first, then Auditor, Notary and Tow Saucer.
- [ ] Their modules: Energy Blaster, Tractor Pull, Tractor Beam, Tow Beam, Call for Backup, Repossess, Final Notice, Tablet Slap, Audit and Notarized.
- [ ] Crop circles: abduct citizens, strip Valuables, act as the loot destination, and scale in count with rank.
- [ ] `Arrival:Beam`, stubbed until Milestone 9.
- [ ] Mothership Strike, switched on from rank S in Milestone 11.
- [ ] `Docs/HowTo_AddFaction.md`: the steps for adding the next faction.

**You check**
- [ ] A Repo squad drags a citizen into a crop circle, and GleebPeep uses each unlocked module in priority order.

---

## Milestone 8: Map generation
**Goal:** random Blazithion maps built from hand-made blocks.

**Design refs:** `design/maps.md`, `design/world.md` (Buildings).

**Claude does**
- [ ] A block prefab format: size, streets, buildings, and sockets with weighted tags.
- [ ] The assembler: pick the district from the location, lay blocks on a street grid at Small, Medium or Large size, place the key building, and build the registry.
- [ ] A socket-filling API for Milestone 9.
- [ ] Exit zones on the map edges.
- [ ] A runtime NavMesh bake for the assembled map, and a check that it isn't too slow on WebGL.
- [ ] Integrate the fog of war option the user picked in Milestone 0.
- [ ] Block authoring guide `Docs/HowTo_AddBlock.md`: the Editor steps for building a block prefab.
- [ ] Tests for the plain-C# layout logic: no overlaps, every block reachable from a street, and the map size matches.

**You check**
- [ ] Build graybox blocks for the 3 districts with the guide: at least one block for each location in `design/maps.md`, except SWIFT Tower. Claude can list exactly which blocks are needed.
- [ ] The launcher generates maps of every size with no errors. Fog of war reveals by line of sight, including through windows, doors and broken walls.
- [ ] A Medium map plays smoothly in a WebGL development build.

---

## Milestone 9: Missions (vertical slice)
**Goal:** a complete, winnable and losable rank D mission of each type.

**Design refs:** `design/missions.md`, `design/enemies.md` (Threat budget, Squads, Mission type profiles, Reinforcements, Limits), `design/difficulty.md` (rank D only), `design/factions.md` (Weights), `design/economy.md` (failing).

**Claude does**
- [ ] A mission setup: type, faction, location, rank, size, Leader, secondary goal and hazards. Random rolls go type, then faction by Mission Weight, then location by Location Weight.
- [ ] The spawner: threat budget, Champion share, squads, the profile per mission type, the population cap, and the Leader's entrance at 30 s.
- [ ] Reinforcements and arrival tags. Built now, switched on from rank B in Milestone 11. Invasion waves work at every rank.
- [ ] The 4 mission types with their goals and fail conditions, including Arson Riot's Ignite action.
- [ ] Extraction: Drag to an exit, extraction gear, and capture in exit zones.
- [ ] Every secondary goal in `design/missions.md`.
- [ ] The start sequence (3 camera moves, the mission text and hold-to-skip), the end of the mission, and a bare results screen.
- [ ] A debug mission launcher with a hero picker.
- [ ] Tests: the budget spend, mission rolls with weights, and each type's fail checks.
- [ ] Propose the scene and mission flow wording for `CLAUDE.md`.

**You check**
- [ ] With each starting hero, win and lose all 4 mission types against the Repo Agency at rank D, on Small and Medium maps.
- [ ] A typical mission lasts about 5 minutes, with a fight every 20-30 s (`design/balance.md`).
- [ ] **Playtest checkpoint** before Milestone 10.

---

## Milestone 10: HUD and feedback
**Goal:** the player can read everything that's happening.

**Design refs:** `design/hud.md`, plus the file each of its rows names.

**Claude does**
- [ ] Propose the uGUI structure (canvases, prefabs, how panels bind to logic events) for the UI TODO in `CLAUDE.md`.
- [ ] Near the player: health bar, Defiance chance, resource bars and status row.
- [ ] Ability bar: active slots, gear actives, Kick and Drag, passive icons, cooldown sweeps, the can't-afford tint and the disabled cross.
- [ ] Objective tracker, secondary goal, Valuables counter and Hazard icons.
- [ ] Edge arrows, enemy health bars and the Leader's top bar.
- [ ] Every in-world indicator in `design/hud.md`, as world-space uGUI or sprites lying on XZ.
- [ ] Damage numbers, warning banners, and the Defiance moments for the player and others.
- [ ] Pause menu and options menu (`design/hud.md` Options), saved in a WebGL-safe way (PlayerPrefs, or what the user picks).
- [ ] Gamepad navigation for every menu.
- [ ] All presentation subscribes to logic events and unsubscribes when disabled.

**You check**
- [ ] Every row of `design/hud.md` shows up in a mission.
- [ ] The HUD scales correctly at different browser window sizes and HUD scales.

---

## Milestone 11: HQ, economy and progression
**Goal:** the full core loop between missions.

**Design refs:** `design/hq.md`, `design/economy.md`, `design/difficulty.md`, `design/heroes.md` (Preset heroes, Slots), `design/balance.md` (sections 6-7), `design/data/prices.csv`.

**Claude does**
- [ ] Saving: propose a WebGL-safe approach (no runtime `System.IO`) for the saving TODO in `CLAUDE.md`, and wait for approval. Save Zenny by family, heroes, per-hero stats, slots, module tiers and reward bonus, ranks, missions on the map, arc state, and options. Make saves versioned.
- [ ] Zenny families, reward mixes per mission type, added bonuses, the hero reward bonus and the dev options.
- [ ] The results screen with Zenny crystals flying in.
- [ ] HQ tabs: Training Room (prices rise per point), Zen Garden and Research Lab (buy, upgrade tiers, equip), Meeting Room, and Missions.
- [ ] Missions tab: up to 6 missions, spawn timing, 60 s lifetimes that pause during missions, hidden intel and its bonus, Reveal intel, and Call in a tip.
- [ ] Ranks D to S and S2+, unlocking and per-rank scaling. Switch on the earlier rank-gated features: reinforcements, Heavy Turrets, fire spread, Leaders' extra modules, faction rank-S rules, and locked door materials.
- [ ] Mission modifiers: Night Time, and the Zenthium Fissure hook (content in Milestone 14).
- [ ] Hero unlocks with Metallic Zenny, and the HQ background with wandering heroes and quips.
- [ ] Tests: reward bonus math (the 280% example, the bonus drop with its floor), rising stat prices, rank scaling sums, intel hidden chance, and save round-trip of the data model.

**You check**
- [ ] From a new save: play, earn Zenny, buy a stat point and a slot, unlock rank C, and see the change.
- [ ] Rank B has reinforcement waves, and rank A spreads fire.
- [ ] Reloading the browser page keeps everything.
- [ ] **Playtest checkpoint**, including `design/balance.md` benchmark 1.

---

## Milestone 12: Story Arcs
**Goal:** the arc system, with Hostile Takeover as the opening of the game.

**Design refs:** `design/story.md`, `design/StoryArcs/HostileTakeover.md`, `design/lore.md` (HQ cast).

**Claude does**
- [ ] Arc data as hand-authored ScriptableObjects, outside `Data/Generated/`: arcs, stages, plot sequences, checkpoints and branches. New arcs need no code.
- [ ] Plot sequences: `design/story.md` suggests a visual novel library. Present options that work with uGUI and WebGL, or a small custom player, and ask before adding anything.
- [ ] Every checkpoint trigger, action and goal type in `design/story.md`.
- [ ] Arc rules: starting, limits, milestone triggers, stage lifetimes, the cover icon, hidden intel, rewards and unlocks.
- [ ] First run: the game opens on Hostile Takeover, with tutorial tips on the first run only.
- [ ] Hostile Takeover authored in full, with Editor steps for the data and for the SWIFT Tower lobby block. Sundown Sal unlocks with a placeholder kit until Milestone 13.
- [ ] `Docs/HowTo_AddStoryArc.md`.

**You check**
- [ ] A new save plays Hostile Takeover through both the success and the failure branches, then normal missions appear.

---

## Milestone 13: Remaining heroes
**Goal:** all 8 preset heroes playable. One session per part.

**Design refs:** `design/heroes.md`, their rows in `design/modules.md`, `design/data/heroes.csv`.

- [ ] 13a: Heave (Telekinetic Grab and Throw, Mind over Matter)
- [ ] 13b: Sundown Sal (Six-Shooter, Ammo and reloads, Bandolier, Quick Draw, Ricochet, Super Lucky, Grappling Hook)
- [ ] 13c: Gelatina (Gel Coat, Water Cannon, Ooze Through, Absorbent)
- [ ] 13d: Prism (Spectrum Bolt's color cycle, Reaper Battery, Crystal Hunger, Glow Up, Spell Tomes)
- [ ] 13e: Madame Tarot (Card Throw's 10 effects, Stacked Deck, Mirage, Stealth Suit)
- [ ] Unlock rules: buyable heroes; Sal through Hostile Takeover only; Tarot through Relic Fever only.

**You check (per hero)**
- [ ] Every module does what its row says, and the hero can finish a rank C mission.

---

## Milestone 14: Remaining factions
**Goal:** every faction, built from `Docs/HowTo_AddFaction.md`. One session per part.

**Design refs:** `design/factions.md`, each `design/Factions/*.md`, their modules in `design/modules.md`, `design/difficulty.md` (Zenthium Fissure).

- [ ] 14a: Technomafia (hacking and Heavy Turrets, Kingping's loot armor, Hit Contract)
- [ ] 14b: Zombies (Infected, rising, Undying, horror eggs, Pandemic)
- [ ] 14c: Circus of Calamity (Encore!, Spotlight, the Pied screen smear, `Arrival:ClownCar`)
- [ ] 14d: Hexbound Coven (Silenced, Imps, Valuables as magic items, Portal Step, Portal Storm)
- [ ] 14e: Infinity Anarchous (random module draws, Chad as a wandering hazard)
- [ ] 14f: Zenthium and the Zenthium Fissure modifier

**You check (per faction)**
- [ ] It runs all 4 mission types from its minimum ranks, and its faction rules and locked modules appear at the right ranks.

---

## Milestone 15: Recruits, late arcs and S ranks
**Goal:** the late game.

**Design refs:** `design/heroes.md` (Create-a-Hero, Slots), `design/StoryArcs/Headhunted.md`, `design/StoryArcs/RelicFever.md`, `design/economy.md` (Metallic sinks), `design/difficulty.md` (Leaders' extra modules).

**Claude does**
- [ ] The Headhunted milestone arc, with the Newcomer ally.
- [ ] Create-a-Hero: Recruits, naming, starting slots, 3 loadouts, starter modules, buying modules per Recruit, and extra Recruits.
- [ ] The remaining catalog modules: the Recruit starters and the unowned "Hero" modules. Add an EditMode test that lists catalog modules with no behavior linked, so gaps are visible.
- [ ] Relic Fever and the Madame Tarot unlock.
- [ ] Metallic sinks: melting down Metallic Zenny, and cosmetics as palette swaps.
- [ ] Tests that S2+ scaling stays sane far up, such as at S10.

**You check**
- [ ] With debug-granted ranks: trigger and finish Headhunted, create a Recruit, and take them into a mission with a custom loadout.

---

## Milestone 16: Polish and WebGL release
**Goal:** it looks, sounds and feels like a finished comic-book game, in the browser.

**Design refs:** `design/hud.md` (Feedback, Options), `design/lore.md` (tone), `design/modules.md` (Presentation).

**Claude does**
- [ ] An art direction proposal with the user first: palette, character style, and comic panel UI.
- [ ] Presentation hooks for every module: icon, flash, sound cue and callout, all driven by logic events.
- [ ] Audio mixer, volume options and WebGL audio unlock on first input.
- [ ] Juice: screen shake (with its option), hit stop and impact flashes.
- [ ] A WebGL performance pass: allocations per frame, a Large rank S map with 60 enemies and fires, load size and loading time.
- [ ] A title screen and credits.

**You check**
- [ ] A WebGL release build runs from the title screen through Hostile Takeover and into normal play, in the browsers you target.
- [ ] Every module has its icon, flash, sound and callout.

---

## Not in this plan
- Everything in `design/future.md`, including the high-priority Story Arcs for each faction. Plan those after the first playtests.
- Final title (`design/README.md`, Open).
