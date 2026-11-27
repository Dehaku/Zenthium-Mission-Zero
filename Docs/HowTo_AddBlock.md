# How to add a city block

Maps are built at runtime from hand-made blocks (`design/maps.md`, Generation). `MapAssembler` lays them on a street grid. It turns each block 0, 90, 180 or 270° at random. Every block of the mission's district can fill the map. One block that holds the mission's location becomes the key block.

The quickest start is to duplicate a graybox block from `Assets/_Project/Prefabs/Blocks/Graybox/` and edit it. Move the copy out of that folder first: Zenthium > Map > Create Graybox Blocks replaces everything in it.

## Blocks needed
Each location in `design/maps.md` needs at least one block in its district, except SWIFT Tower (Story Arcs only). That's 16 blocks:
- **Downtown:** Bank, Store, Club, Hospital, Museum, Library, Records office.
- **Industrial:** Lab, Armory, Warehouse.
- **Residential:** House, Apartment building, Small shop, Antique shop, Mansion, Park.

One block can hold several locations, such as a Bank next to a Store. Zenthium > Map > Refresh Block Library warns about every location that no block holds yet.

## Rules
- **Footprint:** a square `block_size_m` across (`constants.csv`, 68 m), centered on the block's root, with the root at Y 0. The streets (`street_width_m`, 12 m) lie outside it, so keep everything inside the square. Leave a few meters between buildings and the edge as a sidewalk.
- **Any side can face any street.** The block may be turned, so don't rely on north or south.
- **No ground needed.** The map has one ground for all blocks. A flat lot visual with no collider is optional (the graybox lots show the district's color).
- **No scene references.** Leave every Module World, Registry and Service field empty. The map builder fills them in when it places the block.
- **No entities.** Missions spawn goons, citizens and Valuables into sockets (Milestone 9).

## Steps
1. **Root.** Create an empty GameObject at the origin and name it `Block_<Location>` (for example `Block_Bank_2`). Add **Map Block**:
   - Set **District**.
   - Add a location to **Extra Locations** only if it has no Building, such as a Park.
2. **Buildings** (`design/world.md`, Buildings): for each building, add a child with a **Building** component.
   - Set its **Location**. Leave **Registry** empty.
   - Neighboring buildings never share walls: give each building its own.
3. **Interior zones:** under the building, one child per box of floor (an L-shape needs two). Each one has:
   - a **Box Collider** with **Is Trigger** on;
   - an **Interior Zone**;
   - a **NavMesh Obstacle** (Box, the same size, **Carve** on). It switches itself off until the building burns.
4. **Walls and windows:** under the building, a Cube for each, with **Structure**, **Building Part** (set **Material**) and a **NavMesh Obstacle** (Box, size 1, **Carve** on).
   - Windows are Glass and also get **See Through**.
   - Locked doors are set by rank from Milestone 9, so build every door unlocked.
5. **Doors:** an empty child across the doorway, with a **Box Collider** (trigger) reaching about 1.5 m either side of the wall, and a **Door**. Its panel is a child Cube with **Structure** and **Building Part** but **no** NavMesh Obstacle: enemies plan paths as if doors weren't there. Drag the panel into the Door's **Panel**.
6. **Sockets** (`design/maps.md`, Sockets): an empty child with a **Map Socket**. Add **Options**, each a content and a weight.
   - **Arson:** one 1.5 m outside every door and window, as a child of its building.
   - **Valuables** (and Citizen) inside buildings, as children of the building.
   - **Open ground:** each block needs at least one socket that offers **Crop Circle**, **Goon** and **Citizen**, with 3 m clear around it (the crop circle radius).
7. **Fixed obstacles** (fences, ponds, statues): plain colliders that are taller than 0.75 m, the agents' step height. The NavMesh bake keeps them out of paths. Props with **Prop Body** carve their own holes, like walls do.
8. **Save** it as a prefab anywhere under `Assets/_Project/Prefabs/Blocks/` (for example `Blocks/Downtown/`), then delete it from the scene.
9. **Run Zenthium > Map > Refresh Block Library.** The Console lists the block count and warns about locations with no block.
10. **Test** in `Sandbox_Map`: pick the block's location and Build. The key block always holds it. Build a few seeds to see the block turned different ways.

## Checks when a block misbehaves
- **Error that a Building needs the Building Registry:** the block was placed by hand into a scene. Only the map builder places blocks.
- **Agents walk through something:** it has no collider, or it's lower than 0.75 m.
- **Agents stop at a wall that's gone:** the wall has no NavMesh Obstacle with Carve on, so it was never carved and the gap was never opened.
