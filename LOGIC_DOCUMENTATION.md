# GDDV404-402Final Logic Documentation

## Purpose of This Document

This document explains how the project works at a systems level and at a script-by-script level. It is written for a university student audience, so the goal is to be clear and detailed without assuming expert Unity knowledge.

The project is a small turn-based tactics prototype built in Unity. The current playable loop is:

1. The battle scene loads.
2. A grid of tiles is generated.
3. One player unit and one enemy unit are spawned onto the grid.
4. The player selects their unit, previews movement, moves, and attacks.
5. The player ends their turn.
6. The enemy takes its turn automatically.
7. The battle ends when one side has no active units remaining.

The main scene in use is:

- `Assets/Scenes/BattleScene.unity`

## High-Level Architecture

The code is organized into a few major systems:

- `Grid and tiles`
  - Builds the board.
  - Stores tile position, terrain, occupancy, and highlighting.
- `Pathfinding and movement range`
  - Finds paths between tiles.
  - Calculates which tiles can be reached with available movement points.
- `Units and combat`
  - Represents player and enemy units.
  - Handles health, movement, attack checks, attack damage, and death.
- `Turn logic`
  - Controls whether it is the player turn, enemy turn, or a temporary busy state.
  - Resets unit actions between turns.
- `Enemy AI`
  - Makes the enemy decide whether to attack immediately or move toward the player first.
- `Debug tools`
  - Lets the developer paint terrain and place/remove obstacles at runtime.
- `UI and visual support`
  - Shows turn text, battle result, restart button, floating health bars, and attack effects.

## Scene Startup Flow

When `BattleScene` starts, the important startup order is effectively this:

1. `TileManager` loads terrain definitions from the `Resources/TerrainTypes` folder.
2. `GridManager` generates the grid using the tile prefab.
3. Each `GridTile` is initialized with coordinates, terrain data, visuals, and walkability values.
4. `UnitSpawner` creates the player unit and places it on its starting tile.
5. `EnemySpawner` creates the enemy unit and places it on its starting tile.
6. `TurnManager` starts in `PlayerTurn`.
7. `BattleStateManager` prepares battle result UI and restart button state.
8. `TileSelector` begins listening for player mouse/touch input.

This means the board and terrain exist before units are placed, and units exist before the player begins interacting.

## Core Gameplay Loop

The gameplay loop is built around alternating turns:

- During the player turn:
  - The player can select the spawned player unit.
  - The system highlights reachable tiles.
  - Hovering over a reachable tile shows a preview path.
  - Clicking a reachable tile makes the unit move there.
  - Clicking an enemy in attack range triggers an attack.
  - Pressing Space ends the player turn.

- During the enemy turn:
  - The enemy AI checks whether it can already attack the player.
  - If yes, it attacks.
  - If not, it finds a path toward a tile adjacent to the player and moves.
  - If allowed by turn rules, it may attack after moving.
  - Control then returns to the player.

- During the busy state:
  - The project temporarily blocks player actions while movement or enemy logic is running.

## System Relationships

The most important relationships in the project are:

- `GridManager` creates and stores all `GridTile` objects.
- `TileManager` supplies terrain data that `GridTile` uses.
- `AStarPathFinder` depends on `GridManager` and `GridTile`.
- `GridRangeFinder` depends on `GridManager` and `GridTile`.
- `GridUnit` depends on `GridTile`, `TurnManager`, `BattleStateManager`, `AttackEffectData`, and `WorldHealthBar`.
- `TileSelector` is the main player input controller and depends on:
  - `UnitSpawner`
  - `AStarPathFinder`
  - `GridRangeFinder`
  - `TurnManager`
  - `BattleStateManager`
- `EnemyController` depends on:
  - `GridUnit`
  - `GridManager`
  - `AStarPathFinder`
- `TurnManager` depends on:
  - `TileSelector`
  - `UnitSpawner`
  - `EnemySpawner`
  - `EnemyController`
- `BattleStateManager` depends on:
  - `TileSelector`
  - `GridUnit`

## Script-by-Script Breakdown

## Input System

### `Assets/InputSystem_Actions.cs`

This is an auto-generated Unity Input System wrapper. It should not be manually edited because Unity regenerates it from the `.inputactions` asset.

In the `Gameplay` action map, the important actions are:

- `PointerPosition`
  - Used by `TileSelector`, `GridDebugPainter`, and `ObstacleDebugPainter` to know where the cursor is.
- `Click`
  - Used by `TileSelector` for selecting units, moving, and attacking.
- `PaintClick`
  - Bound to right mouse button.
  - Used by `GridDebugPainter` and `ObstacleDebugPainter` to place terrain or obstacles.
- `End Turn`
  - Bound to Space.
  - Used by `TurnManager`.
- `EraseClick`
  - Bound to middle mouse button.
  - Used by `ObstacleDebugPainter` to remove placed obstacles.

This file is important because several gameplay systems are input-driven, but it is infrastructure rather than game logic.

## Grid and Tile System

### `Assets/Scripts/Grid&Tiles/GridManager.cs`

