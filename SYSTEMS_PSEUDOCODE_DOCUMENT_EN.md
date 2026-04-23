# Systems Pseudocode Document: How Everything Connects

## Purpose

This document explains the Unity project as a system map. It starts with the big systems, then breaks them into branches, then explains where each script fits in the game logic.

The goal is to understand:

- what each system does,
- which scripts belong to each system,
- what data each script uses,
- how the scripts communicate,
- how the logic flows during menus, level building, level loading, battle, enemy turns, objectives, UI, store/IAP, and debug tools.

## Very Short Overview

```text
Game
    Main Menu / Level Picker
        choose a level
        store selected level name
        load BattleScene

    BattleScene
        load level JSON
        rebuild grid
        place terrain, elevations, obstacles, interactables, objectives, and units
        start the turn loop
        evaluate win/loss

    LevelBuilderScene
        edit the grid
        paint terrain
        change elevation
        place obstacles
        place units
        place interactables
        define objectives
        save JSON

    Store / IAP
        manage gems
        ads
        purchases
        skins
        analytics
```

## System Index

```text
1. Scene and navigation system
2. Grid, tile, terrain, and elevation system
3. Pathfinding and range system
4. Unit, stats, combat, and action system
5. Turn system
6. Enemy AI system
7. Vision, hiding, and barrel system
8. Objective and win/loss system
9. Level builder system
10. JSON save/load system
11. Runtime level loading system
12. Battle UI and controller navigation system
13. Store, IAP, ads, and analytics system
14. Debug, editor, generated input, and external package scripts
```

---

# 1. Scene and Navigation System

## Branches

```text
Scenes
    MainMenu.unity
    LevelPicker.unity
    BattleScene.unity
    LevelBuilderScene.unity
    IAP Scene.unity

Scripts
    MainMenuManager
    SceneNavigationButton
    LevelPickerUIController
    SelectedBattleLevel
    MenuCameraController
    MenuAmbience
    SettingsManager
```

## General Logic

```pseudocode
WHEN THE GAME OPENS:
    show main menu

IF player presses play:
    load LevelPicker

IN LevelPicker:
    find JSON levels inside Resources/LevelLayouts
    create a button for each level

IF player chooses a level:
    SelectedBattleLevel.LevelFileName = selected level name
    load BattleScene

IN BattleScene:
    BattleSceneLevelLoader reads SelectedBattleLevel
    if no selected level exists:
        use fallbackLevelFileName
    load that JSON
```

## Script Roles

```pseudocode
MainMenuManager:
    belongs to the main menu scene
    currently works mostly as an expansion point

SceneNavigationButton:
    receives a scene name from the inspector
    on click:
        SceneManager.LoadScene(sceneName)

LevelPickerUIController:
    loads level assets from Resources/LevelLayouts
    creates level buttons
    on level click:
        SelectedBattleLevel.Set(levelName)
        load BattleScene

SelectedBattleLevel:
    static memory shared between scenes
    stores the chosen level name until BattleScene loads it

MenuCameraController:
    controls menu camera movement/presentation

MenuAmbience:
    controls menu ambience/audio

SettingsManager:
    expansion point for settings such as volume, display, and controls
```

---

# 2. Grid, Tile, Terrain, and Elevation System

## Branches

```text
Grid
    GridManager
    GridTile

Terrain
    TileManager
    TerrainTypeData

Elevation
    TileElevation

Obstacle support
    ObstacleData
    ObstacleManager
    PlacedObstacle
    PlaceableAnchor

Debug
    GridDebugPainter
    ObstacleDebugPainter
```

## System Purpose

```text
GridManager creates and stores the board.
GridTile represents one cell on the board.
TileManager loads terrain data from Resources.
TerrainTypeData defines terrain behavior and visuals.
TileElevation changes tile height.
ObstacleManager places blockers and obstacle visuals.
```

## Grid Creation Flow

```pseudocode
GridManager.Awake:
    GenerateGrid()

GenerateGrid:
    grid = new GridTile[width, height]

    for x from 0 to width:
        for y from 0 to height:
            worldPosition = (x * tileSpacing, 0, y * tileSpacing)
            tile = Instantiate(tilePrefab, worldPosition)
            tile.Initialize(x, y, tileManager)
            grid[x, y] = tile
```

## Grid Query Flow

```pseudocode
IsInsideGrid(position):
    return position.x is within width AND position.y is within height

GetTileAt(position):
    if position is outside grid:
        return null
    return grid[position.x, position.y]

GetNeighbors(tile):
    check up, right, down, left
    return valid neighboring tiles

CanUnitEnterTile(tile):
    if tile is null:
        return false
    if tile is not walkable:
        return false
    if tile is occupied:
        return false
    return true
```

## Tile Logic

```pseudocode
GridTile.Initialize(x, y, tileManager):
    X = x
    Y = y
    store tileManager reference
    ApplyTerrainSettings()

GridTile.ApplyTerrainSettings:
    data = tileManager.GetTerrainData(terrainType)
    CurrentTerrainData = data

    if data exists:
        isWalkable = data.IsWalkable
        movementCost = data.MovementCost
        apply color/material
        spawn terrain decoration if needed

GridTile.SetOccupant(unitObject):
    OccupyingUnit = unitObject
    isOccupied = unitObject != null

GridTile.ShowOverlayColor(color):
    enable overlay
    set overlay color

GridTile.ClearOverlay:
    disable overlay
```

