# Documento en Pseudocodigo: Como Esta Conectado Todo

## Proposito

Este documento explica el proyecto como si fuera un mapa de sistemas. Primero divide el juego en sistemas grandes, despues en ramas internas, y despues baja a scripts especificos y pseudocodigo.

La idea es que puedas entender:

- que hace cada sistema,
- que scripts pertenecen a cada sistema,
- que datos usa cada script,
- como se comunican entre ellos,
- cual es el flujo de logica cuando el jugador juega, crea niveles o carga niveles.

## Vista General Super Corta

```text
Juego
    Menu / Level Picker
        selecciona nivel
        guarda nombre del nivel elegido
        carga BattleScene

    BattleScene
        carga JSON del nivel
        reconstruye grid
        coloca terreno, altura, obstaculos, interactables y unidades
        inicia turnos
        evalua objetivos y victoria / derrota

    LevelBuilderScene
        deja editar grid
        pinta terreno
        cambia altura
        coloca obstaculos
        coloca unidades
        coloca interactables
        define objetivos
        guarda JSON

    Store / IAP
        maneja gemas
        anuncios
        compras
        skins
        analytics
```

## Indice de Sistemas

```text
1. Sistema de escenas y navegacion
2. Sistema de grid, tiles, terreno y altura
3. Sistema de pathfinding y rango
4. Sistema de unidades, stats, combate y acciones
5. Sistema de turnos
6. Sistema de enemigos e IA
7. Sistema de vision, esconderse y barriles
8. Sistema de objetivos y condiciones de victoria
9. Sistema de level builder
10. Sistema de guardado/carga JSON
11. Sistema de carga runtime de niveles
12. Sistema de UI de batalla y navegacion con control
13. Sistema de tienda, IAP, anuncios y analytics
14. Sistemas de debug y editor
```

---

# 1. Sistema de Escenas y Navegacion

## Ramas

```text
Escenas
    MainMenu.unity
    LevelPicker.unity
    BattleScene.unity
    LevelBuilderScene.unity
    StoreScene / IAP Scene

Scripts
    MainMenuManager
    SceneNavigationButton
    LevelPickerUIController
    SelectedBattleLevel
    MenuCameraController
    SettingsManager
```

## Logica General

```pseudocode
AL ABRIR EL JUEGO:
    mostrar menu principal

SI jugador pulsa jugar:
    cargar LevelPicker

EN LevelPicker:
    buscar archivos JSON en Resources/LevelLayouts
    crear botones para cada nivel

SI jugador elige un nivel:
    SelectedBattleLevel.LevelFileName = nombre del nivel
    cargar BattleScene

EN BattleScene:
    BattleSceneLevelLoader lee SelectedBattleLevel
    si no hay nivel elegido:
        usar fallbackLevelFileName
    cargar ese JSON
```

## Scripts

### `SceneNavigationButton`

```pseudocode
OnButtonClicked(sceneName):
    SceneManager.LoadScene(sceneName)
```

Es un boton generico para cambiar escenas.

### `SelectedBattleLevel`

```pseudocode
static LevelFileName

Set(levelName):
    LevelFileName = levelName

Clear():
    LevelFileName = ""
```

Funciona como memoria temporal global: el LevelPicker guarda aqui el nombre del nivel que BattleScene debe cargar.

### `LevelPickerUIController`

```pseudocode
Start:
    cargar todos los TextAsset desde Resources/LevelLayouts
    por cada nivel:
        crear boton
        poner texto del nombre
        conectar click

OnLevelButtonClicked(levelName):
    SelectedBattleLevel.Set(levelName)
    cargar BattleScene
```

---

# 2. Sistema de Grid, Tiles, Terreno y Altura

## Ramas

```text
Grid
    GridManager
    GridTile

Terreno
    TileManager
    TerrainTypeData

Altura
    TileElevation

Debug de terreno
    GridDebugPainter
```

## Funcion del Sistema

Este sistema crea el tablero y define que significa cada celda:

```text
GridManager = crea y guarda todos los tiles.
GridTile = representa una celda individual.
TileManager = carga datos de terreno desde Resources.
TerrainTypeData = define costo, color, material, dano y caminabilidad.
TileElevation = cambia la altura fisica/visual de un tile.
```

## Flujo al Crear el Grid

```pseudocode
GridManager.Awake:
    GenerateGrid()

GenerateGrid:
    grid = matriz [width, height]

    for x desde 0 hasta width:
        for y desde 0 hasta height:
            worldPosition = (x * tileSpacing, 0, y * tileSpacing)
            tile = Instantiate(tilePrefab, worldPosition)
            tile.Initialize(x, y, tileManager)
            grid[x, y] = tile
```

## Flujo para Consultar Tiles

```pseudocode
IsInsideGrid(pos):
    return pos.x dentro de ancho AND pos.y dentro de alto

GetTileAt(pos):
    if pos fuera del grid:
        return null
    return grid[pos.x, pos.y]

GetNeighbors(tile):
    revisar arriba, derecha, abajo, izquierda
    devolver tiles validos

CanUnitEnterTile(tile):
    if tile == null:
        return false
    if tile no es caminable:
        return false
    if tile esta ocupado:
        return false
    return true
```

## `GridTile`

Cada tile guarda:

```text
coordenadas:
    X, Y, GridPosition

terreno:
    TerrainType
    CurrentTerrainData
    isWalkable
    movementCost

ocupacion:
    isOccupied
    OccupyingUnit

visuales:
    renderer superior
    renderer lateral
    overlay de highlight
    decoracion
```

Pseudocodigo:

```pseudocode
Initialize(x, y, tileManager):
    X = x
    Y = y
    guardar referencia a tileManager
    ApplyTerrainSettings()

ApplyTerrainSettings:
    data = tileManager.GetTerrainData(terrainType)
    CurrentTerrainData = data

    if data existe:
        isWalkable = data.IsWalkable
        movementCost = data.MovementCost
        aplicar color/material
        crear decoracion si existe

SetOccupant(unitObject):
    occupyingUnit = unitObject
    isOccupied = unitObject != null

ShowOverlayColor(color):
    activar overlay
    pintar overlay

ClearOverlay:
    apagar overlay
```

## `TerrainTypeData`

Es un `ScriptableObject`. No ejecuta gameplay por si solo; guarda datos.

```pseudocode
TerrainTypeData:
    TerrainType
    MovementCost
    IsWalkable
    DamageOnEnter
    DamageOnStop
    MovementPenaltyOnEnter
    MovementPenaltyOnStop
    Color
    TopMaterial
    SideMaterial
    DecorationPrefab
```

Se usa asi:

```pseudocode
GridTile.ApplyTerrainSettings:
    pedir data a TileManager
    copiar valores importantes al tile
```

## `TileManager`

```pseudocode
Awake:
    terrainDataList = Resources.LoadAll("TerrainTypes")
    por cada TerrainTypeData:
        dictionary[data.TerrainType] = data

GetTerrainData(type):
    if dictionary contiene type:
        return dictionary[type]
    return null
```

## `TileElevation`

Controla altura visual y fisica del tile.

```pseudocode
SetElevation(value):
    elevation = max(0, value)
    ApplyElevation()

ApplyElevation:
    alturaMundo = elevation * stepHeight
    mover superficie superior hacia arriba
    escalar columna lateral
    mover overlay arriba de la superficie
    ajustar collider
    mover anchor de decoraciones
    ajustar tiling de textura lateral
```

Impacto en gameplay:

```pseudocode
Pathfinding / RangeFinder:
    diferenciaAltura = abs(tileA.elevation - tileB.elevation)
    if diferenciaAltura > unit.MaxClimbHeight:
        no puede pasar
```

---

# 3. Sistema de Pathfinding y Rango

## Ramas

```text
Pathfinding exacto
    AStarPathFinder
    PathNode

Rango posible
    GridRangeFinder
```

## Funcion

```text
AStarPathFinder = encuentra un camino desde tile A hasta tile B.
GridRangeFinder = calcula todos los tiles que la unidad puede alcanzar con sus puntos de movimiento.
```

## `PathNode`

```pseudocode
PathNode:
    tile
    parent
    gCost = costo desde inicio
    hCost = estimado hasta destino
    fCost = gCost + hCost
```

## `AStarPathFinder`

```pseudocode
FindPath(startTile, targetTile, unit):
    if start o target invalidos:
        return lista vacia

    openSet = nodos por revisar
    closedSet = nodos ya revisados

    crear nodo inicial
    agregar a openSet

    while openSet no esta vacio:
        current = nodo con menor fCost

        if current.tile == targetTile:
            return reconstruir camino desde parents

        mover current de openSet a closedSet

        for neighbor in GridManager.GetNeighbors(current.tile):
            if neighbor ya revisado:
                continuar

            if neighbor no es valido para unidad:
                continuar

            nuevoCosto = current.gCost + costoDeMoverseAlNeighbor

            if neighbor no esta en openSet OR nuevoCosto es mejor:
                actualizar parent, gCost, hCost
                agregar/actualizar en openSet

    return lista vacia
```

Reglas que revisa:

```pseudocode
Tile valido si:
    existe
    es caminable
    no esta ocupado, excepto si es el destino permitido
    diferencia de altura <= MaxClimbHeight de la unidad
```

