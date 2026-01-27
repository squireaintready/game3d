# Tower Defense Game - Development Plan

---

## 3D CONVERSION (January 2026) ✅ COMPLETE

### Overview
Converted from 2D sprites to 3D low-poly models (Synty POLYGON Fantasy Kingdom) with Clash Royale-style camera.

### Camera Change
| Setting | Before (2D) | After (3D) |
|---------|-------------|------------|
| Type | Orthographic | **Perspective** |
| Pitch | 90° (straight down) | **~52°** (Clash Royale) |
| FOV | N/A | **60** |
| Billboard | Yes (sprites face camera) | **No** |

### Asset Mapping (Synty Pack) ✅ WIRED
| Game Element | Synty Prefab | Status |
|--------------|--------------|--------|
| Archer Tower | `SM_Wep_Ballista_Mounted_01` | ✅ Wired |
| Wall Tower | `SM_Bld_Castle_Wall_Block_01` | ✅ Wired |
| Soldier Enemy | `SM_Chr_Soldier_Male_01` | ✅ Wired |
| Boss Enemy | `SM_Chr_King_01` | ✅ Wired |
| Projectile | Runtime 3D capsule | ✅ Working |

### Code Changes ✅
- [x] `GridManager.cs` - Perspective camera ~52°, lighting
- [x] `TowerData.cs` - `modelPrefab` replaces `sprites[]`
- [x] `EnemyData.cs` - `modelPrefab` replaces `walkSprites[]`
- [x] `Tower.cs` - 3D model instantiation + placeholder cubes
- [x] `ArcherTower.cs` - 3D projectile (capsule), cleaned sprite refs
- [x] `Enemy.cs` - 3D model, rotate toward movement + placeholder cubes
- [x] `WaveManager.cs` - Uses `SetEnemyData()` instead of `SetSprites()`
- [x] `TowerPlacementSystem.cs` - Uses `icon` for preview
- [x] `Billboard.cs` - **DELETED** (not needed for 3D)
- [x] `SpriteSheetAnimator.cs` - **DELETED** (not needed for 3D)

### Synty Integration ✅ COMPLETE
1. ✅ Imported Synty POLYGON Fantasy Kingdom
2. ✅ Wired prefabs to ScriptableObjects:
   - `ArcherTowerData.asset` → Ballista (scale 0.8)
   - `WallTowerData.asset` → Castle Wall Block (scale 0.4)
   - `SoldierEnemyData.asset` → Soldier Male (scale 0.8)
   - `BossEnemyData.asset` → King (scale 1.0)

### Animation (Future)
- Synty uses Mecanim (no animations included)
- Use Mixamo for idle, walk, attack, death animations

---

## Project Overview

A 3D low-poly isometric tower defense game with open-field pathfinding, featuring a fantasy/sci-fi crossover theme. Players defend 20 civilian characters from waves of enemies by strategically placing towers and commanding a hero unit.

### Target Platforms
- **Primary:** Web (WebGL) for rapid iteration and testing
- **Secondary:** iOS (primary mobile), Android

### Tech Stack
- **Engine:** Unity 2022 LTS (or newer)
- **Language:** C#
- **Build Targets:** WebGL, iOS, Android

---

## Core Game Mechanics

### Unique Selling Points
1. **Civilian Lives System** - 20 civilians visually represented on the map, each trying to survive
2. **Open Field Pathfinding** - Enemies dynamically navigate around player-placed towers
3. **Hero Unit** - Player-controlled character that fights alongside towers
4. **Elemental System** - Rock-paper-scissors damage interactions
5. **Fantasy/Sci-fi Fusion** - Unique aesthetic blending magic and technology

---

## Development Phases

### Phase 1: MVP (Survival Mode - Single Map)

The MVP focuses on proving core gameplay is fun before expanding scope.

#### 1.1 Core Infrastructure
- [ ] Unity project setup with folder structure
- [ ] Input system (touch + mouse/keyboard abstraction)
- [ ] Camera system (isometric view, pan, zoom)
- [ ] Basic UI framework (using Unity UI Toolkit or Canvas)
- [ ] Game state management (menu, playing, paused, game over)