## Terrain Data Flow

```pseudocode
TileManager.Awake:
    terrainTypes = Resources.LoadAll<TerrainTypeData>("TerrainTypes")
    for each terrainData:
        dictionary[terrainData.TerrainType] = terrainData

TerrainTypeData:
    stores TerrainType
    stores movement cost
    stores walkable/not walkable
    stores damage on enter/stop
    stores movement penalties
    stores top material, side material, color, decoration prefab
```

## Elevation Flow

```pseudocode
TileElevation.SetElevation(value):
    elevation = max(0, value)
    ApplyElevation()

ApplyElevation:
    move tile top surface upward
    scale side column
    move overlay above top surface
    update collider
    move decoration anchor
    adjust side texture tiling
```

Elevation affects movement:

```pseudocode
When pathfinding checks a neighbor:
    heightDifference = abs(currentTile.elevation - neighbor.elevation)
    if heightDifference > unit.MaxClimbHeight:
        unit cannot move there
```

## Obstacle Flow

```pseudocode
ObstacleManager.TryPlaceObstacle(obstacleData, origin, rotationY):
    if CanPlaceObstacle(...) is false:
        return false

    create PlacedObstacle record
    rotatedOffsets = GetRotatedFootprintOffsets(obstacleData.FootprintSize, rotationY)

    for each offset:
        tile = gridManager.GetTileAt(origin + offset)
        register tile as occupied by this obstacle

        if obstacle paints terrain underneath:
            tile.TerrainType = obstacleData.TerrainTypeUnderObstacle
            tile.ApplyTerrainSettings()

        if obstacle blocks movement:
            tile.ForceSetWalkable(false)

    instantiate obstacle prefab at calculated center
    add PlacedObstacle to placedObstacles
    return true
```

```pseudocode
ObstacleManager.CanPlaceObstacle:
    for each tile in rotated footprint:
        if tile is outside grid:
            return false
        if tile already has obstacle:
            return false
        if tile is not walkable or is occupied:
            return false
        if footprint tiles do not share same elevation:
            return false
    return true
```

---

# 3. Pathfinding and Range System

## Branches

```text
Exact path
    AStarPathFinder
    PathNode

Reachable area
    GridRangeFinder
```

## A* Pathfinding

```pseudocode
AStarPathFinder.FindPath(startTile, targetTile, unit):
    if start or target is invalid:
        return empty path

    openSet = nodes to check
    closedSet = nodes already checked

    create start PathNode
    add start node to openSet

    while openSet is not empty:
        current = node with lowest fCost

        if current.tile == targetTile:
            return reconstruct path through parent nodes

        move current from openSet to closedSet

        for each neighbor in GridManager.GetNeighbors(current.tile):
            if neighbor is already closed:
                continue

            if neighbor is not valid for this unit:
                continue

            newCost = current.gCost + movement cost into neighbor

            if neighbor is not in openSet OR newCost is better:
                update parent
                update gCost/hCost/fCost
                add/update neighbor in openSet

    return empty path
```

## Reachable Range

```pseudocode
GridRangeFinder.GetReachableTiles(startTile, movementPoints, unit):
    reachable = dictionary tile -> cheapest cost
    frontier = queue

    reachable[startTile] = 0
    enqueue startTile

    while frontier is not empty:
        current = dequeue frontier

        for each neighbor in GridManager.GetNeighbors(current):
            if neighbor is invalid:
                continue

            newCost = reachable[current] + unit.GetMovementCostForTile(neighbor)

            if newCost > movementPoints:
                continue

            if neighbor not in reachable OR newCost is cheaper:
                reachable[neighbor] = newCost
                enqueue neighbor

    return reachable
```

Where it fits:

```text
TileSelector uses GridRangeFinder to highlight possible movement.
TileSelector uses AStarPathFinder to preview and execute the chosen path.
EnemyController uses AStarPathFinder to chase, patrol, investigate, or return home.
```

---

# 4. Unit, Stats, Combat, and Action System

## Branches

```text
Data
    UnitData
    UnitAbilityData
    UnitTurnRulesData
    UnitSpawnEntry
    UnitTeam
    AttackType
    ElementType
    UnitRole
    AIType

Runtime units
    GridUnit
    UnitSpawner
    EnemySpawner

Player action control
    TileSelector
    UnitActionMenuController

Visuals
    WorldHealthBar
    AttackEffectData
    BillBoard
```

## Unit Data

```pseudocode
UnitData:
    UnitId
    unitName
    prefab
    maxHP
    attackPower
    defense
    movementPoints
    attackRange
    maxClimbHeight
    attackType
    elementType
    aiType
    visionRange
    visionAngle
    canHideInBarrel
    canBackstab
    canPush
    pushWeight
```

Small data/enums:

```text
UnitAbilityData = data container for configurable/future abilities.
UnitTurnRulesData = rules for moving after attacking or attacking after moving.
UnitSpawnEntry = spawn configuration used by classic spawners.
UnitTeam = Player or Enemy.
AttackType = attack category.
ElementType = elemental category.
UnitRole = unit role category.
AIType = AI category stored in UnitData.
```

## Unit Placement

```pseudocode
GridUnit.PlaceOnTile(tile):
    if currentTile exists:
        currentTile.SetOccupant(null)

    currentTile = tile
    tile.SetOccupant(gameObject)
    transform.position = grounded position above tile

    if unit is inside a barrel:
        update barrel tile
        refresh hidden state
```

