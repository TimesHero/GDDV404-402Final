# Complete Project Documentation

## Purpose

This is the consolidated English documentation for the project. It combines the older gameplay/system documentation with the newer documentation about:

- all main scenes,
- the level builder and its tools,
- the JSON save/load pipeline,
- the `Resources` folder structure,
- the tactical battle loop,
- units, movement, combat, terrain, elevation, interactables, objectives, UI, IAP/store systems,
- enemy AI, vision, barrels, patrols, investigation, and the state machine,
- where the important scripts fit in the project logic.

The project is a Unity turn-based tactics prototype with an in-game level builder, JSON-based level loading, stealth/barrel interactions, objectives, and a store/IAP layer.

---

# 1. High-Level Project Flow

```pseudocode
Game starts

MainMenu:
    player navigates to:
        LevelPicker
        LevelBuilderScene
        IAP Scene

LevelPicker:
    lists JSON levels from Resources/LevelLayouts
    player chooses one
    SelectedBattleLevel stores the chosen level name
    BattleScene loads

BattleScene:
    BattleSceneLevelLoader reads selected JSON
    GridManager rebuilds the grid
    terrain, elevation, obstacles, interactables, objectives, and units are placed
    TurnManager starts player turn
    player and enemies alternate turns
    BattleStateManager and LevelObjectiveRuntimeManager decide win/loss

LevelBuilderScene:
    player edits a level
    BuilderSaveLoadManager saves JSON into Resources/LevelLayouts
    saved levels become available to LevelPicker and BattleScene

IAP Scene:
    store, gems, ads, analytics, skins, and purchase/reward logic
```

---

# 2. Scenes

## `Assets/Scenes/MainMenu.unity`

Main menu and navigation scene.

Important scripts:

```text
MainMenuManager
    Scene-level menu manager and expansion point.

SceneNavigationButton
    Loads another scene when clicked.

MenuCameraController
    Controls menu camera presentation.

MenuAmbience
    Controls ambience/audio in the menu.

SettingsManager
    Settings expansion point.
```

Logic:

```pseudocode
MainMenu starts:
    menu camera and ambience run

Player clicks navigation button:
    SceneNavigationButton.LoadScene(sceneName)
```

## `Assets/Scenes/LevelPicker.unity`

Lets the player pick a saved level.

Important scripts:

```text
LevelPickerUIController
SelectedBattleLevel
SceneNavigationButton
```

Logic:

```pseudocode
LevelPickerUIController.Start:
    load TextAssets from Resources/LevelLayouts
    create one UI button per JSON level

On level clicked:
    SelectedBattleLevel.Set(levelName)
    load BattleScene
```

## `Assets/Scenes/BattleScene.unity`

Main tactical battle scene.

Important scripts:

```text
BattleSceneLevelLoader
GridManager
TileManager
ObstacleManager
InteractablePlacementService
TurnManager
TileSelector
BattleStateManager
LevelObjectiveRuntimeManager
EnemyController
EnemyVisionDetector
BattleCameraController
BattlePauseMenuController
```

Startup logic:

```pseudocode
BattleScene starts:
    BattleSceneLevelLoader.LoadConfiguredLevel()

LoadConfiguredLevel:
    levelFileName = SelectedBattleLevel.LevelFileName
    if empty:
        use fallbackLevelFileName

    json = Resources.Load<TextAsset>("LevelLayouts/" + levelFileName)
    layoutData = JsonUtility.FromJson<LevelLayoutData>(json.text)

    RebuildBattleLevel(layoutData)
    BattleStateManager.ResetBattleState()
    LevelObjectiveRuntimeManager.InitializeObjectives(layoutData.objectives)
    LevelObjectiveRuntimeManager.SetLoseWhenSeen(layoutData.loseWhenSeen)
    TurnManager.StartPlayerTurn()
    BattleStateManager.CheckBattleState()
```

## `Assets/Scenes/LevelBuilderScene.unity`

In-game level editor.

Important scripts:

```text
BuilderStateController
BuilderInputController
BuilderUIController
BuilderCameraController
BuilderObstaclePreview
BuilderObjectiveUIController
BuilderSaveLoadManager
LevelObjectiveRegistry
UnitPlacementService
BuilderUnitRegistry
ObstacleManager
InteractablePlacementService
GridManager
```

Builder purpose:

```text
Paint terrain.
Paint elevation.
Place obstacles.
Place interactables.
Place player/enemy units.
Configure enemy behavior and patrol routes.
Create objectives.
Save/load JSON levels.
```

## `Assets/Scenes/IAP Scene.unity`

Store, IAP, ads, gems, skins, and analytics scene.

Important scripts:

```text
ConsentGateController
AnalyticsManager
AdInitializer
AdManager
PurchaseFufillment
SkinStoreController
AssetBundleLoader
```

---

# 3. Resources Folder

Unity can load files under `Assets/Resources` by path using `Resources.Load` and `Resources.LoadAll`.

## Main Resource Paths

```text
Assets/Resources/Backgrounds
Assets/Resources/InteractableData
Assets/Resources/LevelLayouts
Assets/Resources/LevelMusic
Assets/Resources/LogoEffects
Assets/Resources/Objectives
Assets/Resources/ObstacleTypes
Assets/Resources/Rules
Assets/Resources/TerrainTypes
Assets/Resources/UnitData
```

## `Resources/LevelLayouts`

Contains saved level JSON files.

Used by:

```text
BuilderSaveLoadManager
LevelPickerUIController
BattleSceneLevelLoader
```

Flow:

```pseudocode
BuilderSaveLoadManager:
    writes level JSON here

LevelPickerUIController:
    lists files from here

BattleSceneLevelLoader:
    loads chosen JSON from here
```

## `Resources/TerrainTypes`

Contains `TerrainTypeData` assets:

```text
Basic.asset
Blocked.asset
Grass.asset
Hazard.asset
Water.asset
```

Used by:

```text
TileManager
GridTile
BuilderStateController
BuilderInputController
BattleSceneLevelLoader
```

## `Resources/ObstacleTypes`

Contains `ObstacleData` assets:

```text
RockBig_2x2.asset
StoneWall_5x2.asset
```

Used by:

```text
BuilderStateController
ObstacleManager
BuilderSaveLoadManager
BattleSceneLevelLoader
```

## `Resources/UnitData`

Player units:

```text
Resources/UnitData/Player/Player_SUS.asset
Resources/UnitData/Player/test.asset
```

Enemy units:

```text
Resources/UnitData/Enemy/Enemy_Grunt.asset
Resources/UnitData/Enemy/Enemy_Grunt 1.asset
Resources/UnitData/Enemy/Pachale.asset
```

Used by:

```text
BuilderStateController
UnitPlacementService
BuilderSaveLoadManager
BattleSceneLevelLoader
GridUnit
EnemyController
EnemyVisionDetector
```

## `Resources/InteractableData`

Contains interactable data such as barrels:

```text
Barril.asset
Barril 1.asset
```

Used by:

```text
BuilderStateController
InteractableLibrary
InteractablePlacementService
BattleSceneLevelLoader
BuilderSaveLoadManager
```

## `Resources/Rules`

Contains turn rule assets:

```text
EnemyTurnRules.asset
PlayerTurnRules.asset
```

Used by:

```text
UnitTurnRulesData
GridUnit
```

## `Resources/Objectives`

Contains objective visual data:

```text
DefaultReachTileMarkerData.asset
```

Used by:

```text
LevelObjectiveRuntimeManager
ReachTileMarkerData
```

## `Resources/LogoEffects`

Contains visual feedback/effect data:

```text
EnemyAttackEffect.asset
EnemyStateFeedback_Default.asset
PlayerAttkEffect.asset
```

Used by:

```text
AttackEffectData
EnemyStateFeedbackData
GridUnit
EnemyStateFeedbackController
```

---

# 4. Grid, Terrain, Elevation, and Obstacles

## Core Scripts

```text
GridManager
GridTile
TileManager
TerrainTypeData
TileElevation
GridRangeFinder
AStarPathFinder
PathNode
ObstacleManager
ObstacleData
PlacedObstacle
PlaceableAnchor
```

## Grid Creation

```pseudocode
GridManager.Awake:
    GenerateGrid()

GenerateGrid:
    create GridTile[width, height]

    for x in width:
        for y in height:
            worldPosition = (x * tileSpacing, 0, y * tileSpacing)
            tile = Instantiate(tilePrefab, worldPosition)
            tile.Initialize(x, y, tileManager)
            grid[x, y] = tile
```

## Tile Logic

```pseudocode
GridTile.Initialize(x, y, tileManager):
    X = x
    Y = y
    store TileManager
    ApplyTerrainSettings()

GridTile.ApplyTerrainSettings:
    data = TileManager.GetTerrainData(TerrainType)
    CurrentTerrainData = data
    isWalkable = data.IsWalkable
    movementCost = data.MovementCost
    apply top/side materials, color, decoration

GridTile.SetOccupant(unit):
    OccupyingUnit = unit
    isOccupied = unit != null
```

## Terrain Data

`TerrainTypeData` defines:

```text
TerrainType
movement cost
walkable/not walkable
damage on enter
damage on stop
movement penalties
top material
side material
decoration prefab
color
```

## Elevation

```pseudocode
TileElevation.SetElevation(value):
    elevation = max(0, value)
    ApplyElevation()

ApplyElevation:
    move top surface
    scale side column
    move overlay
    update collider
    move decoration anchor
```

Gameplay effect:

```pseudocode
Pathfinding checks:
    heightDifference = abs(currentTile.elevation - nextTile.elevation)
    if heightDifference > unit.MaxClimbHeight:
        movement is invalid
```

## Obstacles

```pseudocode
ObstacleManager.TryPlaceObstacle(obstacleData, origin, rotation):
    if CanPlaceObstacle(...) is false:
        return false

    create PlacedObstacle
    calculate rotated footprint

    for each footprint tile:
        register obstacle occupancy
        if obstacle paints terrain:
            tile.TerrainType = obstacleData.TerrainTypeUnderObstacle
            tile.ApplyTerrainSettings()
        if obstacle blocks movement:
            tile.ForceSetWalkable(false)

    instantiate obstacle prefab
    return true
```

Placement validation:

```pseudocode
CanPlaceObstacle:
    reject outside grid
    reject occupied tiles
    reject non-walkable tiles
    reject existing obstacle
    reject uneven footprint elevation
```

---

# 5. Pathfinding and Movement Range

## A* Pathfinding

```pseudocode
AStarPathFinder.FindPath(startTile, targetTile, unit):
    openSet = tiles to check
    closedSet = tiles already checked

    add PathNode for startTile

    while openSet is not empty:
        current = node with lowest fCost

        if current.tile == targetTile:
            return reconstructed path

        move current to closedSet

        for each neighbor from GridManager.GetNeighbors(current.tile):
            if neighbor invalid:
                continue
            if movement blocked by walkability, occupancy, or elevation:
                continue

            update or add PathNode

    return empty path
```

## Range Finder

```pseudocode
GridRangeFinder.GetReachableTiles(start, movementPoints, unit):
    reachable[start] = 0
    frontier = queue with start

    while frontier has tiles:
        current = dequeue
        for each neighbor:
            newCost = currentCost + unit.GetMovementCostForTile(neighbor)
            if newCost <= movementPoints:
                store cheaper cost
                enqueue neighbor
```

Used by:

```text
TileSelector
EnemyController
movement previews
movement validation
```

---

# 6. Units, Combat, and Player Actions

## Core Scripts

```text
GridUnit
UnitData
UnitAbilityData
UnitTurnRulesData
UnitSpawnEntry
UnitSpawner
EnemySpawner
UnitTeam
AttackType
ElementType
UnitRole
AIType
TileSelector
UnitActionMenuController
AttackEffectData
WorldHealthBar
BillBoard
```

## UnitData

`UnitData` stores:

```text
unit id
unit name
prefab
max HP
attack power
defense
movement points
attack range
max climb height
vision range
vision angle
attack type
element type
AI type
can hide in barrel
can backstab
can push
push weight
```

## Placing Units

```pseudocode
GridUnit.PlaceOnTile(tile):
    if currentTile exists:
        currentTile.SetOccupant(null)

    currentTile = tile
    tile.SetOccupant(gameObject)
    move unit to top of tile
```

## Movement

```pseudocode
GridUnit.TryMove(path):
    if cannot move this turn:
        return false
    if path invalid:
        return false
    if destination occupied by another unit:
        return false
    if path cost > remainingMovementPoints:
        return false

    MarkMovedThisTurn()
    remainingMovementPoints -= cost
    MoveAlongPath(path)
    return true
```

## Attack

```pseudocode
GridUnit.TryAttack(target):
    if cannot attack this turn:
        return false
    if target invalid, allied, or out of range:
        return false

    face target
    show AttackEffectData visual
    damage = CalculateAttackDamage(target)
    target.TakeDamage(damage)

    if attacker was hidden:
        reveal attacker

    if player hit enemy:
        EnemyController.NotifyEnemyHitByAttacker(target, this)

    MarkAttackedThisTurn()
    return true
```

## Damage and Death

```pseudocode
GridUnit.TakeDamage(amount):
    if inside barrel and barrel absorbs hit:
        return

    if hidden:
        reveal

    currentHP -= amount

    if currentHP <= 0:
        Die()

GridUnit.Die:
    currentTile.SetOccupant(null)
    gameObject.SetActive(false)
    BattleStateManager.NotifyUnitDied(this)
```

## Player Input

```pseudocode
TileSelector.HandlePrimaryClick:
    if battle ended, paused, not PlayerTurn, or Busy:
        return

    if no selected unit:
        if clicked player unit:
            SelectUnit(unit)
            ShowUnitActionMenu()
        return

    if action mode is Move:
        calculate path
        selectedUnit.TryMove(path)

    if action mode is Attack:
        selectedUnit.TryAttack(enemy)

    if action mode is Push:
        selectedUnit.TryPush(enemy)

    if barrel interaction:
        execute BarrelInteractable logic
```

---

# 7. Turn System

## Core Scripts

```text
TurnManager
TurnState
UnitTurnSnapshot
EnemyTurnSpeedMode
```

## Turn States

```text
PlayerTurn
EnemyTurn
Busy
```

## Player Turn

```pseudocode
TurnManager.StartPlayerTurn:
    CurrentTurn = PlayerTurn
    reset player units
    apply terrain start-turn effects
    EnemyVisionDetector.RefreshAllHiddenStates()
    CapturePlayerTurnSnapshot()
    LevelObjectiveRuntimeManager.OnPlayerTurnStarted()
```

## Enemy Turn

```pseudocode
TurnManager.StartEnemyTurn:
    clear TileSelector selection/highlights
    CurrentTurn = EnemyTurn
    reset enemy units
    StartCoroutine(RunEnemyTurnRoutine())

RunEnemyTurnRoutine:
    CurrentTurn = Busy
    wait enemy delay

    for each living enemy:
        target = GetBestTargetForEnemy(enemy)
        enemy.EnemyController.TryTakeTurn(target)
        wait for movement/action completion
        refresh vision and hidden states

    StartPlayerTurn()
```