`GridManager` is the script that creates the map grid and stores tile references.

Its main jobs are:

- Define the board size using `widht` and `height`.
- Instantiate a tile prefab in a rectangular pattern.
- Initialize each tile with its grid coordinates.
- Provide utility functions for:
  - checking whether a position is inside the grid,
  - fetching a tile by coordinates,
  - getting neighbor tiles,
  - checking whether a unit can enter a tile.

Important fields:

- `widht`, `height`
  - Board dimensions. There is a spelling typo in `widht`, but it still functions.
- `tileSpacing`
  - Distance between tile centers.
- `tilePrefab`
  - Prefab used to build the grid.
- `tileParent`
  - Transform that keeps the hierarchy organized.
- `tileManager`
  - Passed to tiles so they can look up terrain settings.

Important methods:

- `Awake()`
  - Calls `GenerateGrid()`.
- `GenerateGrid()`
  - Creates a 2D array of `GridTile`.
  - Instantiates one tile per `(x, y)` coordinate.
- `GetNeighbors(GridTile tile)`
  - Returns up, right, down, left neighbors only.
  - This means movement is 4-directional, not diagonal.
- `GetWalkableNeighbors(GridTile tile)`
  - Filters neighbors so only unoccupied walkable tiles remain.
- `CanUnitEnterTile(GridTile tile)`
  - Central walkability check used by other systems when needed.

Connection to the rest of the project:

- `AStarPathFinder` and `GridRangeFinder` both rely on `GridManager` for neighbor lookup.
- `UnitSpawner` and `EnemySpawner` rely on it to find spawn tiles.
- `ObstacleManager` uses it to determine placement validity.

### `Assets/Scripts/Grid&Tiles/GridTile.cs`

`GridTile` represents one board cell. It stores both gameplay information and visual information.

Its main jobs are:

- Store grid coordinates.
- Store terrain type and movement cost.
- Store occupancy state.
- Display terrain visuals and highlight overlays.
- Apply changes when terrain data changes.

Important gameplay data:

- `X`, `Y`
  - Tile coordinates.
- `terrainType`
  - Logical terrain category such as Ground, Water, Hazard, or Blocked.
- `isWalkable`
  - Whether units can enter the tile.
- `movementCost`
  - Base movement cost.
- `isOccupied`, `occupyingUnit`
  - Whether a unit is standing on the tile.

Important visual data:

- `tileRenderer`
  - Used for terrain color and material.
- `highlightOverlayRenderer`
  - Used for movement range, hover, start tile, target tile, and path overlays.

Important methods:

- `Initialize(int x, int y, TileManager manager)`
  - Sets coordinates.
  - Stores `TileManager`.
  - Applies terrain settings.
- `ApplyTerrainSettings()`
  - Loads the terrain data for the tile’s current terrain type.
  - Updates movement cost, walkability, visuals, and spawned decoration.
- `SetOccupant(GameObject unit)`
  - Updates tile occupancy when a unit enters or leaves.
- `GetTraversalCost(bool isFinalDestination)`
  - Returns movement cost with special handling for terrain penalties.
  - The code distinguishes traveling through a tile from stopping on it.

Highlight methods:

- `ShowOverlayColor(Color color)`
- `SetHoverHighlight(Color color)`
- `HideOverlay()`
- `ResetHighlight()`

These are used heavily by `TileSelector`.

Connection to the rest of the project:

- `GridUnit` stores its current tile.
- `AStarPathFinder` and `GridRangeFinder` use the tile’s movement cost and walkability.
- `ObstacleManager` can change tile walkability and terrain type.
- `TileSelector` uses tile highlights to visualize player decisions.

### `Assets/Scripts/Grid&Tiles/TileManager.cs`

`TileManager` loads all terrain definitions from the Resources folder and makes them available through a dictionary.

Its main jobs are:

- Load all `TerrainTypeData` assets from `Resources/TerrainTypes`.
- Build a lookup table from `TerrainType` enum to `TerrainTypeData`.
- Provide terrain definitions to tiles.

Important method:

- `GetTerrainData(TerrainType terrainType)`
  - Returns the matching `TerrainTypeData`.

Why it matters:

This script makes terrain data-driven. Instead of hard-coding movement cost or color in `GridTile`, the tile asks `TileManager` for the settings belonging to the current terrain type.

### `Assets/Scripts/Grid&Tiles/TerrainTypeData.cs`

`TerrainTypeData` is a `ScriptableObject` that stores the rules and visuals for a terrain type.

Important fields:

- `terrainType`
  - Which enum value this asset describes.
- `movementCost`
  - Base cost to move on or into the tile.
- `damageOnEnter`
- `movementPenaltyOnStop`
- `damageOnStop`
- `movementPenaltyOnEntry`
- `isWalkable`
- `tileColor`
- `tileDecorationPrefab`
- `decorationOffset`
- `tileMaterialOverride`

Current use in code:

- Directly used:
  - `movementCost`
  - `movementPenaltyOnEntry`
  - `isWalkable`
  - visual fields