## Movement

```pseudocode
GridUnit.TryMove(path):
    if CanMoveThisTurn() is false:
        return false
    if path is empty:
        return false
    if already moving:
        return false
    if path[0] is not currentTile:
        return false

    destination = last tile in path
    if destination is occupied by another unit:
        return false

    cost = CalculatePathMovementCost(path)
    if cost > remainingMovementPoints:
        return false

    MarkMovedThisTurn()
    remainingMovementPoints -= cost
    MoveAlongPath(path)
    return true
```

```pseudocode
GridUnit.MoveAlongPath(path):
    TurnManager.SetBusy()
    StartCoroutine(MoveRoutine(path))

MoveRoutine(path):
    isMoving = true
    currentTile.SetOccupant(null)

    for each next tile in path:
        move transform toward tile
        rotate visual toward movement direction
        apply terrain entry effects
        update barrel if carrying one
        notify enemies if visible player moved

    currentTile.SetOccupant(gameObject)
    isMoving = false

    if Team == Player:
        TurnManager.ReturnToPlayerControl()

    OnMovementFinished?.Invoke(this)
```

## Attack

```pseudocode
GridUnit.CanAttack(target):
    if target is null:
        return false
    if target == this:
        return false
    if target.Team == Team:
        return false
    return IsTargetInRange(target)
```

```pseudocode
GridUnit.TryAttack(target):
    if CanAttackThisTurn() is false:
        return false
    if CanAttack(target) is false:
        return false

    isBackstab = CanPerformBackstabOn(target)
    FaceTarget(target)
    ShowAttackEffect(target)

    damage = CalculateAttackDamage(target, isBackstab)
    target.TakeDamage(damage)

    if attacker is hidden:
        reveal attacker

    if player hit enemy:
        EnemyController.NotifyEnemyHitByAttacker(target, this)

    MarkAttackedThisTurn()
    return true
```

## Damage and Death

```pseudocode
GridUnit.TakeDamage(amount):
    if unit is already dead:
        return

    if unit is inside barrel:
        if barrel absorbs the hit and breaks:
            return

    if unit is hidden:
        reveal unit

    currentHP -= amount
    currentHP = max(currentHP, 0)

    if currentHP <= 0:
        Die()

GridUnit.Die:
    currentTile.SetOccupant(null)
    gameObject.SetActive(false)
    BattleStateManager.NotifyUnitDied(this)
```

## Push

```pseudocode
GridUnit.CanPush(target, gridManager):
    if cannot attack this turn:
        return false
    if unit does not have push ability:
        return false
    if target is invalid, dead, or ally:
        return false
    if target cannot be pushed:
        return false
    if weight rules prevent push:
        return false

    direction = target.tile - my.tile
    destination = calculate final push tile
    return destination exists

GridUnit.TryPush(target):
    if CanPush(...) is false:
        return false

    path = straight push path from target tile to destination
    target.ForceMoveAlongPath(path)
    MarkAttackedThisTurn()
    notify enemy if player pushed enemy
    return true
```

## Player Action Input

```pseudocode
TileSelector.HandlePrimaryClick:
    if battle ended or pause menu open:
        return
    if not player turn or TurnManager is Busy:
        return

    clickedUnit = unit on hovered tile
    clickedBarrel = barrel on hovered tile

    if no selectedUnit:
        if clickedUnit is Player:
            SelectUnit(clickedUnit)
            ShowUnitActionMenu()
        return

    if clicked another Player unit:
        SelectUnit(clickedUnit)
        ShowUnitActionMenu()
        return

    if pendingActionMode == Move:
        if clickedBarrel:
            ExecuteBarrelInteraction(clickedBarrel)
        else:
            ExecuteMoveToTile(currentHoveredTile)
        pendingActionMode = None
        return

    if pendingActionMode == Attack:
        if clickedUnit is Enemy:
            ExecuteAttack(clickedUnit)
        return

    if pendingActionMode == Push:
        if clickedUnit is Enemy:
            ExecutePush(clickedUnit)
        return
```

---

# 5. Turn System

## Branches

```text
State
    TurnManager
    TurnState

Snapshots
    UnitTurnSnapshot

Enemy speed
    EnemyTurnSpeedMode
```

## Turn States

```pseudocode
TurnState:
    PlayerTurn
    EnemyTurn
    Busy
```

`Busy` blocks player input while movement, enemy logic, or an action is running.

## Player Turn

```pseudocode
TurnManager.StartPlayerTurn:
    CurrentTurn = PlayerTurn
    refresh turn UI
    clear player hint

    for each GridUnit:
        if unit.Team == Player:
            unit.ResetTurnState()
            unit.ApplyTerrainStartTurnEffects()

    EnemyVisionDetector.RefreshAllHiddenStates()
    CapturePlayerTurnSnapshot()
    objectiveRuntimeManager.OnPlayerTurnStarted()
```

## Enemy Turn

```pseudocode
TurnManager.StartEnemyTurn:
    TileSelector.ForceClearSelectionAndHighlights()
    CurrentTurn = EnemyTurn
    refresh turn UI

    for each GridUnit:
        if unit.Team == Enemy:
            unit.ResetTurnState()
            unit.ApplyTerrainStartTurnEffects()

    StartCoroutine(RunEnemyTurnRoutine())
```