## Restart Turn

```pseudocode
CapturePlayerTurnSnapshot:
    save each unit:
        tile position
        HP
        dead/alive state
        movement/attack state
        remaining movement points
        visual rotation

RestartPlayerTurn:
    restore snapshots
    reduce restart uses
    clear selection
    recheck battle state
```

---

# 8. Level Builder

## Builder Tool Modes

```text
TerrainPaint
ObstaclePaint
InteractablePaint
UnitPaint
ElevationPaint
Erase
```

## Core Scripts

```text
BuilderStateController
BuilderInputController
BuilderUIController
BuilderCameraController
BuilderObstaclePreview
BuilderObjectiveUIController
LevelObjectiveRegistry
BuilderSaveLoadManager
UnitPlacementService
BuilderUnitRegistry
PlacedBuilderUnit
BuilderToolMode
BuilderUnitPaintTeam
```

## Builder Startup

```pseudocode
LevelBuilderScene starts:
    GridManager creates editable grid
    BuilderStateController loads Resources assets
    BuilderUIController connects buttons/sliders/dropdowns
    BuilderInputController reads pointer and click input
    BuilderCameraController controls editor camera
```

## Builder State

`BuilderStateController` stores:

```text
current tool
brush size
selected elevation
selected terrain
selected obstacle
selected interactable
selected unit
selected unit team
selected enemy behavior
selected rotations
```

## Terrain Tool

```pseudocode
Player chooses TerrainPaint
Player chooses TerrainTypeData
Player clicks tile

BuilderInputController.PaintTerrain:
    tiles = GetTilesInBrush(centerTile)
    for each tile:
        tile.TerrainType = selected terrain
        tile.ApplyTerrainSettings()
```

## Elevation Tool

```pseudocode
Player chooses ElevationPaint
Player chooses elevation value
Player clicks tile

BuilderInputController.PaintElevation:
    tiles = GetTilesInBrush(centerTile)
    for each tile:
        tile.GetComponent<TileElevation>().SetElevation(selectedElevation)
```

## Obstacle Tool

```pseudocode
Player chooses ObstaclePaint
Player chooses ObstacleData
BuilderObstaclePreview shows footprint
Player rotates placement if needed
Player clicks tile

ObstacleManager.TryPlaceObstacle(selectedObstacle, tile.GridPosition, rotation)
```

## Interactable Tool

```pseudocode
Player chooses InteractablePaint
Player chooses InteractableData
Player clicks tile

InteractablePlacementService.TryPlaceInteractable(data, tile, rotation)
```

## Unit Tool

```pseudocode
Player chooses UnitPaint
Player chooses Player or Enemy team
Player chooses UnitData
Player chooses rotation
Player clicks tile

UnitPlacementService.TryPlaceUnit(...)
GridUnit.InitializeFromData(unitData)
GridUnit.PlaceOnTile(tile)
BuilderUnitRegistry.Register(PlacedBuilderUnit)

If Enemy:
    configure EnemyController behavior
    if Patrol:
        choose patrol end tile
```

## Enemy Behavior in Builder

```text
EnemyAIBehavior.Static
    enemy stays at home unless it detects something

EnemyAIBehavior.RandomLook
    enemy idles and looks around

EnemyAIBehavior.Patrol
    enemy moves between patrol start and patrol end
```

```pseudocode
When placing Patrol enemy:
    first click = enemy start/patrol start
    builder enters patrol end selection
    second click = patrol end
    PlacedBuilderUnit stores patrolStart and patrolEnd
    JSON saves patrol data
```

## Erase Tool

```pseudocode
Player chooses Erase
Player clicks tile

BuilderInputController.EraseTile:
    remove unit if present
    remove obstacle if present
    remove interactable if present
    remove patrol marker if present
```

---

# 9. JSON Save/Load Pipeline

## Data Scripts

```text
LevelLayoutData
TileLayoutData
ObstacleLayoutData
UnitLayoutData
InteractableLayoutData
ObjectiveLayoutData
ObjectiveTargetTileData
```

## JSON Structure

```text
LevelLayoutData
    width
    height
    tiles[]
    obstacles[]
    units[]
    interactables[]
    objectives[]
    loseWhenSeen
```

## Save Flow

```pseudocode
BuilderSaveLoadManager.SaveLevel:
    layoutData = BuildLevelLayoutData()
    json = JsonUtility.ToJson(layoutData, true)
    File.WriteAllText(SavePath, json)
    AssetDatabase.Refresh()
```

```pseudocode
BuildLevelLayoutData:
    save grid size
    save every tile terrain and elevation
    save every obstacle name/origin/rotation
    save every placed unit id/team/position/rotation/enemy behavior/patrol
    save every interactable id/position/rotation
    save objectives
    save loseWhenSeen
```

## Runtime Load Flow

```pseudocode
BattleSceneLevelLoader:
    read selected JSON from Resources/LevelLayouts
    parse LevelLayoutData
    rebuild grid
    apply tile elevation
    apply tile terrain
    place obstacles
    place interactables
    place player/enemy units
    initialize objectives
    start player turn
```

---

# 10. Objectives and Win/Loss

## Core Scripts

```text
BuilderObjectiveUIController
LevelObjectiveRegistry
LevelObjectiveRuntimeManager
ReachTileMarkerData
LevelObjectiveData
WinConditionType
BattleStateManager
```

## Win Condition Types

```text
None
KillAllEnemies
SurviveTurns
ReachTile
ReachWithoutBeingSeen
InteractWithObject
```

## Runtime Evaluation

```pseudocode
LevelObjectiveRuntimeManager.EvaluateBattleObjectives:
    if no player units alive:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if reach objective impossible because players died:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if all required objectives complete:
        BattleStateManager.EndBattleExternally("You Win")
```

## Reach Without Being Seen

```pseudocode
EnemyVisionDetector sees player:
    LevelObjectiveRuntimeManager.NotifyPlayerSeenIfNotHidden(player)

If loseWhenSeen:
    BattleStateManager.EndBattleExternally("You Lose")
```

---

# 11. Enemy AI Deep Dive

## Core Scripts

```text
EnemyController
EnemyVisionDetector
EnemyAIState
EnemyAIBehavior
EnemyStateFeedbackController
EnemyStateFeedbackData
GridUnit
AStarPathFinder
TurnManager
BarrelInteractable
HiddenStateComponent
LevelObjectiveRuntimeManager
```

## Behavior vs State

`EnemyAIBehavior` is the enemy's default configured style:

```text
Static
Patrol
RandomLook
```

`EnemyAIState` is what the enemy is doing right now:

```text
Idle
Patrol
Alert
Investigate
SearchBarrels
Combat
ReturnToPost
```

## Enemy Turn Entry

```pseudocode
TurnManager.RunEnemyTurnRoutine:
    for each living enemy:
        controller = enemy.GetComponent<EnemyController>()
        target = GetBestTargetForEnemy(enemy)
        controller.TryTakeTurn(target)
```

Meaning:

```text
TurnManager decides when enemies act.
EnemyController decides what each enemy does.
EnemyVisionDetector decides what each enemy can see.
GridUnit executes the actual move/attack/push.
```

## EnemyController Setup

```pseudocode
EnemyController.Awake:
    controlledUnit = GetComponent<GridUnit>()
    pathFinder = FindFirstObjectByType<AStarPathFinder>()
    gridManager = FindFirstObjectByType<GridManager>()
    interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>()
    stateFeedbackController = GetComponent<EnemyStateFeedbackController>()
    currentState = GetCalmState()
```

## Configuring Behavior