## `GridRangeFinder`

```pseudocode
GetReachableTiles(startTile, movementPoints, unit):
    reachable = diccionario tile -> costo acumulado
    frontier = cola

    agregar startTile con costo 0

    while frontier no vacia:
        current = sacar cola

        for neighbor in GridManager.GetNeighbors(current):
            if neighbor no es valido:
                continuar

            costoNuevo = costoActual + unit.GetMovementCostForTile(neighbor)

            if costoNuevo > movementPoints:
                continuar

            if neighbor no existe en reachable OR costoNuevo es menor:
                reachable[neighbor] = costoNuevo
                agregar neighbor a frontier

    return reachable
```

Se usa principalmente por `TileSelector` para mostrar los tiles donde la unidad puede moverse.

---

# 4. Sistema de Unidades, Stats, Combate y Acciones

## Ramas

```text
Datos
    UnitData
    UnitAbilityData
    UnitTurnRulesData
    UnitSpawnEntry
    UnitTeam
    AttackType
    ElementType
    UnitRole
    AIType

Unidad runtime
    GridUnit
    UnitSpawner
    EnemySpawner

Acciones de jugador
    TileSelector
    UnitActionMenuController

Visuales
    WorldHealthBar
    AttackEffectData
```

## `UnitData`

Es el archivo de datos de cada tipo de unidad.

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
    puede esconderse en barril
    puede backstab
    puede push
    peso para push
```

## `GridUnit`

Es el script central de unidad.

Guarda:

```text
team
unitData
currentTile
currentHP
remainingMovementPoints
hasMovedThisTurn
attacksUsedThisTurn
isMoving
turnRules
healthBar
hidden state
```

### Colocacion

```pseudocode
PlaceOnTile(tile):
    if currentTile existe:
        currentTile.SetOccupant(null)

    currentTile = tile
    tile.SetOccupant(gameObject)
    transform.position = posicion encima del tile

    if unidad esta en barril:
        actualizar tile del barril
        refrescar estado oculto
```

### Movimiento Normal

```pseudocode
TryMove(path):
    if no puede mover este turno:
        return false
    if path vacio:
        return false
    if ya se esta moviendo:
        return false
    if path[0] != currentTile:
        return false

    destination = ultimo tile del path
    if destination ocupado por otro:
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
MoveAlongPath(path):
    TurnManager.SetBusy()
    StartCoroutine(MoveRoutine(path))
```

```pseudocode
MoveRoutine(path):
    isMoving = true
    currentTile.SetOccupant(null)

    for cada tile del path despues del inicial:
        mover transform hasta posicion del tile
        rotar visual hacia direccion de movimiento
        aplicar efectos de terreno al entrar
        actualizar barril si lo lleva
        avisar a enemigos si jugador visible se movio

    currentTile.SetOccupant(gameObject)
    isMoving = false

    si unidad es Player:
        TurnManager.ReturnToPlayerControl()

    disparar OnMovementFinished
```

### Ataque

```pseudocode
CanAttack(target):
    if target null:
        return false
    if target == this:
        return false
    if target.Team == Team:
        return false
    return IsTargetInRange(target)
```

```pseudocode
TryAttack(target):
    if !CanAttackThisTurn():
        return false
    if !CanAttack(target):
        return false

    isBackstab = CanPerformBackstabOn(target)
    FaceTarget(target)
    ShowAttackEffect(target)

    damage = CalculateAttackDamage(target, isBackstab)
    target.TakeDamage(damage)

    if atacante estaba oculto:
        revelar atacante

    if jugador golpeo enemigo:
        EnemyController.NotifyEnemyHitByAttacker(target, this)

    MarkAttackedThisTurn()
    return true
```

### Recibir Dano y Morir

```pseudocode
TakeDamage(amount):
    if unidad muerta:
        return

    if esta dentro de barril:
        if barril absorbe golpe:
            return

    if esta oculto:
        revelar

    currentHP -= amount
    currentHP = max(currentHP, 0)

    if currentHP <= 0:
        Die()
```

```pseudocode
Die:
    currentTile.SetOccupant(null)
    gameObject.SetActive(false)
    BattleStateManager.NotifyUnitDied(this)
```

### Push

```pseudocode
CanPush(target, gridManager):
    if no puede atacar este turno:
        return false
    if unidad no tiene habilidad push:
        return false
    if target invalido / muerto / aliado:
        return false
    if target no puede ser empujado:
        return false
    if peso no permite empujar:
        return false

    direccion = target.tile - my.tile
    destino = calcular tile final segun distancia push

    return destino existe
```

```pseudocode
TryPush(target):
    if !CanPush:
        return false

    path = construir path recto desde target hasta destino
    target.ForceMoveAlongPath(path)
    MarkAttackedThisTurn()
    avisar a enemigos si aplica
```

### Reglas de Turno de Unidad

```pseudocode
CanMoveThisTurn:
    if muerto:
        return false
    if remainingMovementPoints <= 0:
        return false
    if ya ataco:
        return turnRules.CanMoveAfterAttacking
    return true
```

```pseudocode
CanAttackThisTurn:
    if muerto:
        return false
    if attacksUsedThisTurn >= MaxAttacksPerTurn:
        return false
    if ya movio:
        return turnRules.CanAttackAfterMoving
    return true
```

---

# 5. Sistema de Turnos

## Ramas

```text
Estado global
    TurnManager
    TurnState

Snapshots
    UnitTurnSnapshot

Velocidad de enemigo
    EnemyTurnSpeedMode
```

## Estados

```pseudocode
TurnState:
    PlayerTurn
    EnemyTurn
    Busy
```

`Busy` se usa para bloquear input mientras una unidad se mueve o una accion esta en proceso.

## Flujo del TurnManager

```pseudocode
Awake:
    Instance = this
    CurrentTurn = PlayerTurn
    remainingRestartTurnUses = maxRestartTurnUses
    refrescar UI

Start:
    CapturePlayerTurnSnapshot()
```

## Turno del Jugador

```pseudocode
StartPlayerTurn:
    CurrentTurn = PlayerTurn
    refrescar UI
    limpiar hint

    for cada GridUnit:
        if unit.Team == Player:
            unit.ResetTurnState()
            unit.ApplyTerrainStartTurnEffects()

    EnemyVisionDetector.RefreshAllHiddenStates()
    CapturePlayerTurnSnapshot()
    objectiveRuntimeManager.OnPlayerTurnStarted()
```

## Turno del Enemigo

```pseudocode
StartEnemyTurn:
    TileSelector.ForceClearSelectionAndHighlights()
    CurrentTurn = EnemyTurn
    refrescar UI

    for cada enemigo:
        ResetTurnState()
        ApplyTerrainStartTurnEffects()

    StartCoroutine(RunEnemyTurnRoutine())
```

```pseudocode
RunEnemyTurnRoutine:
    CurrentTurn = Busy
    esperar delay inicial

    enemies = GetLivingEnemies()

    for cada enemigo:
        controller = enemy.GetComponent(EnemyController)
        target = GetBestTargetForEnemy(enemy)

        acted = controller.TryTakeTurn(target)

        if no actuo y no tiene informacion de target:
            saltar enemigo

        if se movio:
            esperar hasta que termine movimiento
            refrescar vision

        if ejecuto animacion/accion:
            esperar hasta que termine o timeout
            refrescar vision

    EnemyVisionDetector.RefreshAllHiddenStates()
    StartPlayerTurn()
```

## Cambiar Turno

```pseudocode
EndTurn:
    if CurrentTurn == PlayerTurn:
        objectiveRuntimeManager.OnPlayerTurnEnded()
        if batalla no termino:
            StartEnemyTurn()

    else if CurrentTurn == EnemyTurn:
        StartPlayerTurn()
```

## Reiniciar Turno del Jugador

```pseudocode
CapturePlayerTurnSnapshot:
    limpiar lista
    for cada unidad:
        guardar:
            referencia unidad
            posicion grid
            HP
            si estaba muerta
            si movio
            si ataco
            ataques usados
            movement points restantes
            rotacion visual
```

```pseudocode
RestartPlayerTurn:
    if no es PlayerTurn o esta Busy:
        return
    if no quedan usos:
        return
    if no hay snapshot:
        return

    RestorePlayerTurnSnapshot()
    remainingRestartTurnUses--
    BattleStateManager.ResetBattleState()
    TileSelector.ForceClearSelectionAndHighlights()
```

---

# 6. Sistema de Enemigos e IA

## Ramas

```text
Spawner
    EnemySpawner

Control principal
    EnemyController

Vision
    EnemyVisionDetector

Estados / comportamiento
    EnemyAIState
    EnemyAIBehavior

Feedback visual
    EnemyStateFeedbackController
    EnemyStateFeedbackData
```

## Estados de IA

```pseudocode
EnemyAIState:
    Idle
    Patrol
    Alert
    Investigate
    Combat
    SearchBarrels
    ReturnToPost
```

## Comportamientos configurables

```pseudocode
EnemyAIBehavior:
    Static
    RandomLook
    Patrol