```pseudocode
TurnManager.RunEnemyTurnRoutine:
    CurrentTurn = Busy
    wait enemyTurnDelay adjusted by EnemyTurnSpeedMode

    enemies = GetLivingEnemies()

    for each enemy:
        controller = enemy.GetComponent<EnemyController>()
        target = GetBestTargetForEnemy(enemy)

        acted = controller.TryTakeTurn(target)

        if enemy moved:
            wait until movement finishes or timeout

        if enemy action animation is running:
            wait until animation finishes or timeout

        refresh awareness and hidden states

    EnemyVisionDetector.RefreshAllHiddenStates()
    StartPlayerTurn()
```

## Restart Turn

```pseudocode
TurnManager.CapturePlayerTurnSnapshot:
    clear snapshot list
    for each unit:
        save unit reference
        save grid position
        save current HP
        save dead/alive state
        save movement/attack state
        save remaining movement points
        save visual rotation

TurnManager.RestartPlayerTurn:
    if not PlayerTurn or Busy:
        return
    if no restart uses remain:
        return
    restore every UnitTurnSnapshot
    clear selection and highlights
    refresh battle state
```

---

# 6. Enemy AI System

## Branches

```text
Spawning
    EnemySpawner

Brain
    EnemyController

Vision
    EnemyVisionDetector

State/behavior
    EnemyAIState
    EnemyAIBehavior

Feedback
    EnemyStateFeedbackController
    EnemyStateFeedbackData
```

## Enemy Decision Flow

```pseudocode
EnemyController.TryTakeTurn(visibleTarget):
    if no home post captured:
        CaptureHomePost()

    RefreshBarrelLayoutMemory()

    nextState = ResolveTurnState(visibleTarget)
    SetState(nextState)

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
            if has investigation point:
                SetState(Investigate)
                return TryInvestigate()
            SetState(calm state)
            return false

        Patrol:
            return TryPatrol()

        ReturnToPost:
            return TryReturnToPost()

        Idle:
            if behavior == RandomLook:
                return TryRandomLook()
            return false
```

```pseudocode
EnemyController.ResolveTurnState(visibleTarget):
    if visibleTarget exists:
        return Combat
    if visible/suspicious barrel exists:
        return SearchBarrels
    if has last known target or investigation point:
        return Investigate
    if should return to post:
        return ReturnToPost
    if configured as patrol:
        return Patrol
    return Idle
```

## Vision Flow

```pseudocode
EnemyVisionDetector.CanSeeUnit(unit):
    if unit invalid or dead:
        return false
    if outside vision range:
        return false
    if outside vision angle:
        return false
    if line of sight blocked:
        return false
    if unit is hidden:
        return false
    return true

When player is seen:
    EnemyController.RememberTarget(player)
    LevelObjectiveRuntimeManager.NotifyPlayerSeen(player)
```

## Feedback Flow

```pseudocode
EnemyController.SetState(nextState):
    currentState = nextState
    EnemyStateFeedbackController shows visual feedback

EnemyStateFeedbackData:
    maps each EnemyAIState to icon/color/effect data
```

---

# 7. Vision, Hiding, and Barrel System

## Branches

```text
Hiding
    HiddenStateComponent

Enemy sight
    EnemyVisionDetector

Interactables
    BarrelInteractable
    InteractableData
    InteractableLibrary
    InteractablePlacementService
    InteractableRegistry
    InteractableType
    PlacedInteractable
```

## Hidden State

```pseudocode
HiddenStateComponent:
    IsHidden
    CurrentBarrel
    BarrelKnownToEnemies

HideInBarrel(barrel):
    CurrentBarrel = barrel
    IsHidden = true

ForceReveal:
    IsHidden = false
    BarrelKnownToEnemies = true
```

## Barrel Logic

```pseudocode
BarrelInteractable.CanUnitHideHere(unit):
    if barrel is occupied:
        return false
    if unit cannot hide in barrel:
        return false
    return true

BarrelInteractable.TryHideUnit(unit):
    if CanUnitHideHere is false:
        return false
    store hidden unit
    update HiddenStateComponent
    update visuals
    return true

BarrelInteractable.TryAbsorbHitAndBreak(unit):
    if barrel can absorb hit:
        break barrel
        reveal unit
        return true
    return false
```

## Interactable Placement

```pseudocode
InteractablePlacementService.TryPlaceInteractable(data, tile, rotation):
    if tile invalid:
        return false
    if interactable already exists there:
        return false
    instantiate data prefab
    create PlacedInteractable
    register it
    return true

InteractableRegistry:
    stores placed interactables

InteractableLibrary:
    resolves interactableId into InteractableData
```

---

# 8. Objective and Win/Loss System

## Branches

```text
Objective data
    LevelObjectiveData
    WinConditionType
    ObjectiveLayoutData
    ObjectiveTargetTileData

Builder objective setup
    BuilderObjectiveUIController
    LevelObjectiveRegistry

Runtime objective logic
    LevelObjectiveRuntimeManager
    ReachTileMarkerData

Battle result
    BattleStateManager
```

## Objective Runtime Flow

```pseudocode
LevelObjectiveRuntimeManager.InitializeObjectives(objectives):
    activeObjectives = copy of objectives
    currentRoundCount = 0
    playerWasSeen = false
    objectivesInitialized = true
    RebuildReachTileMarkers()
```