```pseudocode
EnemyController.ConfigureBehavior(behavior, patrolEnabled, patrolStart, patrolEnd):
    configuredBehavior = behavior
    hasPatrolRoute = behavior == Patrol AND patrolEnabled AND patrolStart != patrolEnd
    patrolStartGridPosition = patrolStart
    patrolEndGridPosition = patrolEnd

    if hasPatrolRoute:
        defaultState = Patrol
    else:
        defaultState = Idle

    CaptureHomePost()
    SetState(GetCalmState())
```

## State Resolution

```pseudocode
ResolveTurnState(visibleTarget):
    if visibleTarget exists:
        return Combat

    if visible/suspicious barrel exists:
        return SearchBarrels

    if investigation point exists:
        return Investigate

    if shouldReturnToPost:
        return ReturnToPost

    return GetCalmState()
```

## Main State Machine

```pseudocode
TryTakeTurn(visibleTarget):
    CaptureHomePost if needed
    RefreshBarrelLayoutMemory(true)
    SetState(ResolveTurnState(visibleTarget))

    switch currentState:
        Combat:
            if visibleTarget exists:
                return TryAct(visibleTarget)
            SetState(Investigate)
            return TryInvestigate()

        SearchBarrels:
            barrel = GetPriorityVisibleBarrelTarget()
            if barrel exists:
                return TrySearchVisibleBarrels(barrel)
            SetState(Investigate)
            return TryInvestigate()

        Investigate:
            return TryInvestigate()

        Alert:
            if HasInvestigationPointTarget():
                SetState(Investigate)
                return TryInvestigate()
            SetState(GetCalmState())
            return false

        Patrol:
            return TryPatrol()

        ReturnToPost:
            return TryReturnToPost()

        Idle:
            if configuredBehavior == RandomLook:
                return TryRandomLook()
            return false
```

## Combat

```pseudocode
TryAct(playerUnit):
    SetState(Combat)
    RememberTarget(playerUnit)

    if push is possible and useful:
        TryExecutePush(playerUnit)
        return true

    if attack is possible:
        controlledUnit.TryAttack(playerUnit)
        return true

    path = find path toward useful tile near player
    if path exists:
        controlledUnit.TryMove(path)
        return true

    return false
```

## Vision

```pseudocode
EnemyVisionDetector.CanSeeUnit(target):
    if target invalid:
        return false
    if outside vision range:
        return false
    if outside vision angle:
        return false
    if line of sight blocked:
        return false
    if grid obstacle line of sight blocked:
        return false

    LevelObjectiveRuntimeManager.NotifyPlayerSeenIfNotHidden(target)
    return true
```

Vision uses:

```text
UnitData.visionRange
UnitData.visionAngle
ObstacleData vision occlusion
HiddenStateComponent
BarrelInteractable
```

## Barrels and Suspicion

```pseudocode
Player hides in barrel:
    BarrelInteractable.TryHideUnit(player)
    HiddenStateComponent.IsHidden = true
    HiddenStateComponent.CurrentBarrel = barrel

Enemy sees barrel movement:
    EnemyController.NotifyEnemiesOfVisibleBarrelCarrier(player)
    enemy.RememberMovingBarrelTarget(player)
    enemy may enter Investigate or SearchBarrels

Enemy notices barrel layout changed:
    RefreshBarrelLayoutMemory()
    if new/missing barrel detected:
        RegisterSuspiciousBarrelChange()
        SetState(Investigate)
```

## Patrol and Return

```pseudocode
Patrol enemy:
    move between patrolStartGridPosition and patrolEndGridPosition
    flip destination when reaching endpoint
    if sees player:
        enter Combat

Static/random enemy:
    CaptureHomePost()
    if target lost:
        shouldReturnToPost = true
        TryReturnToPost()
```

---

# 12. UI and Camera

## Core Scripts

```text
BattlePauseMenuController
UnitActionMenuController
WorldHealthBar
BillBoard
BattleCameraController
ControllerUINavigationController
UICanvasSelectionFrame
UISelectionGridIndicator
UIWorldSpaceSelectionFrame
UpgradeButtonAudioController
SceneNavigationButton
```

Flow:

```pseudocode
TileSelector selects unit:
    UnitActionMenuController shows available actions

GridUnit health changes:
    WorldHealthBar updates display

Pause menu opens:
    BattlePauseMenuController blocks gameplay input

Controller input in UI:
    ControllerUINavigationController moves UI selection
```

---

# 13. Store, IAP, Ads, and Analytics

## Core Scripts

```text
ConsentGateController
AnalyticsManager
AdInitializer
AdManager
PurchaseFufillment
BoughtGemsEvent
GemAdViewedEvent
GemClickedEvent
SkinData
SkinStoreController
AssetBundleLoader
EnableDisable
ProfilerExample
```

Flow:

```pseudocode
IAP scene starts:
    ConsentGateController checks consent
    AnalyticsManager initializes services
    AdInitializer initializes ads

Player watches ad:
    AdManager shows rewarded ad
    PurchaseFufillment grants reward
    GemAdViewedEvent is sent

Player buys gems:
    PurchaseFufillment grants gems
    BoughtGemsEvent is sent

Player buys skin:
    SkinStoreController checks gems
    unlocks/equips SkinData
```

---

# 14. Debug, Editor, Generated, and External Scripts

## Debug

```text
DebugModeSwitcher
GridDebugPainter
ObstacleDebugPainter
```

```pseudocode
DebugModeSwitcher:
    toggles debug terrain/obstacle modes

GridDebugPainter:
    paints terrain at runtime for testing

ObstacleDebugPainter:
    places/removes obstacles at runtime for testing
```

## Generated/Input

```text
InputSystem_Actions
```

Generated by Unity Input System and used by gameplay, builder, turn, and UI input scripts.

## Editor

```text
BuildGemAssetBundles
```

Builds gem/asset bundles in the editor.

## External Particle Package

These are package/demo/editor support scripts, not core project gameplay:

```text
CameraTarget
EffectController
EffectDemo
EffectShaderPropertyStr
RenderEffect
TransformExtension
EffectControllerInspector
EffectToolBar
RenderEffectInspector
ShaderMaterialsEditor
XUIUtils
```

## TutorialInfo

```text
Readme
ReadmeEditor
```

Unity tutorial/readme support.

---

# 15. Complete Script Placement Index