- Defined but not currently applied in gameplay:
  - `damageOnEnter`
  - `damageOnStop`
  - `movementPenaltyOnStop`

This is important for understanding the project’s maturity: the terrain system is designed to support richer mechanics than the current battle loop actually uses.

### `Assets/Scripts/Grid&Tiles/GridRangeFinder.cs`

`GridRangeFinder` calculates every tile the selected unit can reach with its movement budget.

Its main jobs are:

- Start from the unit’s current tile.
- Explore outward through neighbors.
- Track traversal cost.
- Return a dictionary of reachable tiles and the cheapest cost to reach them.

How it works:

- It uses a cost-based search, similar in spirit to Dijkstra’s algorithm.
- A tile can be:
  - reachable as a final destination,
  - not necessarily cheap enough to continue traveling through.

Why there are two cost calculations:

- `destinationCost`
  - Cost if the tile is the final stop.
- `traversalCost`
  - Cost if the unit is only passing through it.

This matches the terrain design where entering a tile and stopping on a tile may have different penalties.

Connection to the rest of the project:

- `TileSelector` calls this when a unit is selected.
- The returned dictionary becomes the list of highlighted movement options.

### `Assets/Scripts/AStarCore/AStarPathFinder.cs`

`AStarPathFinder` finds the best path between two tiles.

Its main jobs are:

- Build a node map for the grid.
- Run the A* pathfinding algorithm.
- Respect tile walkability, occupancy, and movement cost.
- Return the final path as a list of `GridTile`.

Key pathfinding rules:

- Movement is only 4-directional.
- Non-walkable tiles are ignored.
- Occupied tiles are ignored unless the occupied tile is the target.
- Terrain traversal cost influences path choice.
- Manhattan distance is used as the heuristic.

Main methods:

- `FindPath(GridTile startTile, GridTile targetTile)`
  - Public entry point.
- `CreateNodeMap()`
  - Wraps each tile in a `PathNode`.
- `CalculateHeuristic()`
  - Uses Manhattan distance.
- `GetLowestFCostNode()`
  - Chooses the best open node.
- `RetracePath()`
  - Builds the final tile path.

Connection to the rest of the project:

- `TileSelector` uses it for preview paths and final movement paths.
- `EnemyController` uses it to move the enemy toward the player.

### `Assets/Scripts/AStarCore/PathNode.cs`

`PathNode` is a helper class for A*.

It stores:

- the tile it represents,
- `GCost` for travel cost so far,
- `HCost` for estimated remaining distance,
- `FCost` as total score,
- `Parent` so the final route can be reconstructed.

This script is not directly attached to GameObjects. It exists purely to support pathfinding logic.

## Unit and Combat System

### `Assets/Scripts/TurnLogic/UNITs/PlayerUnits/GridUnit.cs`

`GridUnit` is the central script for all combat units, both player and enemy.

It contains most of the gameplay state for a unit:

- team,
- health,
- damage,
- attack range,
- movement points,
- movement animation,
- attack permissions,
- current tile,
- death behavior.

This is arguably the most important script in the project.

### Main responsibilities of `GridUnit`

- Spawn a world-space health bar.
- Snap the unit onto a tile.
- Move the unit along a path using a coroutine.
- Face the target before attacking.
- Play an attack effect.
- Apply and receive damage.
- Remove itself from the battle when dead.
- Track whether it has already moved or attacked this turn.

### Placement and occupancy

- `PlaceOnTile(GridTile tile)`
  - Clears the old tile occupant if needed.
  - Assigns the new tile as `currentTile`.
  - Marks the new tile as occupied.
  - Positions the unit above the tile surface.

This method is used by both player and enemy spawners.

### Movement

- `MoveAlongPath(List<GridTile> path)`
  - Prevents null or duplicate movement starts.
  - Sets `TurnManager` to `Busy`.
  - Starts the movement coroutine.

- `MoveRoutine(List<GridTile> path)`
  - Clears occupancy on the old tile before moving.
  - Walks from tile to tile.
  - Smoothly rotates the visual model toward movement direction.
  - Updates `currentTile` at each step.
  - Marks the new tile as occupied at the end.
  - If this is a player unit, returns turn control from `Busy` back to `PlayerTurn`.
  - Fires `OnMovementFinished`.

The event `OnMovementFinished` is important because:

- `TurnManager` waits for enemy movement to finish before restoring player turn.
- `EnemyController` uses it to perform an attack after moving.

### Combat

- `CanAttack(GridUnit target)`
  - Rejects null targets.
  - Rejects attacking self.
  - Rejects same-team targets.
  - Requires the target to be in range.

- `IsTargetInRange(GridUnit target)`
  - Uses Manhattan distance between tile coordinates.

- `Attack(GridUnit target)`
  - Turns to face the target.
  - Spawns the attack effect.
  - Applies damage to the target.

### Health and death

- `TakeDamage(int amount)`
  - Reduces HP.
  - Clamps HP to 0 minimum.
  - Calls `Die()` when HP reaches 0.