#### 1.2 Grid & Placement System
- [ ] Grid-based map system
- [ ] Tower placement preview (valid/invalid indicators)
- [ ] Placement validation (no blocking all paths)
- [ ] Tower selection and selling

#### 1.3 Pathfinding System
- [ ] NavMesh-based pathfinding with dynamic obstacles
- [ ] Path recalculation when towers placed/removed
- [ ] Path validation (ensure civilians always have escape route)
- [ ] Multiple spawn points and exit points support

#### 1.4 Enemy System
- [ ] Base enemy class with stats (HP, speed, armor, element)
- [ ] Enemy spawner with wave configuration
- [ ] 4 enemy types for MVP:
  - **Basic** - Balanced stats
  - **Fast** - Low HP, high speed
  - **Tank** - High HP, slow, armored
  - **Flying** - Ignores ground obstacles
- [ ] Enemy health bars (world-space UI)
- [ ] Death effects and currency drops

#### 1.5 Tower System
- [ ] Base tower class with stats (damage, range, fire rate, element)
- [ ] Tower targeting modes (nearest, strongest, first, last)
- [ ] 5 tower types for MVP:
  - **Ballista** - Physical damage, single target
  - **Fire Tower** - Fire element, AOE splash
  - **Ice Tower** - Ice element, slows enemies
  - **Tesla Coil** - Electric element, chain lightning
  - **Beacon** - Buffs nearby towers
- [ ] Tower range visualization
- [ ] Projectile system with pooling

#### 1.6 Elemental System
| Attacker | Strong Against | Weak Against |
|----------|---------------|--------------|
| Fire | Ice, Nature | Water, Earth |
| Ice | Electric, Water | Fire, Physical |
| Electric | Water, Metal | Earth, Ice |
| Physical | — | Armored |
| Arcane | All (neutral) | — |

- [ ] Damage multipliers (1.5x strong, 0.5x weak)
- [ ] Visual indicators showing enemy weaknesses
- [ ] Status effects:
  - **Burning** - DOT for 3 seconds
  - **Frozen** - 50% slow for 2 seconds
  - **Shocked** - Stun for 0.5 seconds

#### 1.7 Civilian System
- [ ] 20 civilian entities with simple AI
- [ ] Civilians wander in safe zone until attacked
- [ ] Flee behavior when enemies get close
- [ ] Visual indicator when civilian in danger (outline, icon)
- [ ] Death animation with life counter update
- [ ] Victory if any civilians survive all waves

#### 1.8 Hero System
- [ ] Hero with direct player control (virtual joystick on mobile)
- [ ] Basic attack (auto-attack nearby enemies)
- [ ] 3 abilities with cooldowns:
  - **Primary** - Damage skill
  - **Utility** - Movement/CC skill
  - **Ultimate** - High-impact, long cooldown
- [ ] Health system with auto-regen out of combat
- [ ] Revival timer if hero dies (respawn after X seconds)
- [ ] 1 hero for MVP (expand later)

#### 1.9 Economy System
- [ ] Currency (Gold/Energy) earned from kills
- [ ] Bonus currency at wave completion
- [ ] Tower costs and selling (75% refund)
- [ ] Interest system (optional: bonus for unspent currency)

#### 1.10 Wave & Survival System
- [ ] Wave configuration via ScriptableObjects
- [ ] Progressive difficulty scaling:
  - More enemies per wave
  - Stronger enemy variants
  - Mixed enemy compositions
- [ ] Wave timer between waves
- [ ] Endless scaling formula for survival mode
- [ ] High score tracking (local save)

#### 1.11 MVP UI
- [ ] Main menu (Play, Settings, Credits)
- [ ] HUD:
  - Currency display
  - Lives remaining (with civilian icons)
  - Current wave / enemies remaining
  - Hero health and abilities
- [ ] Tower build menu (bottom panel or radial)
- [ ] Tower info panel (tap tower to see stats, upgrade, sell)
- [ ] Pause menu
- [ ] Game over screen (stats, restart, main menu)
- [ ] Settings (volume, graphics quality)