```text
AStarPathFinder -> pathfinding for player and enemy movement.
PathNode -> internal A* node data.

BuilderInputController -> builder input and edit execution.
BuilderStateController -> current builder tool and selected assets.
BuilderToolMode -> builder tool enum.
BuilderUnitPaintTeam -> player/enemy paint team enum.
BuilderUnitRegistry -> tracks placed builder units.
BuilderCameraController -> builder camera control.
BuilderObstaclePreview -> obstacle placement preview.
BuilderUIController -> builder UI bridge.
PlacedBuilderUnit -> metadata for placed builder unit.
UnitPlacementService -> creates units on grid tiles.

DebugModeSwitcher -> toggles debug tools.
GridDebugPainter -> debug terrain painter.
GridManager -> creates/owns grid.
GridRangeFinder -> reachable tile calculation.
GridTile -> board cell.
ObstacleData -> obstacle configuration.
ObstacleDebugPainter -> debug obstacle painter.
ObstacleManager -> obstacle placement/removal.
PlaceableAnchor -> placeable helper anchor.
PlacedObstacle -> runtime obstacle record.
TerrainTypeData -> terrain configuration.
TileElevation -> tile height.
TileManager -> terrain lookup.
TileSelector -> player battle input.

AdInitializer -> initializes ads.
AdManager -> loads/shows ads.
AnalyticsManager -> analytics/events.
AssetBundleLoader -> asset bundle loading.
ConsentGateController -> consent gate.
EnableDisable -> GameObject toggle utility.
BoughtGemsEvent -> analytics event.
GemAdViewedEvent -> analytics event.
GemClickedEvent -> analytics event.
ProfilerExample -> profiling/demo utility.
PurchaseFufillment -> grants purchases/rewards.
SkinData -> skin data.
SkinStoreController -> skin shop.

BuilderSaveLoadManager -> builder JSON save/load.
LevelLayoutData -> JSON data model.

MainMenuManager -> menu manager.
MenuAmbience -> menu audio.
MenuCameraController -> menu camera.
SettingsManager -> settings expansion.

BuilderObjectiveUIController -> objective builder UI.
LevelObjectiveRegistry -> stores builder objectives.
LevelObjectiveRuntimeManager -> runtime objective evaluation.
ReachTileMarkerData -> objective marker data.

BattleSceneLevelLoader -> runtime JSON loader.
LevelPickerUIController -> level selection UI.
SelectedBattleLevel -> selected level memory.

BattleCameraController -> battle camera.
BattleStateManager -> win/loss manager.
EnemyTurnSpeedMode -> enemy speed enum.
TurnManager -> turn loop.
TurnState -> turn state enum.
UnitTurnSnapshot -> restart turn data.

EnemyAIBehavior -> enemy behavior enum.
EnemyAIState -> enemy state enum.
EnemyController -> enemy brain.
EnemySpawner -> classic enemy spawner.
EnemyVisionDetector -> enemy sight.
HiddenStateComponent -> hidden/barrel state.
BarrelInteractable -> barrel interaction.
InteractableData -> interactable data.
InteractableLibrary -> interactable lookup.
InteractablePlacementService -> interactable placement.
InteractableRegistry -> interactable registry.
InteractableType -> interactable type enum.
LevelObjectiveData -> objective data.
PlacedInteractable -> placed interactable record.
WinConditionType -> objective enum.

GridUnit -> runtime unit.
UnitSpawner -> classic player spawner.
AIType -> unit AI type enum.
AttackType -> attack type enum.
ElementType -> element enum.
UnitRole -> role enum.
UnitAbilityData -> ability data.
UnitData -> unit stats/config.
UnitSpawnEntry -> spawn entry.
UnitTeam -> team enum.
UnitTurnRulesData -> turn rules.

BattlePauseMenuController -> pause menu.
BillBoard -> face camera.
AttackEffectData -> attack visual data.
EnemyStateFeedbackController -> enemy state visuals.
EnemyStateFeedbackData -> enemy feedback data.
ControllerUINavigationController -> controller UI nav.
UICanvasSelectionFrame -> canvas selection frame.
UISelectionGridIndicator -> UI grid indicator.
UIWorldSpaceSelectionFrame -> world-space selection frame.
SceneNavigationButton -> scene load button.
UnitActionMenuController -> unit action menu.
UpgradeButtonAudioController -> button audio.
WorldHealthBar -> health bar.

BuildGemAssetBundles -> editor bundle builder.
InputSystem_Actions -> generated input wrapper.

CameraTarget, EffectController, EffectDemo, EffectShaderPropertyStr,
RenderEffect, TransformExtension, EffectControllerInspector, EffectToolBar,
RenderEffectInspector, ShaderMaterialsEditor, XUIUtils -> external effect package.

Readme, ReadmeEditor -> tutorial/readme support.
```

---

# 16. Final Mental Model

```text
Scenes decide where the player is.
Resources provide configurable data.
The Builder creates JSON.
BattleSceneLevelLoader turns JSON into a playable battle.
GridManager and GridTile are the board.
GridUnit is the unit body.
TileSelector is the player input brain.
TurnManager is the turn clock.
EnemyController is the enemy brain.
EnemyVisionDetector is the enemy eyes.
LevelObjectiveRuntimeManager is the objective judge.
BattleStateManager announces the result.
```

---

# 17. Script-by-Script Reference

This section is a compact technical reference. It is meant to answer:

```text
What does this script do?
Which variables/data matter?
Which methods matter?
What does it connect to?
Where is it used?
```

## AStarCore

### `AStarPathFinder`

```text
Purpose
    Finds a valid path between two GridTile objects.

Important data
    GridManager reference.
    Movement cost from GridUnit/GridTile.
    Height difference from TileElevation.

Important methods
    FindPath(startTile, targetTile, unit)
    ReconstructPath(...)
    movement/neighbor validation helpers

Connects to
    GridManager
    GridTile
    GridUnit
    TileElevation
    PathNode

Used by
    TileSelector for player movement.
    EnemyController for chase, patrol, investigation, and return paths.
```

### `PathNode`

```text
Purpose
    Internal A* data object.

Important data
    tile
    parent
    gCost
    hCost
    fCost

Connects to
    AStarPathFinder
```

## Builder

### `BuilderStateController`

```text
Purpose
    Stores the current builder mode and selected builder assets.

Important variables/data
    currentToolMode
    brushSize
    selectedElevationValue
    selectedUnitPaintTeam
    selectedEnemyBehavior
    loadedTerrainTypes
    loadedObstacleTypes
    loadedInteractableTypes
    terrainIndex / obstacleIndex / interactableIndex / unitIndex
    selectedObstacleRotationY
    selectedInteractableRotationY
    selectedUnitRotationY

Important methods
    LoadAssetsFromResources()
    SetToolMode(...)
    SetBrushSize(...)
    SetSelectedElevationValue(...)
    SetSelectedUnitPaintTeam(...)
    SetSelectedEnemyBehavior(...)
    SelectNext/Previous Terrain, Obstacle, Interactable, Unit

Connects to
    Resources/TerrainTypes
    Resources/ObstacleTypes
    Resources/InteractableData
    Resources/UnitData/Player
    Resources/UnitData/Enemy

Used by
    BuilderInputController
    BuilderUIController
    BuilderObstaclePreview
```

### `BuilderInputController`

```text
Purpose
    Converts builder input into actual map edits.

Important variables/data
    currentHoveredTile
    pointerPosition
    isPickingObjectiveTile
    selectedObjectiveTile
    isPickingEnemyPatrolEndTile
    selectedEnemyPatrolStartTile
    pendingEnemyPatrolUnit
    placement rotation state

Important methods
    Update()
    UpdateHoveredTile()
    OnLeftClickStarted(...)
    PaintTerrain(...)
    PaintElevation(...)
    PlaceObstacle(...)
    PlaceInteractable(...)
    PlaceUnit(...)
    EraseTile(...)
    ConfigurePlacedUnitAI(...)
    BeginEnemyPatrolEndSelection(...)
    CompleteEnemyPatrolEndSelection(...)
    GetTilesInBrush(...)

Connects to
    BuilderStateController
    GridManager
    GridTile
    TileElevation
    ObstacleManager
    InteractablePlacementService
    UnitPlacementService
    BuilderUnitRegistry
    BuilderUIController
    BuilderObjectiveUIController

Used in
    LevelBuilderScene
```

### `BuilderUIController`

```text
Purpose
    Connects builder UI controls to builder systems.

Important data
    UI buttons, labels, panels, inputs, sliders, toggles.

Important methods
    UI callbacks for changing tool, brush, terrain, obstacle, unit, elevation, rotation, save/load.

Connects to
    BuilderStateController
    BuilderSaveLoadManager
    BuilderInputController
    GridManager

Used in
    LevelBuilderScene
```

### `BuilderCameraController`

```text
Purpose
    Moves and controls the camera in the builder.

Important data
    movement speed
    zoom settings
    rotation settings
    input state

Connects to
    Camera
    Input

Used in
    LevelBuilderScene
```