- `Die()`
  - Clears tile occupancy.
  - Deactivates the GameObject.
  - Notifies `BattleStateManager`.
  - Destroys the GameObject.

### Turn-state tracking

The unit tracks:

- `hasMovedThisTurn`
- `hasAttackedThisTurn`

Methods related to that:

- `CanMoveThisTurn()`
- `CanAttackThisTurn()`
- `MarkMovedThisTurn()`
- `MarkAttackedThisTurn()`
- `ResetTurnState()`

These methods do not directly enforce all gameplay by themselves. Instead, they are checked by controllers such as `TileSelector` and `EnemyController`.

### Turn rule dependency

`GridUnit` depends on `UnitTurnRulesData`.

That means different unit types can be configured to:

- attack after moving,
- move after attacking,
- auto-deselect when out of actions.

This is a flexible design because the rules are data-driven instead of hard-coded in the unit script.

### `Assets/Scripts/TurnLogic/UNITs/PlayerUnits/UnitSpawner.cs`

`UnitSpawner` creates the player unit at scene start.

Its main jobs are:

- Read the configured spawn coordinate.
- Ask `GridManager` for the tile at that location.
- Validate that the tile exists, is walkable, and is not occupied.
- Instantiate the player prefab.
- Place the unit on the tile.

It also exposes:

- `SpawnedUnit`

This is important because `TileSelector` relies on `UnitSpawner.SpawnedUnit` to know which player unit the user can control.

At the current project stage, the system appears to support one player-controlled unit rather than a full squad.

### `Assets/Scripts/TurnLogic/UNITs/Enemies/EnemySpawner.cs`

`EnemySpawner` performs the same role for the enemy side.

Its main jobs are:

- Pick the enemy spawn tile.
- Validate it.
- Instantiate the enemy prefab.
- Place the enemy on the tile.

It exposes:

- `SpawnedEnemy`

This reference is later used by `TurnManager` during enemy turns.

### `Assets/Scripts/TurnLogic/UNITs/UnitTeam.cs`

This enum identifies which side a unit belongs to:

- `Player`
- `Enemy`

It is used across the codebase for attack validation, battle victory/loss checks, resetting unit turn state by team, and deciding whether player input should control the unit.

### `Assets/Scripts/TurnLogic/UNITs/UnitTurnRulesData.cs`

`UnitTurnRulesData` is a `ScriptableObject` that defines action-order rules for a unit.

Its configurable rules are:

- `canAttackAfterMoving`
- `canMoveAfterAttacking`
- `autoDeselectWhenOutOfActions`

This allows player units and enemy units to behave differently without duplicating movement/attack code.

Resources folder evidence suggests there are at least:

- `Assets/Resources/Rules/PlayerTurnRules.asset`
- `Assets/Resources/Rules/EnemyTurnRules.asset`

## Turn Management and Battle State

### `Assets/Scripts/TurnLogic/TurnState.cs`

This enum defines the overall battle phase:

- `PlayerTurn`
- `EnemyTurn`
- `Busy`

`Busy` is used as a temporary lock while actions are being animated or processed.

### `Assets/Scripts/TurnLogic/TurnManager.cs`

`TurnManager` controls turn order and the overall battle phase.

Its main jobs are:

- Start in player turn.
- Listen for the end-turn input.
- Reset unit action flags at the beginning of each side’s turn.
- Launch enemy AI turns.
- Display current turn text.
- Temporarily lock interaction during movement or AI processing.

### Important flow inside `TurnManager`

#### `Awake()`

- Creates the input action wrapper.
- Sets the initial turn state to `PlayerTurn`.
- Refreshes the UI text.

#### `OnEnable()` / `OnDisable()`

- Subscribes and unsubscribes from the `EndTurn` action.

#### `OnEndTurnPressed()`

The end-turn key is ignored if:

- the battle is already over,
- the state is `Busy`.

Otherwise it calls `EndTurn()`.

#### `StartPlayerTurn()`

- Sets the state to `PlayerTurn`.
- Updates turn UI.
- Finds all `GridUnit` objects.
- Resets turn-state flags for player units only.

#### `StartEnemyTurn()`

- Clears tile selection/highlights through `TileSelector`.
- Sets the state to `EnemyTurn`.
- Updates turn UI.
- Resets turn-state flags for enemy units only.
- Starts `RunEnemyTurnRoutine()`.

#### `SetBusy()`

This is called when a movement action begins. It prevents user input while the move animation is happening.

#### `ReturnToPlayerControl()`

This is called by `GridUnit` after a player unit finishes moving. It changes the state back from `Busy` to `PlayerTurn`, allowing the player to continue acting.

### Enemy turn routine

`RunEnemyTurnRoutine()` is the key enemy-turn sequence:

1. Set the global state to `Busy`.
2. Wait for a short delay.
3. Get the player unit from `UnitSpawner`.
4. Get the enemy unit from `EnemySpawner`.
5. Get `EnemyController` from the enemy object.
6. Ask the enemy to `TryAct(player)`.

Then:

- If the enemy cannot act, return to player turn.
- If the enemy attacked without moving, wait briefly and return to player turn.
- If the enemy moved, wait for the movement finished event and then return to player turn.

