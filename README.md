# TowerDefense
2D Unity tower defense project built with Unity `6000.3.9f1`.

## Tower Placement and Behavior
- Tower selection is controlled by `BuildManager`, which stores the currently selected `TowerData` and raises selection-change events.
- `TilemapPlacement2D` handles pointer input, cell snapping, ghost preview, placement validation, and tower placement.
- A placement is allowed only when all checks pass:
  - No blocked tile conflict (`blockedTilemap`)
  - Cell not already occupied
  - No collider overlap in placement blocking layers
  - Player can afford the tower cost
- On successful placement, money is spent through `CurrencyManager`, the tower is spawned via `CentralObjectPool`, and placement mode is canceled.
- Towers can be clicked after placement to show range and an upgrade UI (`TowerUpgradeButtonUI`).
- Core tower combat is in `TowerController2D`:
  - Scans for enemies in range
  - Prioritizes the lowest-HP target
  - Fires pooled projectiles on cooldown
  - Supports upgrade stages via `TowerUpgradeProfile`
- Specialized behavior:
  - `ArrowTowerController2D`: aiming/facing + shoot/idle animation handling
  - `BombTowerController2D`: shoot fade VFX
  - `BombProjectile2D`: AoE explosion and optional camera shake

## Enemy
- Enemy paths are waypoint-driven through `PathManager`.
- `EnemySpawner` reads `WaveConfig` entries, resolves prefab per `EnemyType` using `EnemyPrefabRegistry`, and spawns enemies (with optional lateral spawn variation).
- `EnemyMovement` follows waypoints, supports momentum-style steering, and raises `OnReachedGoal` when the path ends.
- `EnemyHealth2D` implements `IDamageable`, applies damage, plays hit/death visuals, and despawns via `CentralObjectPool` on death.
- Enemy deaths reward currency through `CurrencyManager` listening to `EnemyHealth2D.OnAnyEnemyDied`.
- Goal leaks are handled by `LivesManager` via `EnemySpawner.OnEnemyReachedGoal`; lives decrease and game over is raised at zero.

## Code Architecture
- The project is organized by gameplay domain under `Assets/Scripts`:
  - `Tower/`: targeting, shooting, projectiles, upgrades, placement (`BuildManager`, `TilemapPlacement2D`, `GhostPreview2D`)
  - `Enemy/`: path following, health, prefab registry, enemy VFX
  - `Waves/`: wave data (`WaveConfig`), spawn orchestration (`EnemySpawner`), wave lifecycle (`WaveManager`)
  - `Economy/`: money state and spending (`CurrencyManager`)
  - `GameLoop/`: lives and camera behavior (`LivesManager`, `OrthographicYBoundsCamera`)
  - `Pooling/`: reusable object pooling service (`CentralObjectPool`)
  - `UI/`: HUD and interaction UI (money, lives, wave display, tower build/upgrade visuals)
- Data is driven with ScriptableObjects (`TowerData`, `TowerUpgradeProfile`, `WaveConfig`, `GamePlayData`) so balancing is mostly editor-side.
- Systems communicate through C# events, which keeps gameplay modules loosely coupled (spawn, death, economy, UI, and wave progression).