### `BuilderObstaclePreview`

```text
Purpose
    Shows a placement preview for obstacles/interactables before clicking.

Important data
    selected obstacle/interactable
    current hovered tile
    preview instance/materials/colors
    validity state

Connects to
    BuilderStateController
    BuilderInputController
    ObstacleManager
    GridTile

Used in
    LevelBuilderScene
```

### `BuilderToolMode`

```text
Purpose
    Enum for active builder tool.

Values
    TerrainPaint
    ObstaclePaint
    InteractablePaint
    UnitPaint
    ElevationPaint
    Erase

Used by
    BuilderStateController
    BuilderInputController
    BuilderUIController
```

### `BuilderUnitPaintTeam`

```text
Purpose
    Enum for whether the builder places player units or enemy units.

Values
    Player
    Enemy

Used by
    BuilderStateController
    BuilderInputController
    UnitPlacementService
    BuilderSaveLoadManager
```

### `BuilderUnitRegistry`

```text
Purpose
    Tracks units placed in the builder.

Important data
    list of PlacedBuilderUnit

Important methods
    Register(...)
    Unregister(...)
    GetPlacedUnits()

Connects to
    PlacedBuilderUnit
    BuilderSaveLoadManager
    UnitPlacementService

Used in
    LevelBuilderScene
```

### `PlacedBuilderUnit`

```text
Purpose
    Stores metadata for a unit placed in the builder.

Important data
    GridUnit reference
    UnitData
    BuilderUnitPaintTeam
    rotationY
    useCardinalFacing
    EnemyAIBehavior
    patrol start/end

Connects to
    BuilderUnitRegistry
    BuilderSaveLoadManager
    EnemyController
```

### `UnitPlacementService`

```text
Purpose
    Instantiates and places units from UnitData.

Important data
    player/enemy parents
    BuilderUnitRegistry
    GridManager

Important methods
    TryPlaceUnit(...)
    Remove/clear placed units
    Configure visuals/rotation

Connects to
    UnitData
    GridUnit
    GridTile
    EnemyController
    BuilderUnitRegistry

Used by
    BuilderInputController
    BuilderSaveLoadManager
```

## Grid, Tiles, Terrain, and Obstacles

### `GridManager`

```text
Purpose
    Creates, stores, rebuilds, and queries the grid.

Important variables/data
    width
    height
    tileSpacing
    tilePrefab
    tileParent
    tileManager
    GridTile[,] grid

Important methods
    GenerateGrid()
    RebuildGrid(newWidth, newHeight)
    GetTileAt(position)
    GetWorldPosition(position)
    GetNeighbors(tile)
    GetWalkableNeighbors(tile)
    CanUnitEnterTile(tile)

Connects to
    GridTile
    TileManager
    Pathfinding
    Builder
    BattleSceneLevelLoader

Used in
    BattleScene
    LevelBuilderScene
```

### `GridTile`

```text
Purpose
    Represents one grid cell.

Important variables/data
    X, Y, GridPosition
    TerrainType
    CurrentTerrainData
    isWalkable
    movementCost
    isOccupied
    OccupyingUnit
    renderers and highlight overlay

Important methods
    Initialize(...)
    ApplyTerrainSettings()
    SetOccupant(...)
    ForceSetWalkable(...)
    ShowOverlayColor(...)
    ClearOverlay()
    GetTraversalCost(...)
    GetTopRenderer()

Connects to
    TileManager
    TerrainTypeData
    GridUnit
    TileElevation
    ObstacleManager
    TileSelector

Used in
    Almost every gameplay and builder system.
```

### `TileManager`

```text
Purpose
    Loads and provides terrain data.

Important data
    TerrainTypeData dictionary

Important methods
    Load terrain assets from Resources/TerrainTypes
    GetTerrainData(type)

Connects to
    TerrainTypeData
    GridTile
    BuilderStateController
```

### `TerrainTypeData`

```text
Purpose
    ScriptableObject for terrain behavior and visuals.

Important variables/data
    TerrainType
    MovementCost
    IsWalkable
    DamageOnEnter
    DamageOnStop
    MovementPenaltyOnEnter
    MovementPenaltyOnStop
    color/materials/decoration

Connects to
    GridTile
    TileManager
    GridUnit terrain effects
```

### `TileElevation`

```text
Purpose
    Controls tile height.

Important variables/data
    elevation
    step height
    top surface
    side column
    overlay/collider/anchor references

Important methods
    SetElevation(value)
    ApplyElevation()

Connects to
    GridTile
    AStarPathFinder
    GridRangeFinder
    ObstacleManager
    BuilderInputController
```

### `GridRangeFinder`

```text
Purpose
    Finds every tile reachable by a unit with current movement points.

Important methods
    GetReachableTiles(...)

Connects to
    GridManager
    GridTile
    GridUnit
    TileElevation

Used by
    TileSelector movement highlights.
```

### `ObstacleManager`

```text
Purpose
    Places, tracks, validates, and removes obstacles.

Important variables/data
    GridManager
    obstacleParent
    placedObstacles
    tileToObstacleMap

Important methods
    TryPlaceObstacle(...)
    CanPlaceObstacle(...)
    TryRemoveObstacleAtTile(...)
    ClearAllObstacles()
    GetPlacedObstacles()

Connects to
    ObstacleData
    PlacedObstacle
    GridTile
    TileElevation
    BuilderSaveLoadManager
    BattleSceneLevelLoader
```

### `ObstacleData`

```text
Purpose
    ScriptableObject for obstacle configuration.

Important variables/data
    obstacle prefab
    footprint size
    movement blocking
    terrain under obstacle
    visual offsets/rotation/scale
    vision occlusion settings

Connects to
    ObstacleManager
    EnemyVisionDetector
    BuilderStateController
```

### `PlacedObstacle`

```text
Purpose
    Runtime record of an obstacle already placed.

Important data
    ObstacleData
    Instance
    Origin
    RotationY
    OccupiedTiles

Connects to
    ObstacleManager
    BuilderSaveLoadManager
    BattleSceneLevelLoader
```

### `PlaceableAnchor`

```text
Purpose
    Helper anchor/pivot for placeable objects.

Used by
    Placement/visual alignment workflows.
```

## Units and Combat

### `GridUnit`

```text
Purpose
    Runtime unit logic: movement, combat, HP, death, push, terrain effects, hidden state support.

Important variables/data
    UnitData
    UnitTeam
    currentTile
    currentHP
    remainingMovementPoints
    hasMovedThisTurn
    attacksUsedThisTurn
    turnRules
    attackEffectData
    healthBarPrefab
    visualRoot

Important methods
    InitializeFromData(...)
    PlaceOnTile(...)
    TryMove(...)
    MoveAlongPath(...)
    TryAttack(...)
    TakeDamage(...)
    Die()
    CanMoveThisTurn()
    CanAttackThisTurn()
    TryPush(...)
    ResetTurnState()
    ApplyTerrainStartTurnEffects()

Connects to
    GridTile
    UnitData
    UnitTurnRulesData
    TurnManager
    BattleStateManager
    EnemyController
    HiddenStateComponent
    BarrelInteractable
    WorldHealthBar

Used in
    Player units and enemy units.
```

### `UnitData`

```text
Purpose
    ScriptableObject defining unit stats and capabilities.

Important variables/data
    UnitId
    unitName
    prefab
    maxHP
    attackPower
    defense
    movementPoints
    attackRange
    maxClimbHeight
    visionRange
    visionAngle
    attackType
    elementType
    aiType
    canHideInBarrel
    canBackstab
    canPush
    push settings

Connects to
    GridUnit
    BuilderStateController
    UnitPlacementService
    BattleSceneLevelLoader
    EnemyVisionDetector
```