This is a simple but effective control structure for one enemy and one player unit.

### `Assets/Scripts/TurnLogic/BattleStateManager.cs`

`BattleStateManager` decides when the battle ends and manages the result UI.

Its main jobs are:

- Show win or lose text.
- Show the restart button.
- Reload the scene when restart is pressed.
- Prevent repeated battle-end processing.

How battle end is detected:

- A `GridUnit` calls `BattleStateManager.NotifyUnitDied(this)` when it dies.
- `BattleStateManager` waits one frame, then checks all active `GridUnit` objects in the scene.
- It looks for any surviving player units and any surviving enemy units.
- If no enemies remain:
  - result is `You Win`
- If no players remain:
  - result is `You Lose`

Why the one-frame delay matters:

- The unit object is destroyed after death.
- Waiting one frame helps the manager check the updated scene state after removal.

Connection to the rest of the project:

- `TileSelector` is told to clear highlights when the battle ends.
- `TurnManager` checks `BattleEnded` to avoid allowing more turns after victory or defeat.

## Player Interaction Layer

### `Assets/Scripts/Grid&Tiles/TileSelector.cs`

`TileSelector` is the main player interaction script. It translates mouse/touch input into tactics-game actions.

This script does a lot of coordination work:

- raycasts from the camera onto tiles,
- tracks which tile the cursor is hovering over,
- selects and deselects the player unit,
- asks `GridRangeFinder` for movement options,
- asks `AStarPathFinder` for preview and final paths,
- handles player attacks,
- triggers movement,
- manages all tile highlight states.

In practice, this is the script that makes the tactics game feel interactive.

### Input handling

`TileSelector` subscribes to:

- `PointerPosition`
- `Click`

It stores the pointer position and uses it every frame to raycast into the grid.

### Hover system

In `Update()`, it calls `HandleTileHover()`.

That method:

- raycasts from the screen to the grid,
- detects which tile is currently under the cursor,
- restores the previous tile’s visuals if needed,
- applies a hover color to the new tile,
- if a unit is selected and the hovered tile is reachable:
  - it shows a preview path from the unit’s current tile to the hovered tile.

This gives the player immediate movement feedback before committing.

### Selection logic

The script currently assumes one player-controlled unit:

- It gets the unit from `unitSpawner.SpawnedUnit`.
- The player can only select that unit by clicking its current tile.

When a unit is selected:

- its tile is marked with `startColor`,
- all reachable tiles are shown with `reachableColor`.

When a unit is deselected:

- start, target, preview, current path, and movement range highlights are cleared.

### Attack logic

If the player clicks an occupied tile:

1. `TileSelector` gets the `GridUnit` on that tile.
2. It checks whether the selected unit can attack it.
3. If yes:
   - the unit attacks,
   - `hasAttackedThisTurn` is marked,
   - if the unit can still move, selection visuals are restored,
   - otherwise the unit is deselected.

This creates a flexible action order where the rules asset can determine whether attacking ends the unit’s activity.

### Movement logic

If the player clicks an unoccupied reachable tile:

1. `TileSelector` validates the tile is in `reachableTiles`.
2. It asks `AStarPathFinder` for a path.
3. It updates final path visuals.
4. It marks the unit as having moved.
5. It calls `MoveAlongPath()`.
6. It deselects the unit.

Important detail:

- `MoveAlongPath()` sets the turn state to `Busy`.
- When the move finishes, `GridUnit` tells `TurnManager` to restore player control.

This means movement is a controlled temporary animation state, not an instant teleport.

### Highlight system

`TileSelector` maintains several overlapping highlight concepts:

- `hoverColor`
  - for the tile under the pointer,
- `reachableColor`
  - for all legal destinations,
- `previewPathColor`
  - for the path currently being previewed by hovering,
- `finalPathColor`
  - for the path chosen by clicking,
- `startColor`
  - for the selected unit’s tile,
- `targetColor`
  - for the hovered or chosen destination tile.

To manage this cleanly, the script includes helper methods such as:

- `RestoreTileVisualState()`
- `ClearPreviewPath()`
- `ClearPathPreview()`
- `ClearReachableTiles()`
- `IsTileInPersistentState()`

These prevent highlights from flickering or being erased incorrectly.

### Important design limitation

Although the script architecture could be expanded, the current implementation is effectively a single-unit player controller because it always uses `unitSpawner.SpawnedUnit`.

## Enemy AI

### `Assets/Scripts/TurnLogic/UNITs/Enemies/EnemyController.cs`

`EnemyController` contains the enemy decision-making logic.

Its main jobs are:

- Decide whether to attack immediately.
- If not, move toward the player.
- Optionally attack after moving if the rules allow it.

### Core method: `TryAct(GridUnit playerUnit)`

This is the main AI entry point called by `TurnManager`.

The sequence is:

1. Reset internal state for this action.
2. Validate references.
3. If the enemy is already moving, abort.
4. If the player is in attack range:
   - attack immediately,
   - mark attack used,
   - finish.
