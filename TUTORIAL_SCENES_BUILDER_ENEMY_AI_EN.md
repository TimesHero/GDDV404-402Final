# Simple Tutorial: Scenes, Level Builder, and Enemy AI

## What This Document Is

This is a short and simple guide for the team. It explains:

- what each scene is for,
- how the Level Builder works,
- how levels are saved and loaded,
- what the `Resources` folders are for,
- how the enemy AI works.

For deep technical details, use `COMPLETE_PROJECT_DOCUMENTATION_EN.md`.

---

# 1. Main Scene Flow

```text
MainMenu
    The player chooses where to go.

LevelPicker
    The player chooses a saved level.

BattleScene
    The selected level is loaded and played.

LevelBuilderScene
    The team can create or edit levels.

IAP Scene
    Store, gems, ads, skins, and analytics.
```

Simple flow:

```pseudocode
MainMenu -> LevelPicker -> BattleScene

or

MainMenu -> LevelBuilderScene -> save level -> LevelPicker -> BattleScene
```

---

# 2. MainMenu

## Purpose

The main menu is just the starting point. It sends the player to other scenes.

## Important Scripts

```text
MainMenuManager
    Main menu manager.

SceneNavigationButton
    Loads another scene when a button is clicked.

MenuCameraController
    Controls the menu camera.

MenuAmbience
    Controls menu ambience/audio.

SettingsManager
    Placeholder for settings.
```

---

# 3. LevelPicker

## Purpose

The Level Picker shows saved levels and lets the player choose one.

Saved levels are stored here:

```text
Assets/Resources/LevelLayouts
```

## Important Scripts

```text
LevelPickerUIController
    Finds saved JSON levels and creates buttons for them.

SelectedBattleLevel
    Remembers which level was selected.
```

## Simple Logic

```pseudocode
LevelPicker opens
    find all JSON levels in Resources/LevelLayouts
    create a button for each level

Player clicks a level
    save level name in SelectedBattleLevel
    load BattleScene
```

---

# 4. BattleScene

## Purpose

BattleScene is where the tactical game is played.

It loads the selected level, builds the grid, places all objects, then starts the turn-based battle.

## Important Scripts

```text
BattleSceneLevelLoader
    Loads the selected JSON level.

GridManager
    Creates the grid.

GridTile
    One tile on the grid.

TurnManager
    Controls player turn, enemy turn, and busy state.

TileSelector
    Handles player clicks and actions.

GridUnit
    Controls unit movement, attacks, health, death, and push.

EnemyController
    Controls enemy decisions.

EnemyVisionDetector
    Checks what enemies can see.

BattleStateManager
    Handles win/loss.

LevelObjectiveRuntimeManager
    Checks level objectives.
```

## Simple Battle Flow

```pseudocode
BattleScene starts
    BattleSceneLevelLoader loads selected JSON
    GridManager creates the grid
    terrain, elevation, obstacles, interactables, units, and objectives are placed
    TurnManager starts PlayerTurn

Player turn
    player selects unit
    player chooses Move, Attack, Push, or barrel interaction
    player ends turn

Enemy turn
    enemies look for players
    enemies move, attack, patrol, investigate, or search barrels
    turn returns to player

Battle ends
    if objectives are complete -> win
    if all players die -> lose
    if loseWhenSeen is active and player is seen -> lose
```

---

# 5. LevelBuilderScene

## Purpose

The Level Builder is where we create and edit levels.

It lets us:

```text
paint terrain
change elevation
place obstacles
place interactables
place player units
place enemy units
set enemy behavior
create objectives
erase things
save/load JSON levels
```

## Important Scripts

```text
BuilderStateController
    Stores the selected tool and selected asset.

BuilderInputController
    Applies the selected tool when the player clicks the grid.

BuilderUIController
    Connects UI buttons/sliders/dropdowns to builder logic.

BuilderCameraController
    Controls the builder camera.

BuilderSaveLoadManager
    Saves and loads JSON levels.

UnitPlacementService
    Places units.

BuilderUnitRegistry
    Tracks placed units so they can be saved.

ObstacleManager
    Places obstacles.

InteractablePlacementService
    Places interactables.

BuilderObjectiveUIController
    Creates objectives.

LevelObjectiveRegistry
    Stores objectives before saving.
```