```pseudocode
LevelObjectiveRuntimeManager.EvaluateBattleObjectives:
    if objectives not initialized:
        return
    if battle already ended:
        return

    if all enemies defeated:
        clear player seen state

    if no player units alive:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if reach objective is impossible because players died:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if not all required win objectives complete:
        return

    BattleStateManager.EndBattleExternally("You Win")
```

## Objective Completion

```pseudocode
IsObjectiveComplete(objective):
    switch objective.winConditionType:
        KillAllEnemies:
            return AreAllEnemiesDefeated()

        SurviveTurns:
            return currentRoundCount >= surviveTurnCount

        ReachTile:
            return all required reach zones are occupied by living players

        ReachWithoutBeingSeen:
            if playerWasSeen:
                return false
            return all required reach zones are occupied by living players

        InteractWithObject:
            return false // present as data, not fully implemented
```

## Battle State

```pseudocode
BattleStateManager.CheckBattleState:
    if battle already ended:
        return
    if no living players:
        end as loss
    else if no living enemies:
        end as win or allow objective system to decide

BattleStateManager.NotifyUnitDied(unit):
    objectiveRuntimeManager.OnUnitDied(unit)
    CheckBattleState()
```

---

# 9. Level Builder System

## Branches

```text
Tool state
    BuilderStateController
    BuilderToolMode
    BuilderUnitPaintTeam

Input/editing
    BuilderInputController
    BuilderCameraController

UI
    BuilderUIController
    BuilderObstaclePreview
    BuilderObjectiveUIController

Units
    UnitPlacementService
    BuilderUnitRegistry
    PlacedBuilderUnit

Saving/loading
    BuilderSaveLoadManager
```

## Builder Startup

```pseudocode
LevelBuilderScene starts:
    GridManager creates editable grid
    BuilderStateController loads Resources assets
    BuilderUIController connects UI controls
    BuilderInputController starts reading pointer/click input
    BuilderCameraController handles camera movement
```

## Tool State

```pseudocode
BuilderStateController.Awake:
    LoadAssetsFromResources()
    ClampSelectionIndices()

LoadAssetsFromResources:
    loadedTerrainTypes = Resources.LoadAll("TerrainTypes")
    loadedObstacleTypes = Resources.LoadAll("ObstacleTypes")
    loadedInteractableTypes = Resources.LoadAll("InteractableData")
    unit types = Resources.LoadAll("UnitData/Player" or "UnitData/Enemy")

SetToolMode(mode):
    currentToolMode = mode

SetBrushSize(size):
    brushSize = max(1, size)

SetSelectedElevationValue(value):
    selectedElevationValue = max(0, value)
```

## Builder Click Logic

```pseudocode
BuilderInputController.Update:
    if UI is blocking scene interaction:
        clear hover
        return

    update hovered tile
    if picking objective tile:
        show objective hover
    else if picking enemy patrol end:
        show patrol hover
    update keyboard rotation
```

```pseudocode
BuilderInputController.OnLeftClickStarted:
    if no hovered tile:
        return

    if picking objective tile:
        select objective tile
        return

    if picking patrol end:
        select patrol end tile
        return

    switch BuilderStateController.CurrentToolMode:
        TerrainPaint:
            paint selected TerrainType on brush tiles

        ElevationPaint:
            call TileElevation.SetElevation on brush tiles

        ObstaclePaint:
            ObstacleManager.TryPlaceObstacle(...)

        UnitPaint:
            UnitPlacementService.TryPlaceUnit(...)
            BuilderUnitRegistry registers PlacedBuilderUnit

        InteractablePaint:
            InteractablePlacementService.TryPlaceInteractable(...)

        ObjectivePaint:
            BuilderObjectiveUIController receives selected target tiles/objectives

        Erase:
            remove unit, obstacle, interactable, or reset tile content
```

## Builder Unit Logic

```pseudocode
UnitPlacementService.TryPlaceUnit(unitData, tile, team, rotation, behavior):
    if tile is invalid or occupied:
        return false
    if unitData or prefab missing:
        return false

    instance = Instantiate(unitData.prefab)
    gridUnit = instance.GetComponent<GridUnit>()
    gridUnit.InitializeFromData(unitData)
    gridUnit.PlaceOnTile(tile)

    if team == Enemy:
        configure EnemyController behavior/patrol

    BuilderUnitRegistry.Register(PlacedBuilderUnit)
    return true
```

```pseudocode
PlacedBuilderUnit:
    stores UnitData
    stores GridUnit reference
    stores Player/Enemy paint team
    stores rotation/facing
    stores enemy behavior and patrol route
```

---

# 10. JSON Save/Load System

## Branches

```text
Serializable data
    LevelLayoutData
    TileLayoutData
    ObstacleLayoutData
    UnitLayoutData
    InteractableLayoutData
    ObjectiveLayoutData
    ObjectiveTargetTileData

Manager
    BuilderSaveLoadManager
```

## JSON Structure

```pseudocode
LevelLayoutData:
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
    json = JsonUtility.ToJson(layoutData, prettyPrint = true)
    File.WriteAllText(SavePath, json)
    AssetDatabase.Refresh()
```

```pseudocode
BuildLevelLayoutData:
    layout.width = gridManager.Width
    layout.height = gridManager.Height

    for each tile:
        save x, y, terrainType, elevation

    for each placed obstacle:
        save obstacleName, origin, rotation

    for each PlacedBuilderUnit:
        save unitId, x, y, team, rotation, behavior, patrol route

    for each interactable:
        save interactableId, x, y, rotation

    for each objective:
        save win condition and targets

    save loseWhenSeen
    return layout
```