```

## `EnemyController`

Guarda:

```text
controlledUnit
pathFinder
gridManager
vision detector
currentState
configuredBehavior
patrol start/end
home post
target recordado
ultimo tile conocido
memoria de barriles
```

## Flujo de Turno del Enemigo

```pseudocode
TryTakeTurn(visibleTarget):
    if no tiene home post:
        CaptureHomePost()

    RefreshBarrelLayoutMemory()

    nextState = ResolveTurnState(visibleTarget)
    SetState(nextState)

    switch currentState:
        Combat:
            if visibleTarget existe:
                return TryAct(visibleTarget)
            else:
                SetState(Investigate)
                return TryInvestigate()

        SearchBarrels:
            barrel = GetPriorityVisibleBarrelTarget()
            if barrel existe:
                return TrySearchVisibleBarrels(barrel)
            else:
                SetState(Investigate)
                return TryInvestigate()

        Investigate:
            return TryInvestigate()

        Alert:
            if hay punto de investigacion:
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

## Resolver Estado

```pseudocode
ResolveTurnState(visibleTarget):
    if visibleTarget existe:
        return Combat

    if ve barril sospechoso:
        return SearchBarrels

    if tiene ultimo punto conocido / investigacion:
        return Investigate

    if debe volver al puesto:
        return ReturnToPost

    return estado calmado:
        Patrol si tiene ruta
        Idle si no
```

## Accion Basica del Enemigo

```pseudocode
TryAct(target):
    if controlledUnit puede atacar target:
        atacar
        LastActionWasMovement = false
        return true

    if puede hacer push y conviene:
        ejecutar push
        return true

    path = buscar camino hacia una posicion util cerca del target

    if path encontrado:
        mover enemigo
        LastActionWasMovement = true
        return true

    return false
```

## Comunicacion con Otros Sistemas

```pseudocode
GridUnit.TryAttack:
    si jugador golpea enemigo:
        EnemyController.NotifyEnemyHitByAttacker(enemy, player)

GridUnit.MoveRoutine:
    si jugador visible se mueve:
        EnemyController.NotifyEnemiesOfVisiblePlayer(player)

EnemyVisionDetector:
    cuando ve jugador:
        LevelObjectiveRuntimeManager.NotifyPlayerSeen()
        EnemyController.RememberTarget()
```

---

# 7. Sistema de Vision, Esconderse y Barriles

## Ramas

```text
Ocultamiento
    HiddenStateComponent

Vision enemiga
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

## Concepto

El jugador puede interactuar con barriles. Un barril puede ocultar una unidad, absorber un golpe o volverse sospechoso para enemigos.

## `HiddenStateComponent`

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

## `BarrelInteractable`

```pseudocode
CanUnitHideHere(unit):
    if barrel ocupado:
        return false
    if unit no puede esconderse:
        return false
    return true

TryHideUnit(unit):
    if !CanUnitHideHere:
        return false
    guardar unidad oculta
    activar HiddenStateComponent
    cambiar visuals si aplica
    return true

TryAbsorbHitAndBreak(unit):
    si barril puede absorber:
        romper barril
        revelar unidad
        return true
    return false
```

## `EnemyVisionDetector`

```pseudocode
CanSeeUnit(unit):
    if unit null / muerto:
        return false
    if fuera de rango:
        return false
    if fuera del angulo de vision:
        return false
    if linea de vision bloqueada:
        return false
    if unit esta oculta:
        return false
    return true
```

```pseudocode
RefreshAllHiddenStates:
    for cada unidad oculta:
        revisar si enemigos ven el barril
        actualizar si sigue oculta o se vuelve sospechosa
```

## `InteractablePlacementService`

```pseudocode
TryPlaceInteractable(data, tile, rotation):
    if tile invalido:
        return false
    if ya hay interactable:
        return false
    instanciar prefab
    registrar PlacedInteractable
    return true

GetPlacedInteractableAtTile(tile):
    buscar interactable registrado en ese tile

ClearAllInteractables:
    destruir instancias
    limpiar registro
```

---

# 8. Sistema de Objetivos y Condiciones de Victoria

## Ramas

```text
Datos de objetivo
    LevelObjectiveData
    WinConditionType
    ObjectiveLayoutData
    ObjectiveTargetTileData

Builder UI
    BuilderObjectiveUIController
    LevelObjectiveRegistry

Runtime
    LevelObjectiveRuntimeManager
    ReachTileMarkerData
```

## Tipos de Victoria

```pseudocode
WinConditionType:
    KillAllEnemies
    SurviveTurns
    ReachTile
    ReachWithoutBeingSeen
    InteractWithObject
```

## Runtime de Objetivos

```pseudocode
InitializeObjectives(objectives):
    activeObjectives = copia de objectives
    currentRoundCount = 0
    playerWasSeen = false
    objectivesInitialized = true
    RebuildReachTileMarkers()
```

## Evaluacion

```pseudocode
EvaluateBattleObjectives:
    if no inicializado:
        return
    if batalla ya termino:
        return

    if todos los enemigos derrotados:
        ClearPlayerSeenState()

    if no quedan jugadores vivos:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if objetivo reach es imposible porque murieron jugadores:
        BattleStateManager.EndBattleExternally("You Lose")
        return

    if no todos los objetivos requeridos estan completos:
        return

    BattleStateManager.EndBattleExternally("You Win")
```

## Completar Objetivos

```pseudocode
IsObjectiveComplete(objective):
    switch objective.winConditionType:
        KillAllEnemies:
            return AreAllEnemiesDefeated()

        SurviveTurns:
            return currentRoundCount >= surviveTurnCount

        ReachTile:
            return todos los jugadores vivos estan en tiles objetivo

        ReachWithoutBeingSeen:
            if playerWasSeen:
                return false
            return todos los jugadores vivos estan en tiles objetivo

        InteractWithObject:
            return false // todavia no implementado completo
```

## Ser Visto

```pseudocode
EnemyVisionDetector detecta jugador:
    LevelObjectiveRuntimeManager.NotifyPlayerSeen(player)

MarkPlayerSeen(player):
    if ya fue visto:
        return
    playerWasSeen = true
    firstSeenPlayerUnit = player

    if loseWhenSeen:
        BattleStateManager.EndBattleExternally("You Lose")
```

---

# 9. Sistema de Level Builder

## Ramas

```text
Estado de herramienta
    BuilderStateController
    BuilderToolMode
    BuilderUnitPaintTeam

Input de edicion
    BuilderInputController
    BuilderCameraController

UI
    BuilderUIController
    BuilderObstaclePreview
    BuilderObjectiveUIController

Colocacion de unidades
    UnitPlacementService
    BuilderUnitRegistry
    PlacedBuilderUnit

Obstaculos
    ObstacleManager
    ObstacleData
    PlacedObstacle
    PlaceableAnchor

Guardado
    BuilderSaveLoadManager
```

## Modos de Herramienta

```pseudocode
BuilderToolMode:
    TerrainPaint
    ElevationPaint
    ObstaclePaint
    UnitPaint
    InteractablePaint
    ObjectivePaint
    Erase
```

## `BuilderStateController`

Guarda que herramienta y recurso estan seleccionados.

```pseudocode
Awake:
    LoadAssetsFromResources()
    ClampSelectionIndices()

LoadAssetsFromResources:
    loadedTerrainTypes = Resources.LoadAll("TerrainTypes")
    loadedObstacleTypes = Resources.LoadAll("ObstacleTypes")
    loadedInteractableTypes = Resources.LoadAll("InteractableData")
    units = Resources.LoadAll("UnitData/Player" o "UnitData/Enemy")
```

```pseudocode
SetToolMode(mode):
    currentToolMode = mode

SetBrushSize(size):
    brushSize = max(1, size)

SetSelectedElevationValue(value):
    selectedElevationValue = max(0, value)

CycleUnitPaintTeam:
    si Player -> Enemy
    si Enemy -> Player
    reset unitIndex
```

## `BuilderInputController`

Es el puente entre el mouse/controller y los cambios reales en el mapa.

```pseudocode
Update:
    if UI bloquea interaccion:
        limpiar hover
        return

    if esta rotando placement:
        mantener hover en tile anchor
    else:
        UpdateHoveredTile()

    if picking objective tile:
        mostrar highlight especial
    else if picking enemy patrol end:
        mostrar highlight de patrol

    UpdatePlacementKeyboardRotation()
```

## Click en Builder

```pseudocode
OnLeftClickStarted:
    if no hay currentHoveredTile:
        return

    if picking objective tile:
        seleccionar tile objetivo
        return

    if picking patrol end:
        seleccionar final de patrol
        return

    switch BuilderStateController.CurrentToolMode:
        TerrainPaint:
            pintar terreno en brush

        ElevationPaint:
            cambiar elevacion en brush

        ObstaclePaint:
            colocar obstaculo seleccionado

        UnitPaint:
            colocar unidad seleccionada

        InteractablePaint:
            colocar interactable seleccionado

        Erase:
            borrar contenido del tile
```

## Brocha

```pseudocode
GetBrushTiles(centerTile, brushSize):
    crear lista
    radio = brushSize
    for offsets alrededor del centro:
        tile = gridManager.GetTileAt(center + offset)
        if tile existe:
            agregar
    return lista