### `UnitTurnRulesData`

```text
Purpose
    ScriptableObject defining action order rules.

Important variables/data
    CanMoveAfterAttacking
    CanAttackAfterMoving

Connects to
    GridUnit.CanMoveThisTurn()
    GridUnit.CanAttackThisTurn()
```

### `UnitAbilityData`

```text
Purpose
    Data container for unit abilities.

Used as
    Ability expansion point.
```

### `UnitSpawnEntry`

```text
Purpose
    Spawn configuration entry for classic spawners.

Connects to
    UnitSpawner
    EnemySpawner
```

### `UnitSpawner`

```text
Purpose
    Classic player unit spawner.

Important data
    spawn entries
    GridManager

Connects to
    UnitSpawnEntry
    UnitData
    GridUnit

Note
    BattleSceneLevelLoader can disable this when JSON levels are used.
```

### `EnemySpawner`

```text
Purpose
    Classic enemy unit spawner.

Connects to
    UnitSpawnEntry
    UnitData
    GridUnit
    EnemyController

Note
    BattleSceneLevelLoader can disable this when JSON levels are used.
```

### Unit Enums

```text
UnitTeam
    Player / Enemy.

AttackType
    Attack category.

ElementType
    Element category.

UnitRole
    Role category.

AIType
    AI type category stored in UnitData.
```

## Turn System

### `TurnManager`

```text
Purpose
    Controls the main turn loop.

Important variables/data
    CurrentTurn
    tileSelector
    playerSpawner
    enemySpawner
    objectiveRuntimeManager
    enemyTurnSpeedMode
    enemyTurnDelay
    playerTurnSnapshots
    remainingRestartTurnUses

Important methods
    StartPlayerTurn()
    StartEnemyTurn()
    EndTurn()
    RunEnemyTurnRoutine()
    SetBusy()
    ReturnToPlayerControl()
    CapturePlayerTurnSnapshot()
    RestartPlayerTurn()
    HandlePlayerUnitsDoneState()

Connects to
    GridUnit
    TileSelector
    EnemyController
    EnemyVisionDetector
    BattleStateManager
    LevelObjectiveRuntimeManager
```

### `TurnState`

```text
Purpose
    Enum for global turn state.

Values
    PlayerTurn
    EnemyTurn
    Busy
```

### `EnemyTurnSpeedMode`

```text
Purpose
    Enum for enemy turn delay multiplier.

Values
    Normal
    Fast
    SuperFast
```

### `UnitTurnSnapshot`

```text
Purpose
    Stores unit state for restarting player turn.

Important data
    unit reference
    grid position
    current HP
    dead/alive state
    action state
    remaining movement points
    visual rotation
```

## Enemy AI and Vision

### `EnemyController`

```text
Purpose
    Enemy AI brain and state machine.

Important variables/data
    controlledUnit
    pathFinder
    gridManager
    interactablePlacementService
    stateFeedbackController
    currentState
    defaultState
    configuredBehavior
    patrol route data
    home post data
    rememberedTargetUnit
    lastKnownTargetTile
    observedBarrelPositions
    suspiciousBarrelPositions
    barrel layout memory
    shouldReturnToPost

Important methods
    ConfigureBehavior(...)
    TryTakeTurn(...)
    ResolveTurnState(...)
    SetState(...)
    TryAct(...)
    TryInvestigate()
    TryPatrol()
    TryReturnToPost()
    TryRandomLook()
    RememberTarget(...)
    RememberMovingBarrelTarget(...)
    NotifyEnemiesOfVisiblePlayer(...)
    NotifyEnemyHitByAttacker(...)
    NotifyEnemiesOfVisibleBarrelCarrier(...)
    RefreshBarrelLayoutMemory(...)

Connects to
    GridUnit
    EnemyVisionDetector
    AStarPathFinder
    GridManager
    BarrelInteractable
    HiddenStateComponent
    EnemyStateFeedbackController
    LevelObjectiveRuntimeManager
```

### `EnemyVisionDetector`

```text
Purpose
    Enemy sight system.

Important variables/data
    ownerUnit
    vision settings from UnitData/GridUnit
    barrel target offset
    debug gizmo/runtime vision settings

Important methods
    CanSeeUnit(...)
    CanSeeBarrel(...)
    CanSeeTile(...)
    CanAnyEnemySeeUnit(...)
    CanAnyEnemySeeBarrel(...)
    RefreshHiddenState(...)
    RefreshAllHiddenStates()
    RevealHiddenByVisibleBarrelMovement(...)

Connects to
    GridUnit
    BarrelInteractable
    HiddenStateComponent
    ObstacleData
    LevelObjectiveRuntimeManager
```

### `EnemyAIBehavior`

```text
Purpose
    Configured enemy behavior enum.

Values
    Static
    Patrol
    RandomLook
```

### `EnemyAIState`

```text
Purpose
    Runtime enemy state enum.

Values
    Idle
    Patrol
    Alert
    Investigate
    SearchBarrels
    Combat
    ReturnToPost
```

### `EnemyStateFeedbackController`

```text
Purpose
    Shows visual feedback for enemy AI state.

Important data
    EnemyStateFeedbackData
    visual/icon/effect references

Connects to
    EnemyController
    EnemyAIState
```

### `EnemyStateFeedbackData`

```text
Purpose
    ScriptableObject/data for enemy state feedback visuals.

Connects to
    EnemyStateFeedbackController
```

## Interactables and Hiding

### `HiddenStateComponent`

```text
Purpose
    Tracks whether a unit is hidden, usually inside a barrel.

Important variables/data
    IsHidden
    CurrentBarrel
    BarrelKnownToEnemies

Important methods
    SetHiddenState(...)
    ForceReveal()

Connects to
    GridUnit
    BarrelInteractable
    EnemyVisionDetector
    EnemyController
```

### `BarrelInteractable`

```text
Purpose
    Barrel interaction: hiding, moving while hidden, absorbing hits, enemy search.

Important variables/data
    hidden unit
    current tile
    break/search state

Important methods
    CanUnitHideHere(...)
    TryHideUnit(...)
    TryAbsorbHitAndBreak(...)
    BreakOpenByEnemySearch()
    OnCarrierTileChanged(...)
    GetBarrelTilePublic()

Connects to
    HiddenStateComponent
    GridUnit
    EnemyVisionDetector
    EnemyController
    InteractablePlacementService
```

### `InteractableData`

```text
Purpose
    ScriptableObject defining interactable content.

Important data
    interactableId
    displayName
    prefab
    type

Connects to
    InteractableLibrary
    InteractablePlacementService
    BuilderStateController
```

### `InteractableLibrary`

```text
Purpose
    Looks up InteractableData by id.

Connects to
    BattleSceneLevelLoader
    BuilderSaveLoadManager
```

### `InteractablePlacementService`

```text
Purpose
    Places and clears interactables on grid tiles.

Important methods
    TryPlaceInteractable(...)
    GetPlacedInteractableAtTile(...)
    ClearAllInteractables()

Connects to
    InteractableData
    PlacedInteractable
    InteractableRegistry
    GridTile
```

### `InteractableRegistry`

```text
Purpose
    Registry/list of placed interactables.

Connects to
    InteractablePlacementService
    BuilderSaveLoadManager
```

### `PlacedInteractable`

```text
Purpose
    Runtime record of a placed interactable.

Important data
    InteractableData
    tile/position
    instance
```

### `InteractableType`

```text
Purpose
    Enum/classification for interactable types.
```

## Objectives

