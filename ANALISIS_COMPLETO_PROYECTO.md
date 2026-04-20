# Analisis Completo del Proyecto

## Objetivo de este documento

Este documento explica como esta organizado el proyecto, que hace cada bloque principal, para que sirve cada script y como se conectan entre si. Esta version esta escrita en espanol y pensada para un nivel de lectura universitario.

El proyecto ya no es solo una escena de combate. Ahora tiene varios subsistemas:

- combate tactico por turnos,
- generacion y edicion de grid,
- sistema de alturas,
- level builder,
- guardado y carga en JSON,
- escenas de menu y navegacion,
- tienda, gemas, anuncios y analytics,
- utilidades de UI,
- assets de terceros y scripts de editor.

## Vista General

Las escenas detectadas en el proyecto son:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/HomeScene.unity`
- `Assets/Scenes/BattleScene.unity`
- `Assets/Scenes/LevelBuilderScene.unity`
- `Assets/Scenes/StoreScene.unity`
- `Assets/Scenes/IAP Scene.unity`

Ademas hay escenas de ejemplo del paquete de particulas:

- `Assets/Particlecollection_Free samples/Scene/...`

Eso indica que el proyecto esta dividido en dos grandes areas:

1. `Juego principal`
   - grid, unidades, combate, turnos, IA enemiga.
2. `Herramientas y metajuego`
   - builder, guardado de niveles, menus, tienda, anuncios y analitica.

## Flujo General del Proyecto

### Flujo de combate

1. `GridManager` crea el tablero.
2. `TileManager` entrega la configuracion visual y de gameplay de cada tipo de terreno.
3. `UnitSpawner` y `EnemySpawner` crean las unidades configuradas.
4. `TileSelector` permite seleccionar unidades del jugador, moverlas y atacar.
5. `TurnManager` controla los estados `PlayerTurn`, `EnemyTurn` y `Busy`.
6. `EnemyController` ejecuta la logica de los enemigos durante su turno.
7. `BattleStateManager` detecta victoria o derrota.

### Flujo del level builder

1. `GridManager` crea el tablero editable.
2. `BuilderStateController` guarda la herramienta y el recurso seleccionados.
3. `BuilderInputController` interpreta clicks y pinta terreno, altura, obstaculos o unidades.
4. `ObstacleManager` coloca obstaculos rotables y con footprint.
5. `BuilderUnitRegistry` registra unidades colocadas manualmente.
6. `BuilderSaveLoadManager` exporta e importa layouts JSON.
7. `BuilderUIController` conecta sliders, toggles, inputs y paneles.

### Flujo de tienda e IAP

1. `ConsentGateController` decide si el usuario puede acceder al contenido monetizado.
2. `AnalyticsManager` inicializa Unity Services y registra eventos.
3. `AdInitializer` y `AdManager` preparan anuncios.
4. `PurchaseFufillment` entrega gemas o upgrades cuando una compra o anuncio se completa.
5. `SkinStoreController` permite gastar gemas en skins y equiparlas.
6. `AssetBundleLoader` carga bundles de iconos u otros assets para la UI.

## Mapa de Dependencias

Las relaciones mas importantes son:

- `GridManager` crea y mantiene `GridTile`.
- `GridTile` depende de `TileManager` y `TerrainTypeData`.
- `TileElevation` modifica la geometria visual y fisica de cada tile.
- `AStarPathFinder` y `GridRangeFinder` dependen de `GridManager`, `GridTile` y ahora tambien de la altura.
- `GridUnit` depende de `UnitData`, `UnitTurnRulesData`, `GridTile`, `TurnManager`, `BattleStateManager` y `AttackEffectData`.
- `TileSelector` coordina input del jugador, pathfinding, rango de movimiento y estados visuales.
- `TurnManager` coordina el flujo completo de turnos y usa snapshots para reiniciar turno.
- `EnemyController` usa `AStarPathFinder` y `GridUnit` para decidir acciones.
- `ObstacleManager` ocupa tiles, modifica su caminabilidad y ajusta altura y rotacion.
- `BuilderInputController` es el puente entre UI de builder y modificaciones reales del mapa.
- `BuilderSaveLoadManager` serializa el estado del builder en JSON.

## Analisis por Sistema

## 1. Sistema de Grid, Terreno y Altura

### `Assets/Scripts/Grid&Tiles/GridManager.cs`

Es el generador principal del tablero.

Responsabilidades:

- mantener `width`, `height` y `tileSpacing`,
- instanciar `tilePrefab` en una matriz,
- reconstruir el grid cuando cambia el tamano,
- exponer consultas como `GetTileAt`, `GetNeighbors` y `CanUnitEnterTile`.

Conexiones:

- usado por combate,
- usado por builder,
- usado por spawners,
- usado por obstaculos,
- usado por pathfinding.

Cambio importante respecto a versiones mas simples:

- ahora puede reconstruir el tablero con `RebuildGrid`,
- esto lo vuelve una pieza central tambien para el level builder.

### `Assets/Scripts/Grid&Tiles/GridTile.cs`

Representa un tile individual del tablero.

Guarda:

- coordenadas `X`, `Y`,
- `terrainType`,
- `isWalkable`,
- `movementCost`,
- ocupacion,
- renderers de superficie, overlay y columna lateral,
- ancla para decoraciones.

Hace varias cosas al mismo tiempo:

- aplica datos de terreno,
- pinta materiales superior y lateral,
- muestra highlights,
- controla decoraciones de terreno,
- guarda la unidad ocupante.

Conexion clave:

- `GridUnit` se posiciona sobre este objeto,
- `ObstacleManager` puede bloquearlo,
- `TileSelector` lo ilumina,
- `TileElevation` altera su forma fisica,
- `TerrainTypeData` define su comportamiento.

### `Assets/Scripts/Grid&Tiles/TileElevation.cs`

Es uno de los scripts nuevos mas importantes.

Su funcion es transformar un tile plano en un tile con altura editable.

Responsabilidades:

- cambiar altura entera por pasos,
- mover la superficie superior,
- escalar la columna lateral,
- recolocar el overlay,
- actualizar el collider,
- mover el anchor de decoraciones,
- ajustar tiling de la textura lateral.

Impacto en gameplay:

- `AStarPathFinder` y `GridRangeFinder` ahora validan si una unidad puede subir segun `maxClimbHeight`.
- el builder puede pintar altura como una propiedad del nivel.

### `Assets/Scripts/Grid&Tiles/TileManager.cs`

Carga todos los `TerrainTypeData` desde `Resources/TerrainTypes`.

Responsabilidades:

- construir un diccionario `TerrainType -> TerrainTypeData`,
- devolver el asset correcto cuando un tile lo pide.

Es la capa de datos de terreno.

### `Assets/Scripts/Grid&Tiles/TerrainTypeData.cs`

`ScriptableObject` que define cada tipo de terreno.

Campos importantes:

- tipo de terreno,
- costo de movimiento,
- dano al entrar,
- dano al detenerse,
- penalizacion de movimiento,
- si es caminable,
- color,
- prefab decorativo,
- material superior,
- material lateral.

Lectura de diseño:

- el sistema ya esta preparado para mecanicas mas profundas de las que hoy se usan.
- por ejemplo, el dano por terreno existe en datos pero todavia no esta aplicado en la logica de movimiento de `GridUnit`.

### `Assets/Scripts/Grid&Tiles/GridRangeFinder.cs`

Calcula todos los tiles alcanzables para una unidad segun su presupuesto de movimiento.

Ahora tiene en cuenta:

- caminabilidad,
- ocupacion,
- costo del terreno,
- diferencia de altura,
- capacidad de escalada de la unidad.

Es esencial para:

- highlights de movimiento,
- validacion de destinos del jugador,
- consistencia del gameplay tactico.

### `Assets/Scripts/AStarCore/AStarPathFinder.cs`

Implementa A*.

Valida:

- tile caminable,
- tile ocupado,
- costo acumulado,
- heuristica Manhattan,
- diferencia de altura entre tiles,
- `MaxClimbHeight` de la unidad.

Es usado por:

- `TileSelector` para preview y movimiento real,
- `EnemyController` para acercarse al objetivo.

### `Assets/Scripts/AStarCore/PathNode.cs`

Clase auxiliar del algoritmo A*.

Guarda:

- tile,
- `GCost`,
- `HCost`,
- `FCost`,
- `Parent`.

No es gameplay directo, pero es indispensable para reconstruir caminos.

## 2. Sistema de Obstaculos

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleManager.cs`