```

## Pintar Terreno

```pseudocode
PaintTerrain(tile):
    tiles = GetBrushTiles(tile, brushSize)
    for each t in tiles:
        if t no esta bloqueado por obstaculo:
            t.TerrainType = selectedTerrain
            t.ApplyTerrainSettings()
```

## Pintar Altura

```pseudocode
PaintElevation(tile):
    tiles = GetBrushTiles(tile, brushSize)
    for each t in tiles:
        elevation = t.GetComponent(TileElevation)
        if elevation existe:
            elevation.SetElevation(selectedElevationValue)
```

## Colocar Obstaculo

```pseudocode
PlaceObstacle(tile):
    selectedObstacle = BuilderStateController.SelectedObstacleData
    rotation = BuilderStateController.SelectedObstacleRotationY
    obstacleManager.TryPlaceObstacle(selectedObstacle, tile.GridPosition, rotation)
```

## Colocar Unidad

```pseudocode
PlaceUnit(tile):
    selectedUnitData = BuilderStateController.SelectedUnitData
    team = BuilderStateController.SelectedUnitPaintTeam
    rotation = BuilderStateController.SelectedUnitRotationY
    behavior = BuilderStateController.SelectedEnemyBehavior

    UnitPlacementService.TryPlaceUnit(...)
    BuilderUnitRegistry registra PlacedBuilderUnit
```

## `UnitPlacementService`

```pseudocode
TryPlaceUnit(unitData, tile, team, rotation, behavior):
    if tile null:
        return false
    if tile ocupado:
        return false
    if unitData o prefab faltan:
        return false

    instancia = Instantiate(unitData.prefab)
    gridUnit = instancia.GetComponent(GridUnit)
    gridUnit.InitializeFromData(unitData)
    gridUnit.PlaceOnTile(tile)

    if team == Enemy:
        configurar EnemyController behavior/patrol

    registrar en BuilderUnitRegistry
    return true
```

## `BuilderUnitRegistry`

```pseudocode
Register(placedUnit):
    agregar a lista

Unregister(placedUnit):
    quitar de lista

GetPlacedUnits:
    devolver lista solo lectura
```

## `ObstacleManager`

```pseudocode
TryPlaceObstacle(obstacleData, origin, rotationY):
    if !CanPlaceObstacle:
        return false

    placedObstacle = nuevo PlacedObstacle
    offsets = GetRotatedFootprintOffsets(footprint, rotation)

    for each offset:
        tile = gridManager.GetTileAt(origin + offset)
        registrar tile como ocupado por este obstaculo

        if obstacle pinta terreno:
            tile.TerrainType = TerrainTypeUnderObstacle
            tile.ApplyTerrainSettings()

        if obstacle bloquea movimiento:
            tile.ForceSetWalkable(false)

    instanciar prefab visual en centro calculado
    agregar a placedObstacles
    return true
```

```pseudocode
CanPlaceObstacle:
    for each tile del footprint rotado:
        if fuera del grid:
            return false
        if ya hay obstaculo:
            return false
        if tile no caminable u ocupado:
            return false
        if elevaciones del footprint no coinciden:
            return false
    return true
```

---

# 10. Sistema de Guardado y Carga JSON

## Ramas

```text
Datos serializables
    LevelLayoutData
    TileLayoutData
    ObstacleLayoutData
    UnitLayoutData
    InteractableLayoutData
    ObjectiveLayoutData

Manager
    BuilderSaveLoadManager
```

## Estructura del JSON

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

```pseudocode
TileLayoutData:
    x
    y
    terrainType
    elevation

ObstacleLayoutData:
    obstacleName
    originX
    originY
    rotationY

UnitLayoutData:
    unitId
    x
    y
    rotationY
    useCardinalFacing
    team
    enemyBehavior
    hasPatrolRoute
    patrolStartX
    patrolStartY
    patrolEndX
    patrolEndY

InteractableLayoutData:
    interactableId
    x
    y
    rotationY

ObjectiveLayoutData:
    winConditionType
    surviveTurnCount
    targetX / targetY
    targetInteractableId
    targetTiles[]
```

## Guardar Nivel

```pseudocode
SaveLevel:
    layoutData = BuildLevelLayoutData()
    json = JsonUtility.ToJson(layoutData, prettyPrint = true)
    File.WriteAllText(SavePath, json)
    AssetDatabase.Refresh()
```

```pseudocode
BuildLevelLayoutData:
    layout.width = gridManager.Width
    layout.height = gridManager.Height

    for cada tile del grid:
        guardar x, y, terrainType, elevation

    for cada obstaculo colocado:
        guardar obstacleName, origin, rotation

    AddPlacedUnitsToLayout(layout)

    layout.interactables = BuildInteractableLayoutData()
    layout.objectives = BuildObjectiveLayoutData()
    layout.loseWhenSeen = objectiveRegistry.LoseWhenSeen

    return layout
```

## Cargar Nivel en Builder

```pseudocode
LoadLevel:
    if archivo no existe:
        warning
        return

    json = File.ReadAllText(SavePath)
    layoutData = JsonUtility.FromJson(json)

    RebuildLevel(layoutData)
```

```pseudocode
RebuildLevel(layoutData):
    ClearCurrentBuilderState()
    gridManager.RebuildGrid(layoutData.width, layoutData.height)

    cargar mapas de assets:
        ObstacleData desde Resources/ObstacleTypes
        UnitData desde Resources/UnitData
        InteractableData desde library

    aplicar elevaciones
    aplicar terrenos
    colocar obstaculos
    colocar interactables
    colocar unidades
    restaurar objetivos
```

---

# 11. Sistema de Carga Runtime de Niveles

## Ramas

```text
BattleSceneLevelLoader
SelectedBattleLevel
Resources/LevelLayouts
```

## Funcion

Este sistema toma un JSON creado por el builder y lo convierte en una batalla jugable.

## Flujo Completo

```pseudocode
BattleSceneLevelLoader.Awake:
    desactivar UnitSpawner viejo si existe
    desactivar EnemySpawner viejo si existe

Start:
    LoadConfiguredLevel()
```

```pseudocode
LoadConfiguredLevel:
    levelFileName = ResolveLevelFileName()

    jsonAsset = Resources.Load("LevelLayouts/" + levelFileName)
    if no existe:
        error
        return

    layoutData = JsonUtility.FromJson(jsonAsset.text)
    RebuildBattleLevel(layoutData)

    battleStateManager.ResetBattleState()
    objectiveRuntimeManager.InitializeObjectives(layoutData.objectives)
    objectiveRuntimeManager.SetLoseWhenSeen(layoutData.loseWhenSeen)
    turnManager.StartPlayerTurn()
    battleStateManager.CheckBattleState()

    if clearSelectedLevelAfterLoad:
        SelectedBattleLevel.Clear()
```

## Reconstruir Batalla

```pseudocode
RebuildBattleLevel(layoutData):
    ClearCurrentBattleState()
    gridManager.RebuildGrid(width, height)

    obstacleMap = Resources.LoadAll("ObstacleTypes")
    playerUnitMap = Resources.LoadAll("UnitData/Player")
    enemyUnitMap = Resources.LoadAll("UnitData/Enemy")

    obstacleCoveredTiles = calcular tiles cubiertos por obstaculos

    ApplyTileElevations(layoutData)
    ApplyTileTerrains(layoutData, obstacleCoveredTiles)
    PlaceObstacles(layoutData, obstacleMap)
    PlaceInteractables(layoutData)
    PlaceUnits(layoutData, playerUnitMap, enemyUnitMap)
```

## Diferencia Entre Builder y Runtime

```text
BuilderSaveLoadManager:
    escribe y lee archivos fisicos dentro de Assets/Resources/LevelLayouts.

BattleSceneLevelLoader:
    no escribe archivos.
    carga TextAsset desde Resources para construir la batalla.
```

---

# 12. Sistema de UI de Batalla y Navegacion

## Ramas

```text
UI batalla
    BattleStateManager
    BattlePauseMenuController
    UnitActionMenuController
    WorldHealthBar
    BillBoard

Input control/UI
    ControllerUINavigationController
    UICanvasSelectionFrame
    UISelectionGridIndicator
    UIWorldSpaceSelectionFrame

Camara
    BattleCameraController
```

## `BattleStateManager`

```pseudocode
CheckBattleState:
    if batalla ya termino:
        return

    if no quedan jugadores vivos:
        ShowResult("You Lose")

    else if no quedan enemigos vivos:
        ShowResult("You Win")
```

```pseudocode
NotifyUnitDied(unit):
    objectiveRuntimeManager.OnUnitDied(unit)
    CheckBattleState()
```

```pseudocode
EndBattleExternally(resultText):
    BattleEnded = true
    mostrar UI de resultado
    bloquear acciones
```

## `TileSelector` Como UI de Acciones

`TileSelector` no solo detecta tiles. Tambien controla el menu de acciones de la unidad.

```pseudocode
Al seleccionar unidad:
    ShowUnitActionMenu()

ShowUnitActionMenu:
    actionMenu.ShowForUnit(
        posicion de unidad,
        puede mover,
        puede atacar,
        puede push,
        tiene barril,
        callbacks:
            Move -> pendingActionMode = Move
            Attack -> pendingActionMode = Attack
            Push -> pendingActionMode = Push
            RemoveBarrel -> ExecuteRemoveBarrel
            Cancel -> DeselectUnit
    )