## Builder Load Flow

```pseudocode
BuilderSaveLoadManager.LoadLevel:
    if save file does not exist:
        warn and return

    json = File.ReadAllText(SavePath)
    layoutData = JsonUtility.FromJson<LevelLayoutData>(json)
    RebuildLevel(layoutData)

RebuildLevel(layoutData):
    ClearCurrentBuilderState()
    gridManager.RebuildGrid(layoutData.width, layoutData.height)
    apply elevations
    apply terrain
    place obstacles
    place interactables
    place units
    restore objectives
```

---

# 11. Runtime Level Loading System

## Main Script

```text
BattleSceneLevelLoader
```

## Runtime Load Flow

```pseudocode
BattleSceneLevelLoader.Awake:
    disable classic UnitSpawner if assigned
    disable classic EnemySpawner if assigned

Start:
    LoadConfiguredLevel()
```

```pseudocode
LoadConfiguredLevel:
    levelFileName = ResolveLevelFileName()

    jsonAsset = Resources.Load<TextAsset>("LevelLayouts/" + levelFileName)
    if jsonAsset missing:
        log error
        return

    layoutData = JsonUtility.FromJson<LevelLayoutData>(jsonAsset.text)
    RebuildBattleLevel(layoutData)

    battleStateManager.ResetBattleState()
    objectiveRuntimeManager.InitializeObjectives(layoutData.objectives)
    objectiveRuntimeManager.SetLoseWhenSeen(layoutData.loseWhenSeen)
    turnManager.StartPlayerTurn()
    battleStateManager.CheckBattleState()

    if clearSelectedLevelAfterLoad:
        SelectedBattleLevel.Clear()
```

```pseudocode
RebuildBattleLevel(layoutData):
    ClearCurrentBattleState()
    gridManager.RebuildGrid(width, height)

    obstacleMap = Resources.LoadAll("ObstacleTypes")
    playerUnitMap = Resources.LoadAll("UnitData/Player")
    enemyUnitMap = Resources.LoadAll("UnitData/Enemy")

    obstacleCoveredTiles = calculate obstacle footprints

    ApplyTileElevations(layoutData)
    ApplyTileTerrains(layoutData, obstacleCoveredTiles)
    PlaceObstacles(layoutData, obstacleMap)
    PlaceInteractables(layoutData)
    PlaceUnits(layoutData, playerUnitMap, enemyUnitMap)
```

Key difference:

```text
BuilderSaveLoadManager writes and reads physical JSON files in the editor.
BattleSceneLevelLoader reads TextAsset JSON from Resources at runtime.
```

---

# 12. Battle UI and Controller Navigation System

## Branches

```text
Battle UI
    BattlePauseMenuController
    UnitActionMenuController
    WorldHealthBar
    BillBoard

Controller/UI navigation
    ControllerUINavigationController
    UICanvasSelectionFrame
    UISelectionGridIndicator
    UIWorldSpaceSelectionFrame

Camera
    BattleCameraController

Button/audio utility
    UpgradeButtonAudioController
```

## UI Flow

```pseudocode
During battle:
    BattleCameraController moves/zooms/rotates tactical camera

    BattlePauseMenuController:
        if pause menu open:
            TileSelector ignores gameplay input
            pause UI buttons become active

    UnitActionMenuController:
        when TileSelector selects a unit:
            show action menu near unit
            enable Move if unit.CanMoveThisTurn()
            enable Attack if unit.CanAttackThisTurn()
            enable Push if unit can push
            enable barrel option if unit has barrel

    WorldHealthBar:
        reads GridUnit.CurrentHP and GridUnit.MaxHP
        updates health display

    BillBoard:
        makes world-space UI face the camera

    ControllerUINavigationController:
        moves UI selection with gamepad/controller

    UICanvasSelectionFrame:
        draws selection frame on canvas UI

    UISelectionGridIndicator:
        shows selected cell in UI grids

    UIWorldSpaceSelectionFrame:
        draws selection frame for world-space UI

    UpgradeButtonAudioController:
        plays/controls audio feedback for upgrade/store buttons
```

---

# 13. Store, IAP, Ads, and Analytics System

## Branches

```text
Services
    ConsentGateController
    AnalyticsManager
    AdInitializer
    AdManager

Rewards and purchases
    PurchaseFufillment
    BoughtGemsEvent
    GemAdViewedEvent
    GemClickedEvent

Store
    SkinData
    SkinStoreController
    AssetBundleLoader

Utilities
    EnableDisable
    ProfilerExample
```

## Store/IAP Flow

```pseudocode
When entering Store/IAP:
    ConsentGateController checks user consent
    AnalyticsManager initializes Unity Services / Analytics
    AdInitializer initializes Unity Ads

If player watches rewarded ad:
    AdManager.ShowRewardedAd()
    when ad completes:
        PurchaseFufillment grants gems/reward
        AnalyticsManager records GemAdViewedEvent

If player buys gems:
    PurchaseFufillment processes purchase
    add gems
    AnalyticsManager records BoughtGemsEvent

If player clicks gem UI:
    AnalyticsManager records GemClickedEvent

If player buys skin:
    SkinStoreController checks gems
    subtracts price
    marks skin as owned
    saves progress
    equips or allows equip
```