#### 1.12 Audio & Polish
- [ ] Sound effects:
  - Tower shots, impacts, construction
  - Enemy footsteps, deaths, spawn
  - UI clicks, alerts
  - Hero abilities
- [ ] Background music (looping ambient track)
- [ ] Particle effects for all combat interactions
- [ ] Juice: screen shake, hit flash, death animations

---

### Phase 2: Tower Upgrades

#### 2.1 Upgrade System Architecture
- [ ] Towers gain XP from damage dealt
- [ ] Level up at thresholds (Lv1 → Lv2 → Lv3)
- [ ] At Lv3, unlock branching specialization

#### 2.2 Upgrade Trees
```
Ballista (Lv1)
  └─► Ballista Lv2 (+damage)
        └─► Ballista Lv3 (+damage, +range)
              ├─► Sniper Ballista (extreme range, slow)
              └─► Repeater (fast fire, lower damage)

Fire Tower (Lv1)
  └─► Fire Tower Lv2 (+AOE radius)
        └─► Fire Tower Lv3 (+burn duration)
              ├─► Inferno Tower (massive AOE)
              └─► Meteor Tower (delayed high-damage strike)

Ice Tower (Lv1)
  └─► Ice Tower Lv2 (+slow strength)
        └─► Ice Tower Lv3 (+effect duration)
              ├─► Blizzard Tower (AOE slow field)
              └─► Cryo Cannon (single-target freeze)

Tesla Coil (Lv1)
  └─► Tesla Coil Lv2 (+chain targets)
        └─► Tesla Coil Lv3 (+stun duration)
              ├─► Storm Spire (AOE lightning strikes)
              └─► Arc Pylon (chains to 8 targets)

Beacon (Lv1)
  └─► Beacon Lv2 (+buff radius)
        └─► Beacon Lv3 (+buff strength)
              ├─► War Banner (damage buff)
              └─► Shield Generator (damage reduction)
```

#### 2.3 Upgrade UI
- [ ] Upgrade button on tower info panel
- [ ] Visual upgrade path tree
- [ ] Preview stats before confirming
- [ ] Tower model/effects change with upgrades

---

### Phase 3: Full Campaign

#### 3.1 Map Roster (5 Maps)
| Map | Theme | Layout | Special Mechanic |
|-----|-------|--------|------------------|
| 1. Haven Outpost | Tutorial plains | Simple, few paths | Guided placement |
| 2. Neon Forest | Bioluminescent woods | Many obstacles | Ambush spawns |
| 3. Rust Desert | Wasteland | Wide open | Sandstorms (vision) |
| 4. Sky Fortress | Floating platforms | Chokepoints | Vertical layers |
| 5. The Breach | Final stand | Boss arena | Multi-phase boss |

#### 3.2 Difficulty Modes
| Mode | Enemy HP | Enemy Speed | Starting Gold | Income Rate |
|------|----------|-------------|---------------|-------------|
| Easy | 75% | 80% | 150% | 125% |
| Normal | 100% | 100% | 100% | 100% |
| Hard | 125% | 110% | 75% | 80% |
| Survival | Scaling | Scaling | 100% | 100% |

#### 3.3 Progression & Unlocks
- [ ] Map selection screen (world map style)
- [ ] Star rating (1-3) per difficulty based on civilians saved
- [ ] Unlock later maps by completing earlier ones
- [ ] Persistent unlocks between sessions:
  - New towers (unlock via stars)
  - New heroes (unlock via achievements)
  - Cosmetics (optional)

#### 3.4 Expanded Content
**Towers (3 additional)**
- [ ] Mortar (physical, AOE, slow fire)
- [ ] Arcane Obelisk (arcane damage, ignores armor)
- [ ] Mine Layer (places proximity mines)