```

## `UnitActionMenuController`

```pseudocode
ShowForUnit(worldPosition, canMove, canAttack, canPush, hasBarrel, callbacks):
    posicionar menu cerca de unidad
    activar/desactivar botones segun acciones disponibles
    conectar callbacks
    mostrar menu

Hide:
    ocultar menu
```

## `WorldHealthBar`

```pseudocode
Initialize(unit):
    guardar referencia
    actualizar barra

Update:
    mirar hacia camara si aplica
    actualizar fill segun unit.CurrentHP / unit.MaxHP
```

---

# 13. Sistema de Tienda, IAP, Anuncios y Analytics

## Ramas

```text
Servicios
    ConsentGateController
    AnalyticsManager
    AdInitializer
    AdManager

Compras / recompensas
    PurchaseFufillment
    BoughtGemsEvent
    GemAdViewedEvent
    GemClickedEvent

Tienda
    SkinData
    SkinStoreController
    AssetBundleLoader

Utilidades
    EnableDisable
    ProfilerExample
    UpgradeButtonAudioController
```

## Flujo General

```pseudocode
Al entrar a tienda/IAP:
    ConsentGateController revisa consentimiento
    AnalyticsManager inicializa Unity Services
    AdInitializer prepara Ads

Si jugador ve anuncio:
    AdManager.ShowRewardedAd()
    cuando termina:
        PurchaseFufillment entrega gemas
        Analytics registra GemAdViewedEvent

Si jugador compra gemas:
    PurchaseFufillment procesa compra
    sumar gemas
    Analytics registra BoughtGemsEvent

Si jugador compra skin:
    SkinStoreController revisa gemas
    descontar precio
    marcar skin como comprada
    permitir equipar
```

## `SkinStoreController`

```pseudocode
Start:
    cargar lista de SkinData
    cargar progreso guardado
    refrescar UI

TryBuySkin(skin):
    if ya comprada:
        equipar
        return
    if gemas insuficientes:
        mostrar error
        return
    restar gemas
    marcar comprada
    guardar
    refrescar UI
```

---

# 14. Sistemas de Debug y Editor

## Ramas

```text
Debug runtime
    DebugModeSwitcher
    GridDebugPainter
    ObstacleDebugPainter

Editor
    BuildGemAssetBundles
```

## `DebugModeSwitcher`

```pseudocode
Update:
    leer input de debug
    cambiar modo activo:
        ninguno
        pintar terreno
        pintar obstaculos

Al cambiar modo:
    activar/desactivar componentes debug correspondientes
```

## `GridDebugPainter`

```pseudocode
OnPaintClick:
    detectar tile bajo cursor
    cambiar terrainType
    ApplyTerrainSettings()
```

## `ObstacleDebugPainter`

```pseudocode
OnPaintClick:
    detectar tile
    obstacleManager.TryPlaceObstacle(...)

OnEraseClick:
    detectar tile
    obstacleManager.TryRemoveObstacleAtTile(tilePosition)
```

---

# Flujo Principal de Combate Completo

```pseudocode
BattleScene inicia

BattleSceneLevelLoader:
    elegir JSON
    reconstruir grid
    aplicar tiles
    colocar obstaculos
    colocar interactables
    colocar unidades
    inicializar objetivos
    StartPlayerTurn()

TurnManager.StartPlayerTurn:
    resetear unidades player
    aplicar terreno
    refrescar vision
    guardar snapshot

Jugador selecciona unidad:
    TileSelector detecta click
    si tile tiene unidad player:
        SelectUnit(unit)
        ShowUnitActionMenu()

Jugador elige Move:
    pendingActionMode = Move
    TileSelector muestra tiles alcanzables
    jugador elige destino
    AStarPathFinder calcula path
    GridUnit.TryMove(path)
    TurnManager pasa a Busy
    unidad se mueve
    TurnManager vuelve a PlayerTurn

Jugador elige Attack:
    pendingActionMode = Attack
    jugador elige enemigo
    GridUnit.TryAttack(enemy)
    enemigo recibe dano
    si muere:
        BattleStateManager.NotifyUnitDied()
        ObjectiveRuntimeManager evalua

Jugador termina turno:
    TurnManager.EndTurn()
    ObjectiveRuntimeManager.OnPlayerTurnEnded()
    StartEnemyTurn()

Turno enemigo:
    por cada enemigo vivo:
        EnemyVisionDetector busca target
        EnemyController.TryTakeTurn(target)
        enemigo ataca, empuja, investiga, patrulla o se mueve

Al terminar enemigos:
    StartPlayerTurn()

Batalla termina si:
    no hay jugadores vivos -> You Lose
    objetivos completos -> You Win
    loseWhenSeen y jugador visto -> You Lose
```

---

# Flujo Principal del Level Builder Completo

```pseudocode
LevelBuilderScene inicia

GridManager:
    crea grid editable

BuilderStateController:
    carga terrain types
    carga obstacle types
    carga interactables
    carga unit data

BuilderUIController:
    conecta botones/sliders/dropdowns con BuilderStateController

Jugador selecciona herramienta:
    Terrain / Elevation / Obstacle / Unit / Interactable / Objective / Erase

BuilderInputController:
    lee mouse
    detecta tile bajo cursor
    muestra hover

Si herramienta Terrain:
    pinta TerrainType en tiles del brush

Si herramienta Elevation:
    llama TileElevation.SetElevation

Si herramienta Obstacle:
    ObstacleManager.TryPlaceObstacle

Si herramienta Unit:
    UnitPlacementService.TryPlaceUnit
    BuilderUnitRegistry registra

Si herramienta Interactable:
    InteractablePlacementService.TryPlaceInteractable

Si herramienta Objective:
    BuilderObjectiveUIController crea ObjectiveLayoutData
    LevelObjectiveRegistry guarda objetivos

Si Save:
    BuilderSaveLoadManager.BuildLevelLayoutData
    convertir a JSON
    escribir en Resources/LevelLayouts

Si Load:
    leer JSON
    gridManager.RebuildGrid
    reconstruir todo
```

---

# Dependencias Importantes

```text
GridManager
    lo usan:
        GridRangeFinder
        AStarPathFinder
        TileSelector
        UnitSpawner
        EnemySpawner
        ObstacleManager
        BuilderInputController
        BuilderSaveLoadManager
        BattleSceneLevelLoader
        LevelObjectiveRuntimeManager

GridTile
    lo usan:
        GridUnit
        TileSelector
        ObstacleManager
        InteractablePlacementService
        Pathfinding
        Builder

GridUnit
    lo usan:
        TileSelector
        TurnManager
        EnemyController
        EnemyVisionDetector
        BattleStateManager
        LevelObjectiveRuntimeManager

TurnManager
    coordina:
        TileSelector
        GridUnit
        EnemyController
        ObjectiveRuntimeManager

BuilderSaveLoadManager
    depende de:
        GridManager
        ObstacleManager
        BuilderUnitRegistry
        InteractablePlacementService
        LevelObjectiveRegistry

BattleSceneLevelLoader
    depende de:
        JSON guardado por BuilderSaveLoadManager
        GridManager
        ObstacleManager
        UnitData
        InteractableData
        ObjectiveRuntimeManager
```

---

# Apendice: Inventario Completo de Scripts

Este apendice existe para confirmar que el documento cubre todos los scripts `.cs` encontrados en el proyecto. Los scripts principales estan explicados en los sistemas anteriores; aqui se listan todos con su rol rapido.

## Scripts Propios del Proyecto

### AStarCore

```text
AStarPathFinder
    Busca caminos entre tiles usando A*.

PathNode
    Nodo interno del pathfinding: tile, parent, gCost, hCost, fCost.
```

### Builder

```text
BuilderInputController
    Lee input del builder y aplica la herramienta activa sobre el grid.

BuilderStateController
    Guarda herramienta actual, seleccion de terreno, obstaculo, unidad, interactable, rotacion y brush.

BuilderToolMode
    Enum de modos del builder: terreno, elevacion, obstaculo, unidad, interactable, objetivo, borrar.

BuilderUnitPaintTeam
    Enum para decidir si el builder coloca unidad Player o Enemy.

BuilderUnitRegistry
    Registro de unidades colocadas en el builder para poder guardarlas.

PlacedBuilderUnit
    Datos de una unidad colocada: UnitData, equipo, rotacion, comportamiento enemigo y patrol.

UnitPlacementService
    Servicio que instancia unidades en tiles desde UnitData.

BuilderCameraController
    Controla movimiento, rotacion y zoom de camara en el builder.

BuilderObstaclePreview
    Muestra previsualizacion del obstaculo seleccionado antes de colocarlo.

BuilderUIController
    Conecta botones, sliders, textos y paneles del builder con BuilderStateController y save/load.
```

### Grid, Tiles y Obstaculos

```text
GridManager
    Crea el tablero, guarda matriz de GridTile y entrega vecinos/posiciones.

GridTile
    Representa una celda: terreno, ocupacion, highlights, renderers y datos de gameplay.

GridRangeFinder
    Calcula tiles alcanzables segun movement points, costo, ocupacion y altura.

