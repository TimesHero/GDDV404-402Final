# Blueprint Nivel Stress Test

## Nombre sugerido

`STRESS_AllMechanics_01`

## Objetivo del diseno

Este nivel esta pensado para probar en un solo mapa:

- movimiento normal
- coste de movimiento por `Grass`
- coste alto por `Water`
- dano y penalizacion por `Hazard`
- terreno `Blocked`
- elevacion con saltos que el jugador si puede subir y los enemigos no
- cobertura y oclusion con `RockBig_2x2` y `StoneWall_5x2`
- `Barrel` para esconderse
- patrulla
- `RandomLook`
- enemigos estaticos
- push del jugador
- push del enemigo hacia `Hazard`
- objetivo multiple con `ReachTile`
- objetivo adicional de `SurviveTurns`
- objetivo adicional de `KillAllEnemies`

## Configuracion general del builder

- Grid: `12 x 10`
- Coordenadas:
  `x` crece de izquierda a derecha
  `y` crece de abajo hacia arriba
- Convencion de rotacion recomendada:
  `0 = norte`
  `90 = este`
  `180 = sur`
  `270 = oeste`

Si algun prefab en tu escena mira distinto, conserva las posiciones y ajusta solo la rotacion visual.

## Orden recomendado de construccion

1. Cambia el grid a `12 x 10`.
2. Pinta primero el terreno base y elevaciones.
3. Coloca los obstaculos.
4. Coloca los barriles.
5. Coloca las unidades.
6. Crea los objetivos al final.

## Leyenda del mapa base

- `B0` = `Basic` elevacion `0`
- `B1` = `Basic` elevacion `1`
- `B2` = `Basic` elevacion `2`
- `G0` = `Grass` elevacion `0`
- `W0` = `Water` elevacion `0`
- `H0` = `Hazard` elevacion `0`
- `X0` = `Blocked` elevacion `0`

## Capa 1: Terreno + elevacion

Pinta exactamente asi antes de colocar obstaculos:

```text
x ->     0    1    2    3    4    5    6    7    8    9    10   11
y=9 |   B2   B2   B2   X0   X0   B0   B0   B0   B2   B2   B2   B2
y=8 |   B2   B2   B2   X0   X0   X0   B0   G0   B2   B2   B2   B2
y=7 |   B2   B2   B2   B0   B0   G0   G0   G0   B0   B0   B1   B1
y=6 |   B2   B2   B2   B0   B0   B0   B0   B0   B0   B0   B0   B0
y=5 |   B0   B0   B0   B0   B0   B0   B0   B0   B0   B0   B0   B0
y=4 |   B0   B0   B0   B0   B0   B0   B0   B0   B0   H0   B0   B0
y=3 |   B0   G0   G0   B0   B0   B0   B0   H0   H0   H0   B0   B0
y=2 |   G0   G0   G0   B0   W0   B0   B0   B0   B0   B0   B0   B0
y=1 |   B0   B0   B0   B0   W0   W0   B0   B0   W0   B0   B0   B0
y=0 |   B0   B0   B0   B0   W0   W0   B0   B0   B0   B0   B0   B0
```

## Capa 2: Obstaculos

Coloca estos obstaculos exactamente asi.

### Obstaculo S1

- Tipo: `StoneWall_5x2`
- Origen: `(4,4)`
- Rotacion: `0`
- Footprint real usado por el juego: `4 x 2`
- Tiles ocupados:
  `(4,4) (5,4) (6,4) (7,4)`
  `(4,5) (5,5) (6,5) (7,5)`

### Obstaculo R1

- Tipo: `RockBig_2x2`
- Origen: `(2,4)`
- Rotacion: `0`
- Tiles ocupados:
  `(2,4) (3,4)`
  `(2,5) (3,5)`

### Obstaculo R2

- Tipo: `RockBig_2x2`
- Origen: `(8,6)`
- Rotacion: `0`
- Tiles ocupados:
  `(8,6) (9,6)`
  `(8,7) (9,7)`

### Obstaculo R3

- Tipo: `RockBig_2x2`
- Origen: `(6,0)`
- Rotacion: `0`
- Tiles ocupados:
  `(6,0) (7,0)`
  `(6,1) (7,1)`

## Capa 3: Interactuables

Usa `Barrel` normal, o sea `barrel_01`.

### Barriles

- B1 en `(1,2)`
- B2 en `(6,7)`
- B3 en `(9,5)`

## Capa 4: Unidades del jugador

Usa `Amongus` / `Player_sus`.

### Squad inicial

- P1 en `(0,0)` rotacion `90`
- P2 en `(1,0)` rotacion `90`
- P3 en `(0,1)` rotacion `90`
- P4 en `(1,1)` rotacion `90`

## Capa 5: Enemigos

### E1

- Unidad: `SUS 2` / `Enemy_test2`
- Posicion: `(3,2)`
- Rotacion: `90`
- Behavior: `RandomLook`
- Patrol: no