### `LevelObjectiveRuntimeManager`

```text
Purpose
    Runtime objective evaluator.

Important variables/data
    activeObjectives
    currentRoundCount
    playerWasSeen
    loseWhenSeen
    reach marker data

Important methods
    InitializeObjectives(...)
    ResetObjectives()
    NotifyPlayerSeen(...)
    NotifyPlayerSeenIfNotHidden(...)
    OnPlayerTurnStarted()
    OnPlayerTurnEnded()
    OnUnitDied(...)
    EvaluateObjectives()
    EvaluateBattleObjectives()

Connects to
    BattleStateManager
    EnemyVisionDetector
    GridUnit
    ReachTileMarkerData
```

### `BuilderObjectiveUIController`

```text
Purpose
    Builder UI for creating/editing objectives.

Connects to
    LevelObjectiveRegistry
    BuilderInputController
    WinConditionType
```

### `LevelObjectiveRegistry`

```text
Purpose
    Stores builder objective data before saving.

Important data
    objectives
    LoseWhenSeen

Connects to
    BuilderObjectiveUIController
    BuilderSaveLoadManager
```

### `ReachTileMarkerData`

```text
Purpose
    Visual data for reach objective tile markers.

Connects to
    LevelObjectiveRuntimeManager
```

### `LevelObjectiveData`

```text
Purpose
    Objective-related data container.
```

### `WinConditionType`

```text
Purpose
    Objective type enum.

Values
    None
    KillAllEnemies
    SurviveTurns
    ReachTile
    ReachWithoutBeingSeen
    InteractWithObject
```

## Runtime Level and JSON

### `BattleSceneLevelLoader`

```text
Purpose
    Loads selected JSON level into BattleScene.

Important variables/data
    fallbackLevelFileName
    clearSelectedLevelAfterLoad
    GridManager
    ObstacleManager
    unit parents
    TurnManager
    BattleStateManager
    LevelObjectiveRuntimeManager
    InteractableLibrary
    InteractablePlacementService

Important methods
    LoadConfiguredLevel()
    RebuildBattleLevel(...)
    ClearCurrentBattleState()
    ApplyTileElevations(...)
    ApplyTileTerrains(...)
    PlaceObstacles(...)
    PlaceInteractables(...)
    PlaceUnits(...)

Connects to
    SelectedBattleLevel
    LevelLayoutData
    Resources/LevelLayouts
    Resources/ObstacleTypes
    Resources/UnitData
```

### `BuilderSaveLoadManager`

```text
Purpose
    Saves builder state to JSON and loads JSON back into builder.

Important variables/data
    levelFileName
    GridManager
    ObstacleManager
    BuilderUnitRegistry
    Interactable systems
    LevelObjectiveRegistry

Important methods
    SaveLevel()
    LoadLevel()
    BuildLevelLayoutData()
    RebuildLevel(...)
    SetLevelFileName(...)

Connects to
    LevelLayoutData
    GridTile
    TileElevation
    PlacedObstacle
    PlacedBuilderUnit
    PlacedInteractable
```

### `LevelLayoutData`

```text
Purpose
    Serializable JSON data model.

Contains
    LevelLayoutData
    TileLayoutData
    ObstacleLayoutData
    UnitLayoutData
    InteractableLayoutData
    ObjectiveLayoutData
    ObjectiveTargetTileData
```

### `LevelPickerUIController`

```text
Purpose
    Builds level selection UI.

Connects to
    Resources/LevelLayouts
    SelectedBattleLevel
    SceneManager
```

### `SelectedBattleLevel`

```text
Purpose
    Static holder for selected level name between scenes.

Important methods
    Set(...)
    Clear()

Connects to
    LevelPickerUIController
    BattleSceneLevelLoader
```

## Battle State, UI, and Camera

### `BattleStateManager`

```text
Purpose
    Tracks and displays battle result.

Important data
    BattleEnded
    result UI
    restart UI

Important methods
    CheckBattleState()
    NotifyUnitDied(...)
    EndBattleExternally(...)
    ResetBattleState()

Connects to
    GridUnit
    TileSelector
    LevelObjectiveRuntimeManager
```

### `TileSelector`

```text
Purpose
    Player battle input controller.

Important data
    selectedUnit
    currentHoveredTile
    reachableTiles
    previewPath
    pendingActionMode
    actionMenu
    pathFinder
    rangeFinder
    interactablePlacementService

Important methods
    HandlePrimaryClick()
    TrySelectUnit(...)
    ShowUnitActionMenu()
    ExecuteMoveToTile(...)
    ExecuteAttack(...)
    ExecutePush(...)
    ForceClearSelectionAndHighlights()

Connects to
    GridUnit
    GridTile
    AStarPathFinder
    GridRangeFinder
    TurnManager
    UnitActionMenuController
    BarrelInteractable
```

### `UnitActionMenuController`

```text
Purpose
    Floating menu for selected unit actions.

Important methods
    ShowForUnit(...)
    Hide()
    TryHandlePointerClick(...)

Connects to
    TileSelector
    GridUnit action availability
```

### `BattlePauseMenuController`

```text
Purpose
    Pause menu and pause state.

Important data
    IsPauseMenuOpen

Connects to
    TileSelector
    UI buttons
    Scene navigation
```

### `BattleCameraController`

```text
Purpose
    Battle camera movement/zoom/rotation.
```

### `WorldHealthBar`

```text
Purpose
    World-space unit health display.

Connects to
    GridUnit
    BillBoard/camera-facing UI
```

### `BillBoard`

```text
Purpose
    Rotates object to face camera.
```

### UI Navigation Scripts

```text
ControllerUINavigationController
    Handles gamepad/controller UI navigation.

UICanvasSelectionFrame
    Selection frame for canvas UI.

UISelectionGridIndicator
    Selection indicator for UI grids.

UIWorldSpaceSelectionFrame
    Selection frame for world-space UI.

SceneNavigationButton
    Loads scenes from UI buttons.

UpgradeButtonAudioController
    Audio/feedback for upgrade or store buttons.
```

## Store, IAP, Ads, Analytics

### Store/IAP Scripts

```text
ConsentGateController
    Checks consent before services.

AnalyticsManager
    Initializes analytics and sends events.

AdInitializer
    Initializes ads.

AdManager
    Loads/shows ads.

PurchaseFufillment
    Grants purchase/ad rewards.

BoughtGemsEvent
    Analytics event for gem purchase.

GemAdViewedEvent
    Analytics event for ad reward.

GemClickedEvent
    Analytics event for gem UI interaction.

SkinData
    Skin configuration data.

SkinStoreController
    Buying/equipping skins.

AssetBundleLoader
    Loads bundles/assets.

EnableDisable
    Toggles objects.

ProfilerExample
    Profiling/demo utility.
```

## Menus

```text
MainMenuManager
    Main menu manager/expansion point.

MenuCameraController
    Menu camera behavior.

MenuAmbience
    Menu audio/ambience.

SettingsManager
    Settings expansion point.
```

## Debug, Generated, Editor, External

```text
DebugModeSwitcher
    Toggles debug modes.

GridDebugPainter
    Debug terrain painting.

ObstacleDebugPainter
    Debug obstacle placement/removal.

InputSystem_Actions
    Unity-generated input wrapper.

BuildGemAssetBundles
    Editor tool for asset bundles.

CameraTarget
EffectController
EffectDemo
EffectShaderPropertyStr
RenderEffect
TransformExtension
EffectControllerInspector
EffectToolBar
RenderEffectInspector
ShaderMaterialsEditor
XUIUtils
    External particle/effect package scripts.

Readme
ReadmeEditor
    Unity tutorial/readme support.
```