GridDebugPainter
    Herramienta debug para pintar terrenos en runtime.

TerrainTypeData
    ScriptableObject con datos de terreno: costo, caminabilidad, materiales, dano y penalizaciones.

TileElevation
    Sube/baja visual y fisicamente un tile.

TileManager
    Carga TerrainTypeData desde Resources y los entrega por TerrainType.

TileSelector
    Control principal de input del jugador en batalla: seleccionar, mover, atacar, push e interactuar.

ObstacleData
    ScriptableObject de obstaculos: prefab, footprint, bloqueo, terreno bajo obstaculo y offsets visuales.

ObstacleDebugPainter
    Debug para colocar o borrar obstaculos con input.

ObstacleManager
    Coloca, valida, registra y remueve obstaculos.

PlaceableAnchor
    Punto/ancla usado para colocar visuales o referencias sobre objetos colocables.

PlacedObstacle
    Datos runtime de un obstaculo colocado: asset, instancia, origen, rotacion y tiles ocupados.
```

### IAP, Ads, Analytics y Store

```text
AdInitializer
    Inicializa Unity Ads.

AdManager
    Maneja carga, disponibilidad y reproduccion de anuncios.

AnalyticsManager
    Inicializa Unity Services/Analytics y registra eventos.

AssetBundleLoader
    Carga AssetBundles, usado para assets de tienda o iconos.

ConsentGateController
    Controla consentimiento antes de habilitar servicios monetizados/analytics.

EnableDisable
    Utilidad simple para activar/desactivar objetos.

ProfilerExample
    Script de ejemplo para profiler o pruebas de rendimiento.

PurchaseFufillment
    Entrega recompensas de compras/anuncios, como gemas.

BoughtGemsEvent
    Evento de analytics cuando se compran gemas.

GemAdViewedEvent
    Evento de analytics cuando se ve anuncio por gemas.

GemClickedEvent
    Evento de analytics cuando se interactua con gemas.

SkinData
    Datos de una skin: nombre, precio, icono/asset.

SkinStoreController
    Controla compra, guardado y equipamiento de skins.
```

### JSON y Runtime Level

```text
BuilderSaveLoadManager
    Convierte el estado del builder a JSON y reconstruye niveles desde JSON en el builder.

LevelLayoutData
    Clases serializables del JSON: tiles, obstaculos, unidades, interactables y objetivos.

BattleSceneLevelLoader
    Carga un JSON desde Resources/LevelLayouts y construye la batalla runtime.

LevelPickerUIController
    Lista niveles disponibles y permite elegir uno para BattleScene.

SelectedBattleLevel
    Memoria estatica del nivel seleccionado.
```

### Menu

```text
MainMenuManager
    Manager del menu principal; actualmente funciona como punto de expansion.

MenuCameraController
    Controla camara del menu.

SettingsManager
    Manager de settings; actualmente funciona como punto de expansion.

MenuAmbience
    Controla ambiente/audio del menu.
```

### Objetivos

```text
BuilderObjectiveUIController
    UI del builder para crear, editar y guardar objetivos.

LevelObjectiveRegistry
    Guarda objetivos configurados en el builder y el flag loseWhenSeen.

LevelObjectiveRuntimeManager
    Evalua objetivos durante batalla y dispara victoria/derrota.

ReachTileMarkerData
    Datos visuales para marcadores de tiles objetivo.
```

### Turn Logic y Unidades

```text
BattleCameraController
    Camara de batalla: movimiento, zoom y control de vista.

BattleStateManager
    Revisa estado de batalla y muestra victoria/derrota.

EnemyTurnSpeedMode
    Enum de velocidad del turno enemigo.

TurnManager
    Controla PlayerTurn, EnemyTurn, Busy, snapshots y ejecucion de enemigos.

TurnState
    Enum de estado global de turno.

UnitTurnSnapshot
    Datos guardados para reiniciar el turno del jugador.

HiddenStateComponent
    Estado de ocultamiento de una unidad, especialmente dentro de barriles.

UnitAbilityData
    Datos base para habilidades de unidad.

UnitData
    ScriptableObject principal de stats y capacidades de unidad.

UnitSpawnEntry
    Entrada de spawn: UnitData y posicion/configuracion.

UnitTeam
    Enum Player/Enemy.

UnitTurnRulesData
    Reglas sobre mover despues de atacar o atacar despues de mover.

EnemyAIBehavior
    Enum de comportamiento enemigo: static, random look, patrol.

EnemyAIState
    Enum de estado IA: idle, patrol, alert, investigate, combat, search barrels, return to post.

EnemyController
    Cerebro de enemigo: combate, persecucion, investigacion, patrulla, memoria y acciones.

EnemySpawner
    Spawnea enemigos desde configuracion.

EnemyVisionDetector
    Calcula vision enemiga, rango, angulo, linea de vision y deteccion de ocultos.

BarrelInteractable
    Interactable para esconder unidades y absorber/romperse con golpes.

InteractableData
    ScriptableObject de interactables.

InteractableLibrary
    Biblioteca para encontrar InteractableData por id.

InteractablePlacementService
    Coloca, registra y remueve interactables en tiles.

InteractableRegistry
    Registro runtime de interactables colocados.

InteractableType
    Enum de tipos de interactable.

LevelObjectiveData
    Datos de objetivo usados por interactables/objetivos.

PlacedInteractable
    Datos runtime de un interactable colocado.

WinConditionType
    Enum de condiciones de victoria.

GridUnit
    Unidad runtime: HP, movimiento, ataque, push, terreno, muerte, ocultamiento y turn state.

UnitSpawner
    Spawnea unidades del jugador desde configuracion.

AIType
    Enum de tipo de IA guardado en UnitData.

AttackType
    Enum de tipo de ataque.

ElementType
    Enum elemental de la unidad.

UnitRole
    Enum de rol de unidad.
```

### UI

```text
BattlePauseMenuController
    Menu de pausa en batalla, pausa input y permite navegar/reiniciar.

BillBoard
    Hace que un objeto UI mire hacia la camara.

SceneNavigationButton
    Boton generico para cambiar escenas.

UnitActionMenuController
    Menu flotante de acciones: move, attack, push, remove barrel, cancel.

UpgradeButtonAudioController
    Audio y feedback de botones de upgrade/tienda.

WorldHealthBar
    Barra de vida en mundo para GridUnit.

AttackEffectData
    ScriptableObject de prefab/configuracion visual de ataque.

EnemyStateFeedbackController
    Muestra feedback visual del estado del enemigo.

EnemyStateFeedbackData
    Datos visuales por estado enemigo.

ControllerUINavigationController
    Controla navegacion de UI con gamepad/controller.

UICanvasSelectionFrame
    Marco visual para seleccionar elementos UI en canvas.

UISelectionGridIndicator
    Indicador de seleccion para grids de UI.

UIWorldSpaceSelectionFrame
    Marco visual de seleccion en world-space UI.
```

### Debug

```text
DebugModeSwitcher
    Alterna modos debug y activa/desactiva painters.
```

## Scripts Fuera de `Assets/Scripts`

```text
InputSystem_Actions
    Archivo generado por Unity Input System. Define actions usadas por gameplay, builder, UI y debug.

BuildGemAssetBundles
    Script de editor para construir AssetBundles relacionados con gemas/assets.
```

## Scripts de Paquetes Externos o Tutorial

Estos scripts existen en el proyecto, pero no son la logica principal del juego. Se listan para que el inventario sea completo.

```text
Particlecollection_Free samples / Effect package
    CameraTarget
        Script de camara/target del paquete de efectos.

    EffectController
        Controla reproduccion/configuracion de efectos del paquete externo.

    EffectDemo
        Demo de efectos del paquete externo.

    EffectShaderPropertyStr
        Constantes/nombres de propiedades shader usadas por el paquete.

    RenderEffect
        Render o post/render helper del paquete de efectos.

    TransformExtension
        Metodos extension para transform usados por el paquete.

    EffectControllerInspector
        Inspector custom de editor para EffectController.

    EffectToolBar
        Toolbar de editor del paquete de efectos.

    RenderEffectInspector
        Inspector custom de editor para RenderEffect.

    ShaderMaterialsEditor
        Editor custom de materiales/shaders del paquete.

    XUIUtils
        Utilidades de UI/editor del paquete.

TutorialInfo
    Readme
        Script de datos para readme/tutorial dentro de Unity.

    ReadmeEditor
        Editor custom para mostrar el readme/tutorial en inspector.
```

---

# Apendice: Donde Encaja Cada Script en la Logica

Esta seccion explica cada script como parte del flujo del juego. La forma de leerlo es:

```text
Momento del flujo
    Script
        donde encaja
        que recibe
        que cambia o a quien llama
```

## 1. Inicio, Menu y Seleccion de Nivel

```pseudocode
AL INICIAR EN MENU:
    MainMenuManager
        encaja como manager de la escena de menu
        hoy es mas punto de expansion que logica fuerte

    MenuCameraController
        encaja en la presentacion del menu
        mueve/orienta la camara de menu

    MenuAmbience
        encaja en el ambiente del menu
        reproduce/controla audio ambiental

    SettingsManager
        encaja cuando exista menu de opciones
        hoy queda como punto de expansion para volumen, pantalla, controles