```pseudocode
SkinData:
    stores skin name, cost, icon/asset reference

AssetBundleLoader:
    loads asset bundles for store/icons/assets if needed

EnableDisable:
    utility for toggling GameObjects

ProfilerExample:
    profiling/demo support, not core gameplay
```

---

# 14. Debug, Editor, Generated Input, and External Package Scripts

## Debug Flow

```pseudocode
DebugModeSwitcher.Update:
    read debug input
    switch active debug mode:
        none
        terrain painting
        obstacle painting
    enable or disable debug painter components

GridDebugPainter.OnPaintClick:
    raycast tile under pointer
    change tile terrain type
    tile.ApplyTerrainSettings()

ObstacleDebugPainter.OnPaintClick:
    raycast tile under pointer
    ObstacleManager.TryPlaceObstacle(...)

ObstacleDebugPainter.OnEraseClick:
    raycast tile under pointer
    ObstacleManager.TryRemoveObstacleAtTile(tilePosition)
```

## Editor and Generated Input

```pseudocode
InputSystem_Actions:
    generated by Unity Input System
    used by:
        TileSelector
        TurnManager
        BuilderInputController
        GridDebugPainter
        ObstacleDebugPainter
        UI/controller navigation scripts

BuildGemAssetBundles:
    editor tool
    builds gem/asset bundles
```


---

# Complete Script Placement Index

This section confirms where each script fits in the logic.

```text
AStarPathFinder -> pathfinding for player movement and enemy movement.
PathNode -> internal A* node data.

BuilderInputController -> builder click/hover/edit logic.
BuilderStateController -> current builder tool and selected assets.
BuilderToolMode -> builder tool enum.
BuilderUnitPaintTeam -> builder unit team enum.
BuilderUnitRegistry -> tracks placed builder units for save/load.
BuilderCameraController -> builder camera control.
BuilderObstaclePreview -> builder placement preview.
BuilderUIController -> builder UI bridge.
PlacedBuilderUnit -> saved/runtime metadata for builder units.
UnitPlacementService -> creates units on grid tiles.

DebugModeSwitcher -> toggles debug tools.

GridDebugPainter -> runtime terrain paint debug.
GridManager -> creates and owns the board.
GridRangeFinder -> calculates reachable movement tiles.
GridTile -> one board cell.
ObstacleData -> obstacle configuration.
ObstacleDebugPainter -> runtime obstacle debug placement.
ObstacleManager -> obstacle placement/removal/validation.
PlaceableAnchor -> helper anchor for placeable objects.
PlacedObstacle -> runtime record of a placed obstacle.
TerrainTypeData -> terrain configuration.
TileElevation -> tile height behavior.
TileManager -> terrain data lookup.
TileSelector -> player input and action execution in battle.

AdInitializer -> initializes ads.
AdManager -> loads/shows ads.
AnalyticsManager -> initializes analytics and sends events.
AssetBundleLoader -> loads bundles/assets.
ConsentGateController -> consent gate for services.
EnableDisable -> toggles GameObjects.
BoughtGemsEvent -> analytics event for bought gems.
GemAdViewedEvent -> analytics event for ad reward.
GemClickedEvent -> analytics event for gem interaction.
ProfilerExample -> profiling/demo utility.
PurchaseFufillment -> grants purchases/rewards.
SkinData -> skin store data.
SkinStoreController -> buying/equipping skins.

BuilderSaveLoadManager -> saves/loads builder JSON.
LevelLayoutData -> JSON data model.

MainMenuManager -> main menu manager/expansion point.
MenuAmbience -> menu ambience/audio.
MenuCameraController -> menu camera.
SettingsManager -> settings manager/expansion point.

BuilderObjectiveUIController -> builder objective UI.
LevelObjectiveRegistry -> stores builder objectives.
LevelObjectiveRuntimeManager -> evaluates runtime objectives.
ReachTileMarkerData -> marker visual data for reach objectives.

BattleSceneLevelLoader -> runtime JSON level loader.
LevelPickerUIController -> level selection UI.
SelectedBattleLevel -> selected level memory between scenes.

BattleCameraController -> battle camera.
BattleStateManager -> battle win/loss state.
EnemyTurnSpeedMode -> enemy turn speed enum.
TurnManager -> global turn loop.
TurnState -> global turn state enum.
UnitTurnSnapshot -> restart-turn snapshot.

EnemyAIBehavior -> enemy behavior enum.
EnemyAIState -> enemy state enum.
EnemyController -> enemy AI brain.
EnemySpawner -> classic enemy spawner.
EnemyVisionDetector -> enemy vision logic.
HiddenStateComponent -> hidden/barrel state on units.
BarrelInteractable -> barrel hiding/breaking interaction.
InteractableData -> interactable configuration.
InteractableLibrary -> interactable data lookup.
InteractablePlacementService -> places interactables.
InteractableRegistry -> stores placed interactables.
InteractableType -> interactable type enum.
LevelObjectiveData -> objective-related data.
PlacedInteractable -> runtime record of placed interactable.
WinConditionType -> win condition enum.

GridUnit -> runtime unit logic.
UnitSpawner -> classic player unit spawner.
AIType -> unit AI type enum.
AttackType -> attack type enum.
ElementType -> element type enum.
UnitRole -> unit role enum.
UnitAbilityData -> ability data.
UnitData -> unit stats/configuration.
UnitSpawnEntry -> spawn entry data.
UnitTeam -> player/enemy enum.
UnitTurnRulesData -> action order rules.

BattlePauseMenuController -> battle pause menu.
BillBoard -> world UI faces camera.
AttackEffectData -> attack visual data.
EnemyStateFeedbackController -> enemy state visual feedback.
EnemyStateFeedbackData -> enemy feedback data.
ControllerUINavigationController -> controller UI navigation.
UICanvasSelectionFrame -> canvas UI selection frame.
UISelectionGridIndicator -> UI grid selection indicator.
UIWorldSpaceSelectionFrame -> world-space UI selection frame.
SceneNavigationButton -> scene loading button.
UnitActionMenuController -> unit action menu.
UpgradeButtonAudioController -> upgrade/store button audio.
WorldHealthBar -> unit health bar.

BuildGemAssetBundles -> editor bundle builder.
InputSystem_Actions -> generated input wrapper.

CameraTarget -> external effect package camera target.
EffectController -> external effect controller.
EffectDemo -> external effect demo.
EffectShaderPropertyStr -> external shader property constants.
RenderEffect -> external render effect helper.
TransformExtension -> external transform helpers.
EffectControllerInspector -> external custom inspector.
EffectToolBar -> external editor toolbar.
RenderEffectInspector -> external custom inspector.
ShaderMaterialsEditor -> external material/shader editor.
XUIUtils -> external UI/editor utilities.

Readme -> Unity tutorial/readme data.
ReadmeEditor -> custom editor for Readme.
```