5. If the enemy cannot move, abort.
6. Find the best adjacent tile near the player.
7. Ask `AStarPathFinder` for a path to that tile.
8. Trim the path to respect `maxTilesToMovePerTurn`.
9. If turn rules allow attacking after moving:
   - store the player as `pendingAttackTarget`.
10. Subscribe to the movement finished event.
11. Mark movement used.
12. Begin moving.

### `GetClosestAdjacentTile(GridUnit playerUnit)`

This method does not pathfind directly to the player’s tile. Instead, it finds the best walkable unoccupied tile adjacent to the player.

That is important because:

- units cannot occupy the same tile,
- melee attacks happen from a neighboring tile,
- the enemy needs a valid stopping point next to the player.

### `TrimPath(List<GridTile> fullPath, int maxSteps)`

The full A* path might be longer than the enemy can move in one turn. This method shortens the path so the enemy only travels a limited number of tiles.

### `HandleMovementFinished(GridUnit unit)`

This is the event callback after the enemy finishes moving.

If there is a pending attack target and the enemy is now in range, then the enemy attacks and marks that attack as used.

This is the key link that makes attack-after-move work.

### AI complexity level

The enemy AI is simple and deterministic. It behaves like a basic chase AI:

- attack if possible,
- otherwise move closer,
- then attack if rules allow and range is reached.

This is appropriate for a prototype and easy to explain, debug, and extend.

## Debug and Authoring Tools

### `Assets/Scripts/DebugModeSwitcher.cs`

This script switches between two debug editing modes at runtime:

- `Terrain`
- `Obstacle`

Pressing `R` toggles between them.

Its main jobs are:

- enable one debug painter,
- disable the other,
- display simple debug mode UI.

This script is mainly for level editing and testing inside play mode.

### `Assets/Scripts/Grid&Tiles/GridDebugPainter.cs`

`GridDebugPainter` is a runtime terrain painting tool.

Its main jobs are:

- load all `TerrainTypeData` assets from `Resources/TerrainTypes`,
- let the user select a terrain type using number keys, Q/E, or mouse wheel,
- paint the clicked tile with the selected terrain,
- display a UI box showing the current selection.

How painting works:

1. Right click on a tile.
2. The tile’s `TerrainType` is set to the selected terrain.
3. `ApplyTerrainSettings()` is called on that tile.
4. The tile updates walkability, costs, visuals, and decorations.

This makes terrain editing immediate and data-driven.

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleManager.cs`

`ObstacleManager` manages obstacle placement and removal on the grid.

Its main jobs are:

- validate whether an obstacle footprint can fit,
- mark the occupied tiles as blocked if needed,
- optionally paint terrain underneath the obstacle,
- instantiate the obstacle model,
- remove obstacles and restore tiles later.

The design supports multi-tile obstacles because `ObstacleData` includes a `footprintSize`.

Important internal structures:

- `placedObstacles`
  - list of all current obstacle instances,
- `tileToObstacleMap`
  - lets the system quickly find which obstacle occupies a clicked tile.

### Placement flow

`TryPlaceObstacle()`:

1. Check references.
2. Validate placement with `CanPlaceObstacle()`.
3. Create a `PlacedObstacle` record.
4. Loop over every tile in the footprint.
5. Store those tiles in the record.
6. Optionally repaint the tile terrain.
7. Optionally force the tile to become non-walkable.
8. Instantiate the obstacle prefab.
9. Add the obstacle to manager collections.

### Removal flow

`TryRemoveObstacleAtTile()`:

1. Convert the clicked tile position to a `GridTile`.
2. Look up whether an obstacle owns that tile.
3. If yes, remove the whole obstacle.

`RemoveObstacle()`:

- removes the tile-to-obstacle mapping,
- restores walkability,
- resets terrain back to `Ground`,
- destroys the placed obstacle instance.

This is a practical runtime level-editing system.

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleData.cs`

`ObstacleData` is a `ScriptableObject` that defines an obstacle type.

Important fields:

- `obstacleId`
- `obstaclePrefab`
- `visualOffset`
- `visualRotationEuler`
- `visualScale`
- `blocksMovement`
- `footprintSize`
- `paintTerrainUnderObstacle`
- `terrainTypeUnderObstacle`

This makes obstacles configurable without changing code.

### `Assets/Scripts/Grid&Tiles/Obstacles/PlacedObstacle.cs`

This is a lightweight data class that stores one placed obstacle instance:

- which `ObstacleData` it came from,
- the spawned prefab instance,
- the origin tile,
- the list of occupied tiles.