SI EL JUGADOR PRESIONA UN BOTON DE NAVEGACION:
    SceneNavigationButton
        recibe nombre de escena desde el inspector
        llama SceneManager.LoadScene(sceneName)

CUANDO SE ABRE LEVEL PICKER:
    LevelPickerUIController
        busca niveles guardados en Resources/LevelLayouts
        crea botones de nivel
        cuando el jugador elige uno:
            llama SelectedBattleLevel.Set(levelName)
            carga BattleScene

    SelectedBattleLevel
        encaja como memoria estatica entre escenas
        guarda el nombre del nivel que BattleScene debe cargar
```

## 2. Inicio de BattleScene y Construccion del Nivel

```pseudocode
AL CARGAR BattleScene:
    BattleSceneLevelLoader
        encaja antes de empezar combate
        lee SelectedBattleLevel o fallbackLevelFileName
        carga JSON desde Resources/LevelLayouts
        reconstruye grid, terreno, altura, obstaculos, interactables y unidades

    LevelLayoutData
        encaja como formato del JSON
        contiene:
            TileLayoutData
            ObstacleLayoutData
            UnitLayoutData
            InteractableLayoutData
            ObjectiveLayoutData

    GridManager
        encaja como base del mapa
        RebuildGrid(width, height)
        crea todos los GridTile

    GridTile
        encaja como cada celda individual del mapa
        recibe terrainType, elevation, ocupacion y highlights

    TileManager
        encaja cuando GridTile aplica terreno
        entrega TerrainTypeData segun TerrainType

    TerrainTypeData
        encaja como datos configurables del terreno
        define costo, material, dano, penalizacion y caminabilidad

    TileElevation
        encaja cuando JSON trae elevation
        SetElevation cambia la altura visual/fisica del tile

    ObstacleManager
        encaja cuando el loader coloca obstaculos
        recibe ObstacleData + posicion + rotacion
        bloquea tiles y crea prefab visual

    ObstacleData
        encaja como datos configurables de cada obstaculo
        define footprint, prefab, bloqueo y terreno bajo obstaculo

    PlacedObstacle
        encaja como registro runtime de un obstaculo ya colocado
        guarda tiles ocupados, instancia, origen y rotacion

    PlaceableAnchor
        encaja como ancla/punto auxiliar para colocables
        ayuda a ubicar visuales o referencias

    InteractableLibrary
        encaja cuando se necesita buscar interactables por id
        entrega InteractableData

    InteractableData
        encaja como datos configurables de interactables
        define id, prefab, tipo y comportamiento base

    InteractablePlacementService
        encaja al colocar interactables en el grid
        instancia prefab y registra PlacedInteractable

    InteractableRegistry
        encaja como lista/registro de interactables colocados
        permite encontrarlos despues

    InteractableType
        encaja como enum para clasificar interactables

    PlacedInteractable
        encaja como instancia runtime de un interactable colocado
        guarda tile, data e instancia

    UnitData
        encaja al crear unidades desde JSON o spawners
        define stats, prefab, movimiento, ataque, vision, push, backstab

    UnitPlacementService
        encaja en builder y runtime cuando se instancia unidad desde UnitData
        coloca GridUnit sobre GridTile

    GridUnit
        encaja como unidad viva en la batalla
        recibe UnitData
        ocupa un GridTile

    UnitTeam
        encaja para distinguir Player vs Enemy

    EnemyController
        encaja si la unidad creada es Enemy
        recibe comportamiento IA y ruta de patrol

    EnemyAIBehavior
        encaja como configuracion de comportamiento enemigo guardada en JSON

    EnemyAIState
        encaja como estado interno runtime del EnemyController

    BattleStateManager
        encaja despues de construir la escena
        resetea estado y revisa victoria/derrota inicial

    LevelObjectiveRuntimeManager
        encaja despues de cargar JSON
        InitializeObjectives(layoutData.objectives)
        SetLoseWhenSeen(layoutData.loseWhenSeen)

    ReachTileMarkerData
        encaja cuando hay objetivos ReachTile
        define prefab/offset/escala de marcadores

    TurnManager
        encaja al final del setup
        StartPlayerTurn()
```

## 3. Turno del Jugador y Acciones

```pseudocode
CUANDO EMPIEZA PLAYER TURN:
    TurnManager
        cambia CurrentTurn a PlayerTurn
        resetea unidades Player
        aplica efectos de terreno
        toma UnitTurnSnapshot

    TurnState
        encaja como enum:
            PlayerTurn permite input
            EnemyTurn prepara enemigo
            Busy bloquea input

    UnitTurnSnapshot
        encaja cuando se permite RestartPlayerTurn
        guarda HP, posicion, acciones usadas, movement points y rotacion

    UnitTurnRulesData
        encaja al preguntar si la unidad puede mover/atacar
        define CanMoveAfterAttacking y CanAttackAfterMoving

    TileSelector
        encaja durante todo el input del jugador
        detecta hover/click/controller cursor
        selecciona unidad
        muestra menu
        ejecuta move, attack, push o barril

    UnitActionMenuController
        encaja despues de seleccionar una unidad
        muestra botones disponibles segun GridUnit.CanMoveThisTurn / CanAttackThisTurn

    GridRangeFinder
        encaja cuando jugador elige Move
        calcula tiles alcanzables con movement points

    AStarPathFinder
        encaja cuando jugador elige destino
        calcula path exacto hacia el tile

    PathNode
        encaja dentro de AStarPathFinder
        representa nodos abiertos/cerrados del algoritmo

    GridUnit
        encaja cuando se ejecuta accion:
            TryMove(path)
            TryAttack(target)
            TryPush(target)
            TakeDamage(amount)
            Die()

    AttackEffectData
        encaja cuando GridUnit ataca
        define prefab, escala, duracion y animacion visual del ataque

    WorldHealthBar
        encaja como UI pegada a la unidad
        lee CurrentHP/MaxHP de GridUnit

    BillBoard
        encaja para que health bars u otros elementos miren a la camara

    HiddenStateComponent
        encaja si la unidad esta escondida o dentro de barril
        puede revelar al atacar, recibir dano o ser vista

    BarrelInteractable
        encaja si el jugador interactua con barril
        puede ocultar unidad, mover barril con unidad o absorber golpe

    LevelObjectiveRuntimeManager
        encaja cuando termina turno, muere unidad o se evalua objetivo

    WinConditionType
        encaja al evaluar que tipo de objetivo debe cumplirse

    LevelObjectiveData
        encaja como dato de objetivo asociado a interactables/configuracion

    BattleStateManager
        encaja cuando una unidad muere o un objetivo termina
        decide mostrar You Win / You Lose
```

## 4. Turno Enemigo e IA

```pseudocode
CUANDO TERMINA PLAYER TURN:
    TurnManager.EndTurn()
        llama objectiveRuntimeManager.OnPlayerTurnEnded()
        llama StartEnemyTurn()

EN START ENEMY TURN:
    EnemyTurnSpeedMode
        encaja para decidir delay normal, fast o super fast

    TurnManager.RunEnemyTurnRoutine()
        obtiene enemigos vivos
        por cada enemigo:
            obtiene EnemyController
            pregunta mejor target
            llama EnemyController.TryTakeTurn(target)

PARA CADA ENEMIGO:
    EnemyVisionDetector
        encaja antes/durante IA
        calcula si ve jugador o barril
        si ve jugador:
            avisa a EnemyController
            avisa a LevelObjectiveRuntimeManager.NotifyPlayerSeen

    EnemyController
        encaja como cerebro de decision
        ResolveTurnState:
            si ve jugador -> Combat
            si ve barril sospechoso -> SearchBarrels
            si recuerda ultimo punto -> Investigate
            si debe volver -> ReturnToPost
            si tiene ruta -> Patrol
            si no -> Idle

    EnemyAIBehavior
        encaja como personalidad base:
            Static
            RandomLook
            Patrol

    EnemyAIState
        encaja como estado actual:
            Idle, Patrol, Alert, Investigate, Combat, SearchBarrels, ReturnToPost

    EnemyStateFeedbackController
        encaja cuando cambia estado IA
        muestra icono/color/feedback del estado

    EnemyStateFeedbackData
        encaja como configuracion visual por estado enemigo

    AStarPathFinder
        encaja si enemigo necesita moverse hacia target, patrol o investigacion

    GridRangeFinder
        encaja indirectamente para validar movimientos/rangos si se usa en decisiones

    GridUnit
        encaja como cuerpo que ejecuta la decision:
            TryAttack
            TryPush
            TryMove
            ForceMoveAlongPath

    TurnManager
        espera que termine movimiento/animacion
        refresca vision
        pasa al siguiente enemigo

CUANDO TODOS LOS ENEMIGOS TERMINAN:
    TurnManager.StartPlayerTurn()