---

# 6. Builder Tools

The builder has these main tools:

```text
TerrainPaint
ElevationPaint
ObstaclePaint
InteractablePaint
UnitPaint
Erase
```

## Terrain Paint

Use this to paint tiles with terrain types.

Example terrain types:

```text
Basic
Blocked
Grass
Hazard
Water
```

Simple logic:

```pseudocode
select TerrainPaint
choose terrain type
click tile
tile changes terrain
```

## Elevation Paint

Use this to change tile height.

Simple logic:

```pseudocode
select ElevationPaint
choose height value
click tile
tile height changes
```

Why it matters:

```text
Units can only climb if the height difference is allowed by their UnitData.
```

## Obstacle Paint

Use this to place rocks, walls, or blockers.

Example obstacle assets:

```text
RockBig_2x2
StoneWall_5x2
```

Simple logic:

```pseudocode
select ObstaclePaint
choose obstacle
rotate if needed
click valid tile
obstacle is placed
```

Obstacles cannot be placed if:

```text
outside the grid
on occupied tiles
on invalid terrain
on uneven elevation
on another obstacle
```

## Interactable Paint

Use this to place objects the player can interact with, like barrels.

Example:

```text
Barril
```

Simple logic:

```pseudocode
select InteractablePaint
choose interactable
click tile
interactable is placed
```

## Unit Paint

Use this to place player or enemy units.

Simple logic:

```pseudocode
select UnitPaint
choose Player or Enemy
choose unit type
choose rotation
click tile
unit is placed
```

Player unit data is here:

```text
Assets/Resources/UnitData/Player
```

Enemy unit data is here:

```text
Assets/Resources/UnitData/Enemy
```

## Enemy Setup in Builder

When placing an enemy, choose one behavior:

```text
Static
    Enemy stays at its position unless it detects something.

RandomLook
    Enemy stays mostly in place but looks around.

Patrol
    Enemy moves between two points.
```

Patrol setup:

```pseudocode
select enemy with Patrol behavior
click first tile to place enemy
click second tile to set patrol end
enemy will patrol between those two tiles
```

## Erase

Use this to remove things from the level.

```pseudocode
select Erase
click tile
remove unit, obstacle, interactable, or marker
```

---

# 7. Objectives

Objectives tell the battle how the player wins or loses.

Objective types:

```text
KillAllEnemies
    Win when all enemies are defeated.

SurviveTurns
    Win after surviving a number of turns.

ReachTile
    Win when player units reach target tiles.

ReachWithoutBeingSeen
    Win by reaching target tiles without being seen.

InteractWithObject
    Exists as a type, but is not fully developed yet.
```

Important scripts:

```text
BuilderObjectiveUIController
    Used in the builder to create objectives.

LevelObjectiveRegistry
    Stores objectives before saving.

LevelObjectiveRuntimeManager
    Checks objectives during battle.

BattleStateManager
    Shows win/loss result.
```

---

# 8. Saving and Loading Levels

## Save

When a level is saved, the builder creates a JSON file.

Save location:

```text
Assets/Resources/LevelLayouts
```

Important script:

```text
BuilderSaveLoadManager
```

Save logic:

```pseudocode
SaveLevel
    save grid size
    save terrain
    save elevation
    save obstacles
    save interactables
    save units
    save enemy behavior/patrols
    save objectives
    write JSON file
```

## Load

BattleScene loads the JSON and rebuilds the level.

Important script:

```text
BattleSceneLevelLoader
```

Load logic:

```pseudocode
Load selected JSON
rebuild grid
apply terrain
apply elevation
place obstacles
place interactables
place units
start battle
```

---

# 9. Resources Folder

The `Resources` folder stores data that scripts can load.

Important folders:

```text
Resources/LevelLayouts
    Saved JSON levels.

Resources/TerrainTypes
    Terrain data like Basic, Grass, Water, Hazard, Blocked.

Resources/ObstacleTypes
    Obstacle data like rocks and walls.

Resources/UnitData/Player
    Player unit data.

Resources/UnitData/Enemy
    Enemy unit data.

Resources/InteractableData
    Interactable data, like barrels.

Resources/Rules
    Turn rule assets for players/enemies.

Resources/Objectives
    Objective marker data.

Resources/LogoEffects
    Attack and enemy state feedback effects.
```

Simple idea:

```text
Resources holds the data.
Scripts load the data.
The builder uses the data to create levels.
BattleScene uses saved JSON and data to rebuild the level.
```

---

# 10. Enemy AI Simple Explanation

## Main Enemy AI Scripts

```text
EnemyController
    The enemy brain.

EnemyVisionDetector
    The enemy eyes.

EnemyAIBehavior
    The enemy's default behavior.

EnemyAIState
    What the enemy is doing right now.

GridUnit
    Actually moves, attacks, takes damage, and dies.

AStarPathFinder
    Finds paths for enemy movement.

TurnManager
    Tells enemies when it is their turn.
```

## Behavior vs State

Behavior is the enemy's normal style.

```text
Static
Patrol
RandomLook
```

State is what the enemy is currently doing.

```text
Idle
Patrol
Alert
Investigate
SearchBarrels
Combat
ReturnToPost
```

## Enemy Turn Flow

```pseudocode
Enemy turn starts

for each enemy:
    check what the enemy can see
    decide current state

    if enemy sees player:
        enter Combat

    else if enemy sees suspicious barrel:
        enter SearchBarrels

    else if enemy remembers something:
        enter Investigate

    else if enemy has patrol:
        Patrol

    else:
        Idle or RandomLook
```

## Enemy State Meanings

```text
Idle
    Enemy is doing nothing important.

Patrol
    Enemy moves between patrol points.

Alert
    Enemy noticed something but does not fully know what to do yet.

Investigate
    Enemy moves toward a last known or suspicious location.

SearchBarrels
    Enemy checks barrels that may hide the player.

Combat
    Enemy attacks, pushes, or moves toward the player.

ReturnToPost
    Enemy goes back to its original position.
```

## Combat Logic

```pseudocode
if enemy can attack player:
    attack

else if enemy can push player:
    push

else:
    find path toward player
    move closer
```

## Vision Logic

`EnemyVisionDetector` checks:

```text
distance
vision angle
line of sight
obstacles
whether the player is hidden
barrels
```

If the enemy sees the player:

```pseudocode
EnemyController remembers player
enemy enters Combat

if objective is ReachWithoutBeingSeen:
    LevelObjectiveRuntimeManager marks player as seen
```

## Barrel Logic

Players can hide in barrels.

Important scripts:

```text
BarrelInteractable
HiddenStateComponent
EnemyVisionDetector
EnemyController
```

Simple flow:

```pseudocode
player hides in barrel
HiddenStateComponent marks player as hidden

enemy sees barrel move or notices barrel changed
enemy becomes suspicious
enemy investigates or searches barrel

if enemy finds player:
    player is revealed
    enemy can enter Combat
```

---

# 11. Simple Team Workflow

## To Build a Level

```pseudocode
Open LevelBuilderScene
Paint terrain
Paint elevation
Place obstacles
Place barrels/interactables
Place player units
Place enemy units
Set enemy behavior
Create objectives
Save level
```

## To Test a Level

```pseudocode
Open LevelPicker
Select saved level
BattleScene loads
Play and test
```

## Things to Check Before Saving

```text
At least one player unit exists.
Enemies exist if the objective needs enemies.
Patrol enemies have a patrol end point.
Objectives are configured.
Obstacles are not blocking everything.
Level name is correct.
```

---

# 12. Quick Mental Model

```text
MainMenu sends players to scenes.
LevelPicker chooses saved levels.
LevelBuilderScene creates JSON levels.
BattleScene loads JSON levels.
Resources stores data.
GridManager builds the board.
GridUnit controls units.
TileSelector controls player actions.
TurnManager controls turns.
EnemyController controls enemy decisions.
EnemyVisionDetector controls enemy sight.
LevelObjectiveRuntimeManager checks objectives.
BattleStateManager ends the battle.
```