It is not a MonoBehaviour. It is simply a data container used by `ObstacleManager`.

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleDebugPainter.cs`

`ObstacleDebugPainter` is the obstacle equivalent of the terrain painter.

Its main jobs are:

- load `ObstacleData` assets from `Resources/ObstacleTypes`,
- let the user choose an obstacle type,
- place the selected obstacle on right click,
- erase an obstacle on middle click,
- show simple UI explaining the selected obstacle and controls.

This script depends on `ObstacleManager` to actually validate placement and modify the grid.

## UI and Visual Support

### `Assets/Scripts/UI/WorldHealthBar.cs`

`WorldHealthBar` shows a floating health bar above a unit.

Its main jobs are:

- hold a reference to a target `GridUnit`,
- face the camera every frame,
- update the fill image and HP text according to the unit’s current health.

Health display details:

- `fillAmount` reflects HP ratio,
- `healthGradient` changes color based on remaining health,
- the label shows `HP: current/max`.

How it is connected:

- `GridUnit.Start()` instantiates the health bar prefab and calls `Initialize(this)`.

### `Assets/Scripts/UI/BillBoard.cs`

`Billboard` is a simpler helper that only rotates an object to face the main camera.

This can be used on world-space UI or visual elements that should always face the player’s view.

### `Assets/Scripts/UI/Effects/AttackEffectData.cs`

`AttackEffectData` is a `ScriptableObject` that describes a unit’s attack effect.

Important fields:

- `effectPrefab`
- `positionOffset`
- `effectScale`
- `duration`
- `popInDuration`
- `riseAmount`

How it is used:

- `GridUnit.Attack()` calls `ShowAttackEffect()`.
- That method instantiates the effect prefab.
- `AnimateAttackEffect()` makes it scale in and rise upward before being destroyed.

This separates combat feedback data from unit logic.

## Generated and Non-Gameplay Utility Scripts

### `Assets/TutorialInfo/Scripts/Readme.cs`
### `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`

These are tutorial or template support files and are not part of the battle logic.

They can usually be ignored when analyzing gameplay systems.

## Data Assets and Resource Folders

The code shows a strong data-driven approach. Several systems rely on Unity `Resources` folders:

- `Assets/Resources/TerrainTypes`
  - loaded by `TileManager` and `GridDebugPainter`
- `Assets/Resources/ObstacleTypes`
  - loaded by `ObstacleDebugPainter`
- `Assets/Resources/Rules`
  - contains turn-rule assets used by units
- `Assets/Resources/AttkEffectData`
  - contains attack effect assets used by units

Observed examples in the repository include:

- Terrain assets:
  - `Basic`
  - `Blocked`
  - `Grass`
  - `Hazard`
  - `Water`
- Rule assets:
  - `PlayerTurnRules`
  - `EnemyTurnRules`
- Obstacle assets:
  - `RockBig_2x2`
- Attack effects:
  - `PlayerAttkEffect`
  - `EnemyAttackEffect`

This design is good for iteration because designers can tweak data without rewriting scripts.

## Prefabs and Scene Objects Expected by the Code

The scripts imply several required scene references and prefabs:

- Grid prefab:
  - `Assets/Prefabs/Tiles/Tile.prefab`
- Player unit prefab:
  - `Assets/Prefabs/PlayerUnits/AmongusUnit.prefab`
- Enemy unit prefab:
  - `Assets/Prefabs/EnemyUnits/AmongusUnit 1.prefab`
- Health bar UI prefab:
  - `Assets/Prefabs/UI/HealthBarCanvas.prefab`
- Weapon or effect visuals:
  - assets under `Assets/Prefabs/UI/Weapons`

The scene likely contains manager GameObjects with references wired in the Inspector, including:

- `GridManager`
- `TileManager`
- `AStarPathFinder`
- `GridRangeFinder`
- `TileSelector`
- `TurnManager`
- `BattleStateManager`
- `UnitSpawner`
- `EnemySpawner`
- `DebugModeSwitcher`
- `ObstacleManager`

Because many scripts use `[SerializeField]`, correct Inspector setup is essential for the project to run.

## Detailed Interaction Examples

## Example 1: Player movement

1. The player clicks the tile containing their unit.
2. `TileSelector` selects the unit.
3. `GridRangeFinder` calculates reachable tiles.
4. Those tiles are highlighted.
5. The player hovers a destination tile.
6. `AStarPathFinder` calculates the preview route.
7. The preview path is highlighted.
8. The player clicks the destination.
9. `TileSelector` checks reachability and requests the final path.
10. `GridUnit.MarkMovedThisTurn()` is called.
11. `GridUnit.MoveAlongPath()` begins movement.
12. `TurnManager.SetBusy()` blocks further input during animation.
13. The unit animates step-by-step to the destination.
14. The new tile becomes occupied.
15. `GridUnit` calls `TurnManager.ReturnToPlayerControl()`.
16. The player can continue acting if rules allow.

## Example 2: Player attack

1. The player selects the unit.
2. The player clicks an enemy tile.
3. `TileSelector` gets the enemy `GridUnit`.
4. `selectedUnit.CanAttack(targetUnit)` is checked.
5. If valid:
   - the unit rotates toward the enemy,
   - the attack effect spawns,
   - damage is applied,
   - the attack is marked as used.
6. If the target reaches 0 HP:
   - `Die()` clears occupancy,
   - `BattleStateManager` is notified,
   - the GameObject is destroyed.

## Example 3: Enemy turn

1. The player presses Space.
2. `TurnManager.EndTurn()` switches to enemy turn.
3. Enemy action flags are reset.
4. `RunEnemyTurnRoutine()` starts.
5. The enemy controller gets the player reference.
6. If the player is already in range:
   - attack immediately.
7. Otherwise:
   - pick a tile adjacent to the player,
   - find a path,
   - trim the path to the enemy movement limit,
   - start moving.
8. When movement ends:
   - if allowed and now in range, attack.
9. `TurnManager` returns control to the player.

## Example 4: Battle end

1. A unit dies.
2. `GridUnit.Die()` calls `BattleStateManager.NotifyUnitDied(this)`.
3. On the next frame, `BattleStateManager.CheckBattleState()` runs.
4. It scans all active units.
5. If one team has no units left:
   - the battle is marked ended,
   - selection highlights are cleared,
   - result text is shown,
   - restart button appears.

## Important Design Strengths

The project already shows several good software design choices:

- Clear separation between systems:
  - grid,
  - pathfinding,
  - units,
  - turns,
  - AI,
  - debug tooling.
- Heavy use of `ScriptableObject` data:
  - terrain,
  - turn rules,
  - attack effects,
  - obstacles.
- Reusable `GridUnit` class for both player and enemy.
- Event-based movement completion with `OnMovementFinished`.
- Clean use of a `Busy` turn state to avoid input conflicts.
- Debug painters that support quick iteration in play mode.

## Important Limitations and Incomplete Features

From the code, there are also clear signs that the prototype is still in progress.

### Single-unit player control

The player interaction system only directly supports the single spawned unit from `UnitSpawner`.

### Single-enemy turn routine

`TurnManager` currently fetches one player unit and one enemy unit from the spawners, so the enemy phase is built around a one-vs-one structure rather than a full multi-unit tactics battle.

### Terrain effects are only partly implemented

`TerrainTypeData` defines:

- damage on enter,
- damage on stop,
- movement penalty on stop,

but these values are not yet applied by `GridUnit` movement or turn resolution logic.

### `HandlePostActionSelection()` is currently unused

`TileSelector` contains a helper named `HandlePostActionSelection()`, but the current click flow does not call it. That suggests the selection logic was being refactored or planned for extension.

### `autoDeselectWhenOutOfActions` is not meaningfully differentiated yet

The code checks the rule, but both branches currently call `RestoreSelectionVisuals()`, so the behavior is effectively the same either way.

### Possible naming and maintainability issues

- `widht` in `GridManager` is misspelled.
- `AttkEffectData` folder name is inconsistent with `AttackEffectData`.

These are not gameplay bugs by themselves, but they make the project slightly less polished and harder to maintain.

## How Everything Connects

The simplest way to understand the whole project is as a chain of responsibility:

1. `TileManager` defines what tiles mean.
2. `GridManager` creates the board out of tiles.
3. `GridTile` stores the state of each board cell.
4. `UnitSpawner` and `EnemySpawner` place combatants on those cells.
5. `TileSelector` lets the player choose actions on the board.
6. `GridRangeFinder` tells the player where movement is legal.
7. `AStarPathFinder` tells units how to travel.
8. `GridUnit` performs movement, attacks, damage, and death.
9. `TurnManager` decides whose turn it is and when control changes.
10. `EnemyController` chooses the enemy action during the enemy turn.
11. `BattleStateManager` decides when the match is over.
12. `WorldHealthBar`, attack effects, and tile highlights communicate state visually to the player.

That means the project is not a random collection of scripts. It is a connected turn-based tactics framework with:

- a generated board,
- data-driven terrain,
- basic AI,
- animated movement,
- melee combat,
- runtime debug editing tools.

## Recommended Reading Order for New Team Members

If someone new joins the project, the best reading order is:

1. `GridUnit.cs`
2. `TileSelector.cs`
3. `TurnManager.cs`
4. `EnemyController.cs`
5. `GridManager.cs`
6. `GridTile.cs`
7. `AStarPathFinder.cs`
8. `GridRangeFinder.cs`
9. `BattleStateManager.cs`
10. the `ScriptableObject` data classes

This order helps a new developer understand the game from player actions inward, then move outward into supporting systems.

## Final Summary

This project is a turn-based grid combat prototype built around one main battle scene. The board is generated at runtime, units are spawned onto tiles, the player uses a tile-selection system to move and attack, the enemy uses a simple chase-and-attack AI, and the battle ends when one team has no units left.

The core logic is spread across a small number of focused systems:

- `GridManager` and `GridTile` define the board.
- `TileManager` and terrain assets define terrain behavior.
- `AStarPathFinder` and `GridRangeFinder` control legal movement.
- `GridUnit` represents all combatants.
- `TileSelector` handles player-side interaction.
- `TurnManager` controls turn flow.
- `EnemyController` handles enemy actions.
- `BattleStateManager` ends the match and updates the UI.

The project is already structured well for a prototype and is especially strong in its use of reusable data assets and modular systems. Its main current limitation is that the battle flow is still built around a very small-scale encounter rather than a full multi-unit tactics game.