---

# Main Battle Flow

```pseudocode
BattleScene starts

BattleSceneLevelLoader:
    choose JSON level
    rebuild GridManager grid
    apply TileElevation values
    apply GridTile terrain using TileManager and TerrainTypeData
    place obstacles using ObstacleManager and ObstacleData
    place interactables using InteractablePlacementService
    place units using UnitData and GridUnit
    configure EnemyController if enemy units exist
    initialize LevelObjectiveRuntimeManager
    call TurnManager.StartPlayerTurn()

Player selects unit:
    TileSelector detects click
    if hovered tile has Player GridUnit:
        select unit
        UnitActionMenuController shows actions

Player chooses Move:
    GridRangeFinder finds reachable tiles
    AStarPathFinder finds exact path
    GridUnit.TryMove(path)
    TurnManager enters Busy
    unit moves
    TurnManager returns to PlayerTurn

Player chooses Attack:
    TileSelector receives target
    GridUnit.TryAttack(enemy)
    enemy TakeDamage()
    if enemy dies:
        BattleStateManager.NotifyUnitDied()
        LevelObjectiveRuntimeManager evaluates objectives

Player ends turn:
    TurnManager.EndTurn()
    LevelObjectiveRuntimeManager.OnPlayerTurnEnded()
    TurnManager.StartEnemyTurn()

Enemy turn:
    for each living enemy:
        EnemyVisionDetector checks targets
        EnemyController.TryTakeTurn(target)
        enemy attacks, pushes, investigates, patrols, returns, or waits

After all enemies:
    TurnManager.StartPlayerTurn()

Battle ends if:
    no player units alive -> lose
    objectives complete -> win
    loseWhenSeen and player is seen -> lose
```

---

# Main Builder Flow

```pseudocode
LevelBuilderScene starts

GridManager:
    creates editable grid

BuilderStateController:
    loads terrain types
    loads obstacle types
    loads interactables
    loads unit data

BuilderUIController:
    connects UI buttons/sliders/dropdowns to BuilderStateController

BuilderInputController:
    reads pointer input
    detects hovered GridTile
    applies active BuilderToolMode

If Terrain tool:
    change GridTile.TerrainType
    GridTile.ApplyTerrainSettings()

If Elevation tool:
    TileElevation.SetElevation()

If Obstacle tool:
    ObstacleManager.TryPlaceObstacle()

If Unit tool:
    UnitPlacementService.TryPlaceUnit()
    BuilderUnitRegistry registers PlacedBuilderUnit

If Interactable tool:
    InteractablePlacementService.TryPlaceInteractable()

If Objective tool:
    BuilderObjectiveUIController creates ObjectiveLayoutData
    LevelObjectiveRegistry stores objectives

If Save:
    BuilderSaveLoadManager.BuildLevelLayoutData()
    write JSON into Resources/LevelLayouts

If Load:
    BuilderSaveLoadManager reads JSON
    GridManager.RebuildGrid()
    rebuild terrain, elevation, obstacles, units, interactables, objectives
```

---

# Mental Summary

```text
The grid is the base.
    GridManager creates it.
    GridTile stores terrain, occupancy, and highlights.

Units live on tiles.
    GridUnit knows HP, movement, attacks, push, death, and current tile.

TileSelector turns player input into gameplay.
    Select, move, attack, push, interact.

TurnManager controls when anything can happen.
    PlayerTurn allows input.
    Busy blocks input.
    EnemyTurn runs AI.

EnemyController decides enemy behavior.
    Combat if it sees player.
    Investigate if it remembers something.
    Patrol if it has a route.
    Idle if nothing is happening.

The builder creates data.
    Builder tools edit the map.
    BuilderSaveLoadManager writes JSON.

BattleSceneLevelLoader consumes that data.
    Reads JSON.
    Rebuilds the level.
    Starts combat.

LevelObjectiveRuntimeManager controls special win/loss objectives.
    Kill all enemies.
    Survive turns.
    Reach tile.
    Reach without being seen.
```