```

## 5. Builder de Niveles

```pseudocode
AL ABRIR LEVEL BUILDER:
    GridManager
        crea grid editable

    BuilderStateController
        carga assets desde Resources:
            TerrainTypeData
            ObstacleData
            InteractableData
            UnitData Player/Enemy
        guarda herramienta actual y seleccion actual

    BuilderToolMode
        encaja como enum de herramienta activa

    BuilderUnitPaintTeam
        encaja para decidir si se pinta Player o Enemy

    BuilderUIController
        encaja como puente UI -> BuilderStateController
        botones cambian herramienta, brush, rotacion, assets, save/load

    BuilderCameraController
        encaja mientras el usuario edita
        mueve camara del builder

    BuilderObstaclePreview
        encaja al tener herramienta de obstaculo/interactable
        muestra previsualizacion antes de colocar

CUANDO EL USUARIO HACE CLICK EN EL GRID:
    BuilderInputController
        lee currentHoveredTile
        revisa si UI bloquea input
        segun BuilderToolMode:
            TerrainPaint -> cambia GridTile.TerrainType
            ElevationPaint -> llama TileElevation.SetElevation
            ObstaclePaint -> llama ObstacleManager.TryPlaceObstacle
            UnitPaint -> llama UnitPlacementService.TryPlaceUnit
            InteractablePaint -> llama InteractablePlacementService.TryPlaceInteractable
            ObjectivePaint -> ayuda a BuilderObjectiveUIController a elegir tiles
            Erase -> borra unidad/obstaculo/interactable/terreno segun caso

    BuilderUnitRegistry
        encaja cuando se coloca o borra unidad
        registra PlacedBuilderUnit para que SaveLevel pueda encontrarla

    PlacedBuilderUnit
        encaja como metadata de unidad colocada:
            UnitData
            equipo
            rotacion
            uso de facing cardinal
            EnemyAIBehavior
            patrol start/end

    BuilderObjectiveUIController
        encaja cuando se crean objetivos en builder
        permite elegir condicion, turnos, tiles objetivo y loseWhenSeen

    LevelObjectiveRegistry
        encaja como almacenamiento de objetivos del builder
        BuilderSaveLoadManager lo lee al guardar
```

## 6. Guardado y Carga en Builder

```pseudocode
CUANDO EL USUARIO PRESIONA SAVE:
    BuilderSaveLoadManager
        BuildLevelLayoutData()
        lee GridManager.Grid
        por cada GridTile:
            guarda TileLayoutData
        lee ObstacleManager.GetPlacedObstacles()
            guarda ObstacleLayoutData
        lee BuilderUnitRegistry.GetPlacedUnits()
            guarda UnitLayoutData
        lee InteractablePlacementService/Registry
            guarda InteractableLayoutData
        lee LevelObjectiveRegistry
            guarda ObjectiveLayoutData y loseWhenSeen
        escribe JSON en Assets/Resources/LevelLayouts

CUANDO EL USUARIO PRESIONA LOAD EN BUILDER:
    BuilderSaveLoadManager
        lee JSON desde SavePath
        JsonUtility.FromJson(LevelLayoutData)
        ClearCurrentBuilderState()
        GridManager.RebuildGrid()
        reconstruye terreno, altura, obstaculos, unidades, interactables y objetivos
```

## 7. UI, Pausa, Camara y Navegacion con Control

```pseudocode
DURANTE BATALLA:
    BattleCameraController
        encaja como camara tactica de batalla
        permite mover/rotar/zoom

    BattlePauseMenuController
        encaja cuando jugador pausa
        bloquea input de TileSelector
        muestra opciones de pausa

    ControllerUINavigationController
        encaja cuando el usuario navega UI con control
        mueve seleccion entre botones/elementos

    UICanvasSelectionFrame
        encaja como marco visual sobre UI Canvas seleccionada

    UISelectionGridIndicator
        encaja como indicador para grids de botones/opciones

    UIWorldSpaceSelectionFrame
        encaja como marco para UI en world space

    UpgradeButtonAudioController
        encaja en botones de upgrade/tienda
        maneja sonidos y feedback de boton
```

## 8. Store, IAP, Ads y Analytics

```pseudocode
AL ENTRAR A STORE / IAP:
    ConsentGateController
        encaja antes de inicializar servicios
        valida consentimiento del usuario

    AnalyticsManager
        encaja despues del consentimiento
        inicializa analytics y manda eventos

    AdInitializer
        encaja al preparar Unity Ads

    AdManager
        encaja cuando se cargan/muestran anuncios
        al completar rewarded ad:
            llama flujo de recompensa

    PurchaseFufillment
        encaja cuando compra/anuncio se completa
        entrega gemas o recompensa

    BoughtGemsEvent
        encaja cuando una compra de gemas se registra en analytics

    GemAdViewedEvent
        encaja cuando se ve anuncio por gemas

    GemClickedEvent
        encaja cuando se hace click/interaccion con gemas

    SkinStoreController
        encaja como tienda de skins
        revisa gemas, compra, guarda y equipa skins

    SkinData
        encaja como datos de cada skin

    AssetBundleLoader
        encaja si la tienda necesita cargar assets/bundles externos

    EnableDisable
        encaja como utilidad simple para prender/apagar objetos de UI

    ProfilerExample
        encaja como prueba/ejemplo de profiling, no como flujo principal
```

## 9. Debug, Editor, Input Generado y Externos

```pseudocode
DURANTE DEBUG:
    DebugModeSwitcher
        encaja para alternar herramientas debug
        activa GridDebugPainter u ObstacleDebugPainter

    GridDebugPainter
        encaja para probar terreno sin usar builder completo
        click sobre tile -> cambia terrainType

    ObstacleDebugPainter
        encaja para probar obstaculos sin builder completo
        click -> coloca obstaculo
        erase -> remueve obstaculo

EN EDITOR:
    BuildGemAssetBundles
        encaja como herramienta de editor
        construye bundles de assets/gemas

INPUT:
    InputSystem_Actions
        encaja como archivo generado por Unity Input System
        lo usan:
            TileSelector
            TurnManager
            BuilderInputController
            GridDebugPainter
            ObstacleDebugPainter
            UI/controller scripts

PAQUETE DE PARTICULAS:
    CameraTarget
        encaja dentro de escenas/demo de efectos externos

    EffectController
        controla efectos externos

    EffectDemo
        demo para probar efectos del paquete

    EffectShaderPropertyStr
        nombres de propiedades shader del paquete

    RenderEffect
        helper/render script del paquete de efectos

    TransformExtension
        metodos extension para transforms del paquete

    EffectControllerInspector
        inspector custom para EffectController

    EffectToolBar
        toolbar editor del paquete

    RenderEffectInspector
        inspector custom para RenderEffect

    ShaderMaterialsEditor
        editor custom para materiales/shaders

    XUIUtils
        utilidades editor/UI del paquete externo

TUTORIALINFO:
    Readme
        encaja como asset/script de informacion tutorial en Unity

    ReadmeEditor
        encaja como editor custom para mostrar Readme
```

## 10. Enums y Datos Pequenos Dentro de la Logica

```pseudocode
CUANDO UnitData DEFINE IDENTIDAD DE UNIDAD:
    AIType
        clasifica estilo/tipo IA guardado en datos

    AttackType
        clasifica ataque melee/ranged/etc

    ElementType
        clasifica elemento de unidad/ataque

    UnitRole
        clasifica rol de unidad

    UnitAbilityData
        encaja como dato base para habilidades futuras o configurables

    UnitSpawnEntry
        encaja en spawners clasicos
        combina UnitData con posicion/configuracion de spawn

CUANDO SE SPAWNEA SIN JSON:
    UnitSpawner
        encaja en flujo clasico de batalla
        crea unidades Player desde entradas configuradas

    EnemySpawner
        encaja en flujo clasico de batalla
        crea unidades Enemy desde entradas configuradas

NOTA:
    BattleSceneLevelLoader puede desactivar UnitSpawner y EnemySpawner
    cuando se usa carga por JSON para evitar doble spawn.
```

## Verificacion de Cobertura

```text
Revision hecha contra:
    Assets/Scripts/**/*.cs
    Assets/InputSystem_Actions.cs
    Assets/Editor/BuildGemAssetBundles.cs
    Assets/Particlecollection_Free samples/**/*.cs
    Assets/TutorialInfo/**/*.cs

Resultado:
    Todos los nombres de scripts encontrados estan mencionados en este documento.
```

---

# Resumen Mental Para Estudiarlo

```text
El grid es la base.
    Sin GridManager no hay tiles.
    Sin GridTile no hay movimiento, ocupacion ni terreno.

Las unidades viven sobre tiles.
    GridUnit sabe donde esta, cuanto HP tiene y que acciones puede hacer.

TileSelector traduce input del jugador en acciones.
    Seleccionar, mover, atacar, empujar, interactuar.

TurnManager decide cuando se puede actuar.
    PlayerTurn permite input.
    Busy bloquea input.
    EnemyTurn ejecuta IA.

EnemyController decide que hacen los enemigos.
    Combat si ve jugador.
    Investigate si recuerda algo.
    Patrol si tiene ruta.
    Idle si no tiene nada.

El builder crea datos.
    El jugador pinta un nivel.
    BuilderSaveLoadManager lo convierte en JSON.

BattleSceneLevelLoader consume esos datos.
    Lee JSON.
    Reconstruye el nivel.
    Inicia combate.

ObjectiveRuntimeManager decide victoria/derrota especial.
    Kill all enemies.
    Survive turns.
    Reach tile.
    Reach without being seen.
```