### E2

- Unidad: `SUS 1` / `Enemy_test1`
- Posicion inicial: `(5,6)`
- Rotacion: `90`
- Behavior: `Patrol`
- Patrol start: `(5,6)`
- Patrol end: `(7,6)`

### E3

- Unidad: `SUS 1` / `Enemy_test1`
- Posicion: `(8,5)`
- Rotacion: `180`
- Behavior: `Static`
- Patrol: no

### E4

- Unidad: `SUS 2` / `Enemy_test2`
- Posicion: `(10,6)`
- Rotacion: `180`
- Behavior: `Static`
- Patrol: no

### E5

- Unidad: `SUS 2` / `Enemy_test2`
- Posicion: `(11,8)`
- Rotacion: `270`
- Behavior: `Static`
- Patrol: no

## Capa 6: Objetivos

### Objetivo 1

- Tipo: `KillAllEnemies`

### Objetivo 2

- Tipo: `SurviveTurns`
- Valor: `5`

### Objetivo 3

- Tipo: `ReachTile`
- No uses `ReachWithoutBeingSeen` para esta version base
- Reach tiles:
  `(8,9)`
  `(9,9)`
  `(10,9)`
  `(11,9)`

## Opcion `loseWhenSeen`

- `false`

## Por que esta combinacion

Uso `ReachTile` normal porque este mapa esta pensado para testear stealth, combate y push en la misma partida sin que una sola deteccion invalide todo el run. Ademas, el builder realmente solo te deja mantener una sola familia de objetivo de reach a la vez, asi que esta version prioriza cobertura total de sistemas.

## Resumen de cantidades

- Player units: `4`
- Enemies: `5`
- Obstacles: `4`
- Barrels: `3`
- Reach tiles: `4`

## Que prueba cada zona

### Zona suroeste

- Spawn de 4 unidades
- Grass temprano
- Barril B1 para probar entrar oculto sin presion inmediata
- E1 con `RandomLook` para revisar cambios de cono de vision

### Ruta inferior

- `Water` en `(4,0) (5,0) (4,1) (5,1) (4,2) (8,1)`
- R3 fuerza a rodear y evita correr recto por abajo
- Sirve para probar gasto de movimiento y pathfinding evitando bloqueos

### Banda central

- R1 + S1 crean un choke horizontal
- Obligan a elegir paso oeste o paso este
- Sirven para probar oclusion parcial y total

### Trampa de hazard

- `Hazard` en `(7,3) (8,3) (9,3) (9,4)`
- Tile cebo recomendado para probar push enemigo: `(8,4)`
- Si un player termina turno en `(8,4)` y E3 sigue en `(8,5)`, el AI deberia preferir empujar hacia `(8,3)` porque el score del push es mejor que el de un ataque normal

### Ruta superior

- B2 en `(6,7)` para probar stealth a mitad de mapa
- E2 patrulla de `(5,6)` a `(7,6)` para probar patrol path
- Los tiles altos del oeste `(0..2, 6..9)` se suben con salto de `2`, que los players si pueden hacer pero los enemigos normales no

### Extraccion

- Plataforma alta en `(8..11, 8..9)`
- Acceso normal enemigo por rampa en `(10,7)` y `(11,7)`
- Acceso shortcut de jugador desde `(7,8)` hacia `(8,8)` por diferencia de altura `2`
- E4 y E5 prueban defensa del approach final

## Checklist de test recomendado

Haz esta secuencia para sacarle valor al nivel:

1. Mete un player en B1 `(1,2)` y confirma que el barril lo oculta.
2. Muevete por la ruta inferior para comprobar el coste alto de `Water`.
3. Deja un player en `(8,4)` para baitear el push de E3 hacia `Hazard`.
4. Usa push del jugador sobre E3 o E4 para confirmar forced movement y dano por terreno.
5. Sube al plateau oeste desde `(1,5)` a `(1,6)` para confirmar climb de `2`.
6. Intenta que un enemigo haga lo mismo y verifica que no puede porque su `maxClimbHeight` es `1`.
7. Muevete cerca de B2 `(6,7)` cuando E2 patrulla para revisar vision, sospecha y reaccion.
8. Usa la rampa derecha para una subida normal al objetivo.
9. Usa el shortcut de `(7,8)` a `(8,8)` para validar el acceso exclusivo del jugador.
10. Mata a los 5 enemigos, aguanta hasta el turno 5 y termina con los 4 players en `(8,9) (9,9) (10,9) (11,9)`.

## Variante challenge opcional

Si despues quieres una version mas dura sin rehacer el layout:

- cambia `ReachTile` por `ReachWithoutBeingSeen`
- deja los mismos 4 reach tiles
- activa `loseWhenSeen = true`

Esa variante te convierte el mismo mapa en test de stealth puro.