Administra colocacion, eliminacion, consulta, rotacion y elevacion de obstaculos.

Responsabilidades:

- validar footprints rotados,
- colocar obstaculos con rotacion en Y,
- bloquear tiles,
- pintar terreno debajo del obstaculo,
- recalcular posicion visual,
- limpiar todos los obstaculos,
- devolver obstaculos por tile.

Este script es clave tanto para combate como para builder.

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleData.cs`

`ScriptableObject` de configuracion de obstaculos.

Define:

- prefab,
- footprint,
- si bloquea movimiento,
- terreno bajo el obstaculo,
- offset visual,
- rotacion,
- escala,
- presets de transform por rotacion.

La presencia de presets por 0, 90, 180 y 270 grados muestra que el builder esta pensado para objetos no simetricos.

### `Assets/Scripts/Grid&Tiles/Obstacles/PlacedObstacle.cs`

Contenedor de datos en runtime.

Guarda:

- que `ObstacleData` genero el objeto,
- instancia creada,
- origen,
- rotacion Y,
- tiles ocupados.

### `Assets/Scripts/Grid&Tiles/Obstacles/PlaceableAnchor.cs`

Script pequeno que expone un anchor local de colocacion.

Probable uso:

- ayudar a alinear prefabs de builder u obstaculos con un punto de referencia consistente.

### `Assets/Scripts/Grid&Tiles/Obstacles/ObstacleDebugPainter.cs`

Herramienta de debug para colocar y borrar obstaculos en runtime.

Hoy convive con el builder, pero sigue siendo util como herramienta rapida de test.

## 3. Sistema de Unidades y Datos de Unidad

### `Assets/Scripts/TurnLogic/UNITs/PlayerUnits/GridUnit.cs`

Es el nucleo de las unidades jugables y enemigas.

Responsabilidades:

- mantener referencia a `UnitData`,
- exponer stats derivados del asset,
- manejar HP actual,
- moverse por un path,
- atacar,
- recibir dano,
- morir,
- restaurar estado desde snapshots,
- instanciar barra de vida,
- informar fin de movimiento.

Aspectos importantes:

- ya no guarda stats duros como antes, ahora los lee de `UnitData`,
- `TryMove` valida la accion antes de mover,
- `TryAttack` calcula dano final usando defensa del objetivo,
- `RestoreHealth`, `RestoreAliveState` y `RestoreTurnState` permiten rebobinar el turno.

Este script conecta datos, combate, UI y sistema de turnos.

### `Assets/Scripts/TurnLogic/UNITs/UnitData.cs`

`ScriptableObject` principal de definicion de unidades.

Incluye:

- identidad,
- descripcion,
- rol,
- team por defecto,
- HP, ataque, defensa, movimiento, rango,
- `maxClimbHeight`,
- tipo de ataque,
- elemento,
- vision,
- IA,
- habilidades,
- prefab,
- portrait.

Es la base para que el proyecto soporte muchas unidades distintas sin duplicar codigo.

### `Assets/Scripts/TurnLogic/UNITs/UnitAbilityData.cs`

Define una habilidad individual.

Campos:

- nombre,
- descripcion,
- rango,
- poder,
- cooldown,
- si termina turno.

Observacion:

- el sistema de habilidades existe a nivel de datos, pero no hay todavia ejecucion real de habilidades en `GridUnit` o `TurnManager`.

### `Assets/Scripts/TurnLogic/UNITs/UnitSpawnEntry.cs`

Estructura serializable que une:

- `UnitData`,
- posicion de spawn.

Permite que los spawners ya no dependan de un solo prefab fijo.

### `Assets/Scripts/TurnLogic/UNITs/UnitTurnRulesData.cs`

Define reglas de secuencia de acciones:

- atacar despues de mover,
- mover despues de atacar,
- autodesseleccionar cuando ya no quedan acciones.

### `Assets/Scripts/TurnLogic/UNITs/UnitTeam.cs`

Enum simple:

- `Player`
- `Enemy`

Se usa en combate, IA, spawns y chequeos de victoria.

### `Assets/Scripts/TurnLogic/UNITs/Roles&Characteristics/AIType.cs`

Enum de perfil de IA:

- `None`,
- `Aggressive`,
- `Defensive`,
- `Patrol`,
- `Support`,
- `Ambusher`.

Hoy sirve como dato descriptivo. La IA real todavia no usa estos perfiles.

### `Assets/Scripts/TurnLogic/UNITs/Roles&Characteristics/AttackType.cs`

Enum:

- `Melee`
- `Ranged`

Actualmente el combate usa principalmente rango numerico; este enum deja preparada una futura diferenciacion.

### `Assets/Scripts/TurnLogic/UNITs/Roles&Characteristics/ElementType.cs`

Enum elemental:

- `None`,
- `Fire`,
- `Water`,
- `Earth`,
- `Air`,
- `Light`,
- `Dark`.

Todavia no hay sistema de ventajas elementales implementado.

### `Assets/Scripts/TurnLogic/UNITs/Roles&Characteristics/UnitRole.cs`

Enum de rol tactico:

- `Frontliner`,
- `Assassin`,
- `Striker`,
- `Tank`,
- `Support`,
- `Scout`,
- `Mage`.

Sirve para clasificacion de contenido y diseno de unidades.

## 4. Spawners y Composicion Inicial del Combate

### `Assets/Scripts/TurnLogic/UNITs/PlayerUnits/UnitSpawner.cs`

Ahora soporta multiples unidades.

Responsabilidades:

- leer una lista de `UnitSpawnEntry`,
- validar cada tile de spawn,
- instanciar el prefab indicado por `UnitData`,
- inicializar `GridUnit` desde datos,
- ubicar cada unidad.

### `Assets/Scripts/TurnLogic/UNITs/Enemies/EnemySpawner.cs`

Misma idea que `UnitSpawner`, pero para enemigos.

Esto confirma que el proyecto ya evoluciono de 1 vs 1 a grupos de unidades.

## 5. Seleccion, Input Tactico y Turnos

### `Assets/InputSystem_Actions.cs`

Archivo auto-generado del Input System de Unity.

No se edita manualmente.

Se usa para:

- posicion del puntero,
- click,
- erase click,
- end turn,
- acciones compartidas por builder y combate.

### `Assets/Scripts/Grid&Tiles/TileSelector.cs`

Es el interprete principal del input del jugador durante el combate.

Responsabilidades:

- detectar el tile bajo el mouse,
- seleccionar unidades del jugador,
- hacer hover con preview de path,
- atacar objetivos enemigos,
- intentar mover unidades,
- mantener todos los highlights.

Cambio importante:

- antes estaba mas centrado en una sola unidad del player,
- ahora puede seleccionar cualquier `GridUnit` del team jugador que tenga acciones restantes.

Conexion fuerte con:

- `TurnManager`,
- `GridRangeFinder`,
- `AStarPathFinder`,
- `GridUnit`,
- `BattleStateManager`.

### `Assets/Scripts/TurnLogic/TurnState.cs`

Enum simple de flujo:

- `PlayerTurn`,
- `EnemyTurn`,
- `Busy`.

### `Assets/Scripts/TurnLogic/EnemyTurnSpeedMode.cs`

Enum para velocidad del turno enemigo:

- `Normal`,
- `Fast`,
- `SuperFast`.

### `Assets/Scripts/TurnLogic/UnitTurnSnapshot.cs`

Contenedor serializable de estado temporal de una unidad:

- referencia a unidad,
- posicion,
- HP,
- si estaba muerta,
- si ya habia movido,
- si ya habia atacado,
- rotacion visual.

Se usa para el reinicio de turno.

### `Assets/Scripts/TurnLogic/TurnManager.cs`

Es uno de los scripts mas importantes de todo el proyecto.

Responsabilidades:

- mantener el turno actual,
- escuchar `EndTurn`,
- iniciar turno del jugador,
- iniciar turno enemigo,
- bloquear input con `Busy`,
- manejar auto-fin de turno,
- manejar velocidad del turno enemigo,
- guardar snapshots del estado del jugador,
- restaurar snapshots al reiniciar turno,
- mostrar hints y UI de turno.

Capacidades nuevas detectadas:

- `autoEndTurnEnabled`,
- `CycleEnemyTurnSpeedMode`,
- `RestartPlayerTurn`,
- control de usos de reinicio,
- soporte para multiples enemigos en la rutina enemiga.

Lectura de diseño:

- el combate ahora esta mucho mas maduro,
- ya existe calidad de vida para el usuario,
- y tambien capacidad de undo parcial mediante snapshots.

### `Assets/Scripts/TurnLogic/BattleStateManager.cs`

Determina cuando termina la batalla.

Responsabilidades:

- revisar si quedan jugadores o enemigos vivos,
- mostrar `You Win` o `You Lose`,
- activar boton de restart,
- limpiar seleccion visual,
- resetear estado si el turno se reinicia.

## 6. IA Enemiga

### `Assets/Scripts/TurnLogic/UNITs/Enemies/EnemyController.cs`

Controla una unidad enemiga individual.

Flujo:

1. intenta atacar si ya esta en rango,
2. si no puede, busca un tile adyacente al jugador,
3. calcula un path,
4. lo recorta segun presupuesto real de movimiento,
5. se mueve,
6. si las reglas lo permiten, vuelve a intentar atacar al terminar.

Mejoras relevantes:

- ya no usa un maximo fijo de tiles,
- ahora corta el path segun `movementPoints` reales y costo de terreno,
- usa `TryAttack` y `GetMovementCostForTile`.

Esto lo hace mucho mas coherente con el sistema tactico.

## 7. Level Builder

### `Assets/Scripts/Builder/BuilderToolMode.cs`

Enum de herramientas del builder:

- `TerrainPaint`
- `ObstaclePaint`
- `UnitPaint`
- `ElevationPaint`
- `Erase`

### `Assets/Scripts/Builder/BuilderUnitPaintTeam.cs`

Enum para decidir si la unidad colocada pertenece a:

- `Player`
- `Enemy`

### `Assets/Scripts/Builder/BuilderStateController.cs`

Es el estado global del builder.

Guarda:

- herramienta actual,
- brush size,
- elevacion seleccionada,
- equipo de unidad a pintar,
- indices de terreno, obstaculo y unidad,
- rotacion seleccionada para obstaculos y unidades.

Tambien carga assets desde `Resources`:

- terrenos,
- obstaculos,
- unidades de player,
- unidades de enemy.

Es el cerebro de configuracion del builder.

### `Assets/Scripts/Builder/BuilderInputController.cs`

Es el controlador real de interaccion del builder.

Este script es grande porque concentra la logica de edicion:

- hover de brush,
- bloqueo cuando hay UI encima,
- pintar terreno,
- pintar elevacion,
- colocar obstaculos,
- colocar unidades,
- borrar terreno, obstaculos y unidades,
- usar drag para fijar un anchor de rotacion,
- cambiar rotacion con flechas,
- mantener el hover anclado mientras se decide la rotacion.

Este script conecta:

- `BuilderStateController`,
- `ObstacleManager`,
- `BuilderUnitRegistry`,
- `GridManager`,
- `TileManager`,
- parents de unidades,
- input del mouse.

Es la pieza central del editor de niveles.

### `Assets/Scripts/Builder/BuilderUnitRegistry.cs`

Registro runtime de unidades colocadas en el builder.

Guarda:

- lista de unidades colocadas,
- mapa `GridTile -> PlacedBuilderUnit`.

Sirve para:

- borrar una unidad por tile,
- saber que grupo de tiles ocupa una unidad,
- limpiar el estado del builder.

### `Assets/Scripts/Builder/PlacedBuilderUnit.cs`

Contenedor de datos para una unidad colocada manualmente.

Guarda:

- referencia a `GridUnit`,
- `UnitData`,
- origen,
- footprint,
- tiles ocupados.

### `Assets/Scripts/Builder/CamaraConttrols/BuilderCameraController.cs`

Controlador de camara del builder.

Soporta:

- pan con WASD,
- edge pan opcional,
- zoom con wheel,
- rotacion con Q y E,
- orbit con click derecho alrededor de un punto,
- reset a la posicion inicial.

Este script muestra que el builder esta pensado como herramienta usable, no solo como prueba tecnica.

### `Assets/Scripts/Builder/UI/BuilderObstaclePreview.cs`

Genera un preview visual translcido del obstaculo seleccionado.

Responsabilidades:

- reconstruir preview si cambia el asset,
- seguir el tile bajo el cursor,
- aplicar material de preview,
- desactivar colliders del preview,
- ajustar posicion, escala y rotacion.

### `Assets/Scripts/Builder/UI/BuilderUIController.cs`

Controlador de UI del builder.

Responsabilidades:

- sincronizar sliders de brush y elevacion,
- cambiar modo de herramienta con toggles,
- mostrar el nombre del terreno, obstaculo y unidad actual,
- cambiar team de unidad,
- editar el tamano del grid,
- abrir y cerrar el panel de carga,
- mostrar lista de niveles guardados,
- confirmar la carga del nivel elegido,
- controlar paneles de ayuda.

Este script convierte el builder en una herramienta completa dentro del juego.

## 8. Guardado y Carga de Niveles

### `Assets/Scripts/JSON/LevelLayoutData.cs`

Define las estructuras serializables del nivel:

- `LevelLayoutData`
- `TileLayoutData`
- `ObstacleLayoutData`
- `UnitLayoutData`

Que guarda:

- ancho y alto,
- tiles con terreno y elevacion,
- obstaculos con origen y rotacion,
- unidades con `unitId`, posicion, rotacion y team.

### `Assets/Scripts/JSON/BuilderSaveLoadManager.cs`

Es el sistema de persistencia del level builder.

Responsabilidades:

- construir el JSON desde el estado actual,
- escribirlo en `Assets/Resources/LevelLayouts`,
- leer JSON,
- reconstruir grid, elevacion, terreno, obstaculos y unidades,
- listar archivos disponibles,
- sanitizar nombres de archivo,
- limpiar el estado actual antes de redimensionar o cargar.

Detalles importantes:

- usa `Application.dataPath`, asi que el flujo parece orientado a desarrollo/editor,
- guarda niveles en `Resources`, por lo que luego pueden cargarse facilmente dentro de Unity,
- recompone obstaculos antes de aplicar ciertos terrenos para evitar conflictos visuales.

Es una pieza clave porque une builder y contenido jugable.

## 9. UI de Juego y Utilidades

### `Assets/Scripts/UI/WorldHealthBar.cs`

Barra de vida flotante para cada unidad.

Hace:

- mirar a la camara,
- actualizar fill amount,
- cambiar color con gradient,
- mostrar texto de HP.

### `Assets/Scripts/UI/BillBoard.cs`

Utilidad simple para que un objeto mire hacia la camara.

### `Assets/Scripts/UI/Effects/AttackEffectData.cs`

`ScriptableObject` para configurar el efecto visual de ataque.

Define:

- prefab,
- offset,
- escala,
- duracion,
- pop in,
- desplazamiento vertical.

### `Assets/Scripts/UI/SceneNavigationButton.cs`

Script utilitario de UI para cargar una escena por nombre.

### `Assets/Scripts/UI/UpgradeButtonAudioController.cs`

Script de UX para botones de upgrade o compra.

Hace varias cosas:

- reproduce audio de hover,
- pausa y reanuda hover al salir y volver,
- dispara una secuencia de click,
- crossfade entre musica de fondo y audio de transicion,
- muestra un reveal con `CanvasGroup`,
- incluso puede auto-disparar la compra al terminar el hover.

Es bastante especifico para la experiencia de la tienda o la pantalla IAP.

## 10. Menus y Navegacion

### `Assets/Scripts/MenuScripts/MainMenuManager.cs`

Actualmente es un placeholder sin logica real.

### `Assets/Scripts/MenuScripts/SettingsManager.cs`

Tambien es un placeholder sin implementacion.

Lectura del estado del proyecto:

- las escenas y flujo de navegacion existen,
- pero los managers de menu aun no tienen comportamiento programado.

## 11. IAP, Ads, Store y Analytics

### `Assets/Scripts/IAPs/AdInitializer.cs`

Inicializa Unity Ads segun plataforma.

Hace:

- elegir Game ID de Android o iOS,
- inicializar Ads,
- informar resultado.

### `Assets/Scripts/IAPs/AdManager.cs`

Es el manejador principal de anuncios.

Soporta:

- interstitial,
- rewarded,
- banner.

Responsabilidades:

- cargar ads,
- mostrar ads,
- reaccionar a eventos de load/show,
- otorgar recompensa si el rewarded se completa,
- ciclar banners con coroutine,
- enviar evento de analytics al ver un ad.

Conexion importante:

- si el ad rewarded termina, busca `PurchaseFufillment` y entrega gemas.

### `Assets/Scripts/IAPs/AnalyticsManager.cs`

Inicializa `UnityServices` y registra eventos custom:

- click en gema,
- compra de gemas,
- visualizacion de anuncios.

Tambien aplica consentimiento del usuario mediante `UnityConsent`.

### `Assets/Scripts/IAPs/ConsentGateController.cs`

Controla el acceso al contenido monetizado segun consentimiento.

Responsabilidades:

- guardar consentimiento en `PlayerPrefs`,
- mostrar u ocultar panel de consentimiento,
- activar u ocultar objetos bloqueados,
- redirigir a otra escena si se rechaza.

### `Assets/Scripts/IAPs/PurchaseFufillment.cs`

Entrega el resultado real de compras o recompensas.

Maneja:

- gemas disponibles,
- gemas gastadas,
- upgrade a gemas rojas,
- texto e iconos de UI,
- boton de reward ad,
- disparo de interstitial cuando se gasta suficiente moneda.

Tambien procesa `ConfirmedOrder` y `FailedOrder`.

### `Assets/Scripts/IAPs/Store/SkinData.cs`

`ScriptableObject` para una skin:

- id,
- nombre,
- costo,
- material,
- desbloqueo por defecto.

### `Assets/Scripts/IAPs/Store/SkinStoreController.cs`

Controla la tienda de skins.

Hace:

- moverse entre skins,
- comprar si hay gemas,
- equipar skin,
- persistir unlock y skin equipada en `PlayerPrefs`,
- mostrar preview del material.

### `Assets/Scripts/IAPs/AssetBundleLoader.cs`

Carga bundles desde `StreamingAssets`.

Uso principal observado:

- iconos de gemas para UI,
- sprites hacia arreglos de `Image`,
- integracion con `PurchaseFufillment`.

### `Assets/Scripts/IAPs/EnableDisable.cs`

Utilidad minima para alternar el estado activo de un GameObject.

### `Assets/Scripts/IAPs/ProfilerExample.cs`

Script de ejemplo y pruebas de profiling.

No parece formar parte del loop del juego. Es mas bien un ejemplo de rendimiento.

### `Assets/Scripts/IAPs/Events/BoughtGemsEvent.cs`
### `Assets/Scripts/IAPs/Events/GemAdViewedEvent.cs`
### `Assets/Scripts/IAPs/Events/GemClickedEvent.cs`

Son wrappers de eventos custom de Unity Analytics.

Su proposito es encapsular parametros y nombres de evento.

## 12. Scripts de Editor y Build

### `Assets/Editor/BuildGemAssetBundles.cs`

Script de editor para:

- asignar assets seleccionados al bundle `icons/gems`,
- construir asset bundles en `Assets/StreamingAssets`.

Es soporte tecnico para la parte de tienda e iconos.

## 13. Scripts de Particulas de Terceros

En `Assets/Particlecollection_Free samples/...` hay un paquete externo.

Scripts detectados:

- `EffectControllerInspector.cs`
- `EffectToolBar.cs`
- `RenderEffectInspector.cs`
- `XUIUtils.cs`
- `ShaderMaterialsEditor.cs`
- `CameraTarget.cs`
- `EffectController.cs`
- `EffectDemo.cs`
- `EffectShaderPropertyStr.cs`
- `RenderEffect.cs`
- `TransformExtension.cs`

Analisis funcional:

- los scripts `Editor` sirven para inspeccion y edicion del paquete en Unity Editor,
- `EffectDemo` y clases relacionadas sirven para escenas demo y control de efectos,
- no parecen ser logica propia del juego tactico,
- deben documentarse como dependencia externa importada.

## 14. Scripts Tutorial / Plantilla

### `Assets/TutorialInfo/Scripts/Readme.cs`
### `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`

Son scripts de informacion/tutorial del template de Unity.

No forman parte de la logica del juego.

## Recursos Importantes en `Assets/Resources`

El proyecto usa `Resources` como repositorio central de datos:

- `TerrainTypes`
- `ObstacleTypes`
- `Rules`
- `UnitData/Player`
- `UnitData/Enemy`
- `LevelLayouts`
- `LogoEffects`
- `Backgrounds`
- `Interactables`
- `LevelMusic`
- `TileModifier`

Los mas importantes para gameplay son:

- terrenos,
- obstaculos,
- reglas de turno,
- definiciones de unidades,
- layouts guardados,
- efectos de ataque.

## Como se Conecta Todo

La cadena mas importante del proyecto hoy es esta:

1. `UnitData`, `TerrainTypeData`, `ObstacleData` y `UnitTurnRulesData` definen datos.
2. `GridManager`, `GridTile` y `TileElevation` construyen el tablero.
3. `UnitSpawner` y `EnemySpawner` crean las unidades desde `UnitData`.
4. `TileSelector` y `TurnManager` controlan el turno del jugador.
5. `AStarPathFinder` y `GridRangeFinder` validan movimiento real.
6. `EnemyController` actua con la misma logica base de pathfinding y stats.
7. `BattleStateManager` determina final de partida.
8. `BuilderStateController`, `BuilderInputController` y `BuilderUIController` permiten editar niveles.
9. `BuilderSaveLoadManager` convierte el estado del builder en JSON y viceversa.
10. `ConsentGateController`, `AdManager`, `PurchaseFufillment` y `SkinStoreController` manejan el metajuego monetizado.

## Lectura Critica del Estado del Proyecto

Fortalezas claras:

- el proyecto ya tiene buena separacion por sistemas,
- las unidades son data-driven,
- el combate soporta multiples unidades,
- el builder es funcional y bastante completo,
- la altura ya afecta gameplay real,
- hay persistencia de layouts,
- existe infraestructura de monetizacion y tienda.

Limitaciones o areas todavia incompletas:

- `MainMenuManager` y `SettingsManager` siguen vacios,
- el sistema de habilidades existe en datos pero no en ejecucion,
- enums como `AIType`, `ElementType` y `UnitRole` aun no afectan logica real,
- algunos scripts de debug antiguos siguen conviviendo con el builder,
- parte del sistema de IAP parece mas demostrativo o academico que completamente integrado al loop tactico,
- varios flujos dependen mucho de referencias manuales en Inspector.

## Resumen Final

Este proyecto es ahora una plataforma bastante amplia, no solo una demo de grid. Combina:

- combate tactico por turnos,
- editor de niveles dentro del juego,
- guardado y carga de layouts,
- datos configurables por `ScriptableObject`,
- navegacion entre escenas,
- tienda, ads e analytics,
- utilidades de editor y paquetes externos.

La arquitectura central esta bien encaminada: el combate usa datos de unidades y terreno, la altura influye en movimiento, los obstaculos soportan rotacion y footprint, y el builder puede producir niveles persistentes en JSON. Al mismo tiempo, hay capas mas avanzadas que aun estan preparadas pero no completadas, como habilidades activas, diferencias reales por tipo de IA o sistemas elementales.
