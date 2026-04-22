# Resumen del Proyecto

## Que es este proyecto

Es un proyecto de Unity que combina tres capas principales:

- un juego tactico por turnos en grid,
- un editor de niveles dentro del juego,
- un bloque de metajuego con tienda, gemas, anuncios y analytics.

## Sistemas principales

### Combate tactico

Los scripts mas importantes son:

- `GridManager`
- `GridTile`
- `TileElevation`
- `AStarPathFinder`
- `GridRangeFinder`
- `GridUnit`
- `TileSelector`
- `TurnManager`
- `EnemyController`
- `BattleStateManager`

Funcion general:

- se crea un tablero,
- se colocan unidades de jugador y enemigos,
- el jugador selecciona unidades, se mueve y ataca,
- la IA enemiga responde durante su turno,
- la batalla termina cuando un equipo queda sin unidades activas.

### Builder de niveles

Los scripts principales son:

- `BuilderStateController`
- `BuilderInputController`
- `BuilderUIController`
- `BuilderCameraController`
- `BuilderObstaclePreview`
- `BuilderUnitRegistry`
- `BuilderSaveLoadManager`

Funcion general:

- permite pintar terreno,
- cambiar elevacion,
- colocar obstaculos,
- colocar unidades,
- borrar contenido,
- cambiar tamano del grid,
- guardar y cargar layouts en JSON.

### Datos del juego

El proyecto usa muchos `ScriptableObject` para evitar codigo duro:

- `TerrainTypeData`
- `ObstacleData`
- `UnitData`
- `UnitAbilityData`
- `UnitTurnRulesData`
- `AttackEffectData`
- `SkinData`

Esto hace que el contenido sea configurable desde Unity.

### Store, ads y analytics

Los scripts principales son:

- `ConsentGateController`
- `AnalyticsManager`
- `AdInitializer`
- `AdManager`
- `PurchaseFufillment`
- `SkinStoreController`
- `AssetBundleLoader`

Funcion general:

- pedir consentimiento,
- inicializar servicios,
- mostrar anuncios,
- entregar gemas o upgrades,
- permitir compra y equipamiento de skins,
- registrar eventos de analytics.

## Conexiones clave

- `GridManager` crea el tablero.
- `TileManager` le da significado al terreno.
- `GridUnit` usa `UnitData` para obtener stats.
- `TileSelector` usa `GridRangeFinder` y `AStarPathFinder` para mover unidades.
- `TurnManager` coordina turnos y estados globales.
- `EnemyController` usa la misma informacion de grid para actuar.
- `BuilderSaveLoadManager` serializa el nivel a JSON.
- `ObstacleManager` y `BuilderInputController` conectan builder con gameplay real.

## Estado actual del proyecto

Lo mas fuerte del proyecto hoy:

- combate tactico funcional,
- soporte para multiples unidades,
- altura que afecta movimiento,
- builder bastante completo,
- guardado y carga de niveles,
- uso correcto de datos configurables.

Lo que todavia parece incompleto o en preparacion:

- habilidades activas aun no implementadas en combate,
- tipos de IA y elementos existen como datos, pero no afectan mucho la logica real,
- `MainMenuManager` y `SettingsManager` estan vacios,
- parte de la capa IAP parece de demo o integracion academica.

## Conclusion corta

El proyecto ya es una base bastante solida para un juego tactico con editor de niveles y una capa de monetizacion experimental. Su mejor parte es la separacion entre sistemas y el uso de `ScriptableObject`. Su siguiente salto de calidad estaria en profundizar combate, habilidades, IA y pulido general de menus y metajuego.