**Enemies (6+ additional)**
- [ ] Healer (heals nearby enemies)
- [ ] Shield Bearer (blocks projectiles for others)
- [ ] Burrower (underground travel, immune until surfacing)
- [ ] Splitter (splits into smaller units on death)
- [ ] Boss: The Siege Engine (multi-stage, spawns adds)
- [ ] Boss: The Swarm Mother (summons endless minions)

**Heroes (2 additional)**
- [ ] Ranger - Long range, traps, mobility
- [ ] Mage - AOE damage, elemental mastery
- [ ] (MVP Hero) Knight - Balanced melee fighter

---

## Technical Architecture

### Project Structure
```
Assets/
├── _Project/
│   ├── Scenes/
│   │   ├── Boot.unity              # Initialization
│   │   ├── MainMenu.unity
│   │   ├── Game.unity              # Main gameplay scene
│   │   └── Maps/                   # Map-specific scene additives
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          # High-level game state
│   │   │   ├── LevelManager.cs         # Map loading, wave control
│   │   │   ├── InputManager.cs         # Unified input (touch/mouse)
│   │   │   ├── AudioManager.cs         # Sound/music control
│   │   │   ├── SaveManager.cs          # Persistence
│   │   │   └── Events/
│   │   │       └── GameEvents.cs       # Static event bus
│   │   │
│   │   ├── Grid/
│   │   │   ├── GridManager.cs          # Grid state
│   │   │   ├── GridCell.cs             # Individual cell data
│   │   │   ├── PlacementSystem.cs      # Tower placement logic
│   │   │   └── PlacementValidator.cs   # Path validation
│   │   │
│   │   ├── Pathfinding/
│   │   │   ├── PathfindingManager.cs   # NavMesh controller
│   │   │   └── DynamicObstacle.cs      # Tower obstacle component
│   │   │
│   │   ├── Entities/
│   │   │   ├── Towers/
│   │   │   │   ├── Tower.cs            # Base tower class
│   │   │   │   ├── TowerTargeting.cs   # Targeting logic
│   │   │   │   ├── TowerUpgrade.cs     # Upgrade system
│   │   │   │   └── Projectile.cs       # Projectile behavior
│   │   │   │
│   │   │   ├── Enemies/
│   │   │   │   ├── Enemy.cs            # Base enemy class
│   │   │   │   ├── EnemyMovement.cs    # Pathfinding movement
│   │   │   │   ├── EnemyHealth.cs      # Health component
│   │   │   │   └── EnemySpawner.cs     # Wave spawning
│   │   │   │
│   │   │   ├── Hero/
│   │   │   │   ├── HeroController.cs   # Player input handling
│   │   │   │   ├── HeroAbilities.cs    # Ability system
│   │   │   │   └── HeroStats.cs        # Stats and leveling
│   │   │   │
│   │   │   └── Civilians/
│   │   │       ├── Civilian.cs         # Civilian behavior
│   │   │       └── CivilianManager.cs  # Track all civilians
│   │   │
│   │   ├── Combat/
│   │   │   ├── DamageSystem.cs         # Damage calculation
│   │   │   ├── ElementSystem.cs        # Element matchups
│   │   │   ├── StatusEffect.cs         # Base status effect
│   │   │   └── Effects/                # Specific effects
│   │   │
│   │   ├── Economy/
│   │   │   └── EconomyManager.cs       # Currency tracking
│   │   │
│   │   └── UI/
│   │       ├── HUDController.cs
│   │       ├── TowerBuildMenu.cs
│   │       ├── TowerInfoPanel.cs
│   │       ├── PauseMenu.cs
│   │       ├── GameOverScreen.cs
│   │       └── Components/             # Reusable UI elements
│   │
│   ├── Data/                           # ScriptableObjects
│   │   ├── Towers/
│   │   │   └── TowerData.asset
│   │   ├── Enemies/
│   │   │   └── EnemyData.asset
│   │   ├── Waves/
│   │   │   └── WaveConfig.asset
│   │   ├── Heroes/
│   │   │   └── HeroData.asset
│   │   └── Elements/
│   │       └── ElementConfig.asset
│   │
│   ├── Prefabs/
│   │   ├── Towers/
│   │   ├── Enemies/
│   │   ├── Projectiles/
│   │   ├── Effects/
│   │   ├── UI/
│   │   └── Environment/
│   │
│   ├── Art/
│   │   ├── Models/
│   │   ├── Materials/
│   │   ├── Textures/
│   │   ├── Animations/
│   │   └── VFX/
│   │
│   ├── Audio/
│   │   ├── SFX/
│   │   └── Music/
│   │
│   └── Settings/                       # Input actions, render settings
│
├── Plugins/                            # Third-party assets
└── Resources/                          # Runtime-loaded assets
```

