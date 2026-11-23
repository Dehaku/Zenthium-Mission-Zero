# Source of these design docs

This folder is a read-only mirror. Edit the design in the source repo, then refresh the mirror.

| | |
|---|---|
| Repo | https://github.com/Dehaku/Zenthium-Swift- |
| Branch | `claude/hero-game-design-brainstorm-zdo1or` |
| Folder | `design/` (copied here), plus `tools/balance.py` (copied to `Docs/tools/`) |
| Commit | `02bd9b8a85d705a3fdea014f49d1e3e5daca775d` (2026-11-23, "Quote the Cosmetic price note, which has a comma") |
| Mirrored | 2026-11-22; updated to the commit above on 2026-11-23 by applying its one-line change |

## Refreshing
1. Clone or pull the branch somewhere outside the project.
2. Replace `Docs/design/` with the branch's `design/` folder, keeping this file, and replace `Docs/tools/balance.py`.
3. Update the commit and date above.
4. Re-run the data import in Unity (Milestone 1) if any CSV changed.

`balance.py` works from `Docs/tools/` unchanged: it reads `Docs/design/data/`. Run it with `python Docs/tools/balance.py report`.