### Key Design Patterns

#### 1. ScriptableObject Data Architecture
```csharp
[CreateAssetMenu(menuName = "TD/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Identity")]
    public string towerName;
    public Sprite icon;
    public GameObject prefab;

    [Header("Stats")]
    public int cost;
    public float damage;
    public float range;
    public float fireRate;
    public ElementType element;

    [Header("Targeting")]
    public TargetingMode defaultTargeting;
    public bool canTargetFlying;

    [Header("Upgrades")]
    public TowerData[] upgradePaths;
    public int upgradeXPRequired;
}
```

#### 2. Event-Driven Communication
```csharp
public static class GameEvents
{
    // Economy
    public static event Action<int> OnCurrencyChanged;
    public static event Action<int, int> OnCurrencyTransaction; // amount, newTotal

    // Combat
    public static event Action<Enemy> OnEnemySpawned;
    public static event Action<Enemy, int> OnEnemyDamaged; // enemy, damage
    public static event Action<Enemy> OnEnemyKilled;
    public static event Action<Civilian> OnCivilianKilled;

    // Waves
    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    public static event Action OnAllWavesCompleted;

    // Game State
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action<bool> OnGameOver; // won
}
```

#### 3. Object Pooling
```csharp
public class ObjectPool<T> where T : Component
{
    private Queue<T> _pool = new();
    private T _prefab;
    private Transform _parent;

    public T Get() { /* ... */ }
    public void Return(T obj) { /* ... */ }
}
```
Use for: Enemies, Projectiles, VFX, Damage numbers

#### 4. State Machine for Game Flow
```csharp
public enum GameState { Menu, Loading, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameState CurrentState { get; private set; }

    public void ChangeState(GameState newState) { /* ... */ }
}
```

---

## MVP Milestones

### Milestone 1: Core Loop Prototype
**Goal:** Playable loop with placeholder art

- [ ] Unity project initialized
- [ ] Isometric camera with pan/zoom
- [ ] Grid system with buildable/non-buildable cells
- [ ] Tower placement (cubes as placeholders)
- [ ] Enemy spawning and pathfinding (spheres)
- [ ] Enemies reroute when path blocked
- [ ] Towers shoot at enemies
- [ ] Enemies take damage and die
- [ ] Currency drops and collection
- [ ] Basic HUD (currency, lives)

**Deliverable:** WebGL build demonstrating core mechanics

---

### Milestone 2: Core Systems Complete
**Goal:** All MVP systems functional

- [ ] All 5 tower types implemented
- [ ] All 4 enemy types implemented
- [ ] Elemental damage system working
- [ ] Status effects (burn, freeze, shock)
- [ ] 20 civilians with flee behavior
- [ ] Hero character with movement and attack
- [ ] Hero abilities (3)
- [ ] Wave spawner with 10+ wave config
- [ ] Tower targeting modes
- [ ] Economy (buy, sell, wave bonuses)

**Deliverable:** Feature-complete gameplay with programmer art

---

### Milestone 3: Art & Audio Integration
**Goal:** Game looks and sounds good

- [ ] Low-poly tower models integrated
- [ ] Low-poly enemy models integrated
- [ ] Hero model and animations
- [ ] Civilian models
- [ ] Environment art (ground, props)
- [ ] Particle effects for all actions
- [ ] UI art and layout
- [ ] Sound effects complete
- [ ] Background music

**Deliverable:** Visually polished MVP

---

### Milestone 4: Polish & Platform Testing
**Goal:** Shippable MVP

- [ ] Balance pass (tower costs, damage, wave difficulty)
- [ ] Performance optimization (pooling, batching, LOD)
- [ ] WebGL build optimized and tested
- [ ] Mobile touch controls
- [ ] iOS build tested on device
- [ ] Android build tested on device
- [ ] Bug fixes from playtesting
- [ ] Tutorial tooltips or onboarding
- [ ] Settings menu (audio, quality)

**Deliverable:** MVP ready for public playtesting

---

## Art Asset Checklist (Self-Created)

### Environment
- [ ] Ground tile: Grass/tech hybrid
- [ ] Ground tile: Path/walkway
- [ ] Ground tile: Buildable platform
- [ ] Prop: Crystal/tech rocks (3 variants)
- [ ] Prop: Trees/structures (3 variants)
- [ ] Spawn portal model
- [ ] Exit safe zone model

### Towers (5 + upgrades)
- [ ] Ballista (base + 2 upgrades)
- [ ] Fire Tower (base + 2 upgrades)
- [ ] Ice Tower (base + 2 upgrades)
- [ ] Tesla Coil (base + 2 upgrades)
- [ ] Beacon (base + 2 upgrades)

### Enemies (4)
- [ ] Basic enemy (humanoid or creature)
- [ ] Fast enemy (small, agile)
- [ ] Tank enemy (large, armored)
- [ ] Flying enemy (winged/hovering)

### Characters
- [ ] Hero model (with attack animations)
- [ ] Civilian model (2-3 color variants)

### Projectiles & Effects
- [ ] Arrow/bolt projectile
- [ ] Fireball projectile
- [ ] Ice shard projectile
- [ ] Lightning bolt effect
- [ ] Buff aura effect
- [ ] Explosion particles (fire, ice, electric)
- [ ] Hit impact effects
- [ ] Death effects

---

## Risk Management

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Pathfinding performance with many enemies | Medium | High | Use NavMesh, limit recalc frequency, spatial partitioning |
| Mobile performance issues | Medium | High | Object pooling, LOD, aggressive batching, profile early |
| Open-field maze solving (players block all paths) | Low | Medium | Validate placement, require minimum path width |
| Scope creep | High | Medium | Strict MVP feature lock, maintain backlog |
| Art asset creation bottleneck | Medium | Medium | Start with primitives, parallelize art/code work |
| WebGL-specific bugs | Low | Medium | Test WebGL builds frequently throughout development |
| Touch controls feeling bad | Medium | Medium | Iterate on mobile UX early, test on real devices |

---

## Definition of Done

### MVP Complete When:
- [ ] Player can complete 20+ waves in survival mode
- [ ] All 5 towers function correctly with unique behaviors
- [ ] All 4 enemy types function correctly
- [ ] Hero is controllable with all 3 abilities working
- [ ] 20 civilians flee and can be killed
- [ ] Game ends when all civilians die OR player quits
- [ ] High score is saved locally
- [ ] Runs at 60fps in WebGL on modern browsers
- [ ] Runs at 30fps on iPhone 11 / equivalent Android
- [ ] No game-breaking bugs

### Full Game Complete When:
- [ ] 5 maps playable
- [ ] 4 difficulty modes functional
- [ ] Star progression system working
- [ ] Tower upgrade system complete
- [ ] 8+ tower types
- [ ] 10+ enemy types including 2 bosses
- [ ] 3 playable heroes
- [ ] Tutorial or onboarding flow
- [ ] App store ready (icons, screenshots, descriptions)

---

## Immediate Next Steps

1. **Create Unity project** with folder structure above
2. **Set up version control** (Git + .gitignore for Unity)
3. **Implement isometric camera** with pan and zoom
4. **Build grid system** with visual debug overlay
5. **Basic tower placement** (no validation yet)
6. **Enemy spawn + NavMesh pathfinding**
7. **Tower shooting mechanic**
8. **Iterate until core loop feels fun**

---

*Document Version: 1.0*
*Created: January 2026*
*Last Updated: January 2026*
