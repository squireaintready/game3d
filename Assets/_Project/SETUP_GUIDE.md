# Tower Defense Game - Unity Setup Guide

## Quick Start

1. Open Unity Hub and create a new 3D (URP) project (Unity 6.3 LTS recommended)
2. Copy the `Assets/_Project` folder into your new Unity project's `Assets` folder
3. Import **PolygonFantasyKingdom** asset pack for 3D models
4. Open the project in Unity
5. Use the menu **Tower Defense > Setup Game Scene** to create the basic scene structure
6. Follow the detailed setup steps below

---

## Camera Setup (GameSceneSetup Component)

The game uses a top-down camera view. Add the **GameSceneSetup** component to the GameManager object.

### Recommended Camera Settings:
- **Setup Top Down Camera**: Enabled
- **Camera Angle**: 30 (slight isometric tilt)
- **Camera Height**: 12
- **Use Orthographic**: Disabled (perspective mode)
- **Vertical View Offset**: 1.28 (positions grid with top near window top, bottom above UI)
- **Main Camera Field of View**: 36
- **Update Camera In Realtime**: Enabled (for live adjustment during play)

These settings provide a 2D-style top-down view while still showing 3D model depth.

---

## Step 1: Project Settings

### Tags (Edit > Project Settings > Tags and Layers)
Add these tags:
- `SpawnPoint`
- `SafeZone`
- `Tower`
- `Enemy`
- `Projectile`

### Layers
Add these layers:
- `Ground` (for mouse raycasting)
- `Towers`
- `Enemies`

### Input (optional, for custom controls)
The game uses default Unity input: Horizontal/Vertical axes, Mouse ScrollWheel

---

## Step 2: Package Dependencies

Install via Package Manager (Window > Package Manager):

1. **AI Navigation** (for NavMesh pathfinding)
   - Click "+" > Add package by name
   - Enter: `com.unity.ai.navigation`

2. **TextMeshPro** (for UI text)
   - Usually included, or add: `com.unity.textmeshpro`

---

## Step 3: Scene Setup

### Using the Editor Tool
1. Go to **Tower Defense > Setup Game Scene**
2. This creates the basic structure automatically

### Manual Setup (if needed)

#### Managers GameObject Hierarchy:
```
--- MANAGERS ---
├── GameManager (GameManager.cs)
├── GridManager (GridManager.cs)
├── WaveManager (WaveManager.cs)
├── EconomyManager (EconomyManager.cs)
├── PlacementSystem (PlacementSystem.cs, PlacementValidator.cs)
└── GameInitializer (GameInitializer.cs)
```

#### Environment:
```
--- ENVIRONMENT ---
├── Ground (Plane, NavMesh Surface)
├── SpawnPoints
│   ├── SpawnPoint_1 (tag: SpawnPoint, position: 1.5, 0, 11.5)
│   └── SpawnPoint_2 (tag: SpawnPoint, position: 5.5, 0, 11.5)
└── SafeZone (tag: SafeZone, position: 3.5, 0, 0.5)
```

---

## Step 4: NavMesh Setup

1. Select the Ground object
2. Add component: **NavMesh Surface** (from AI Navigation package)
3. Configure:
   - Agent Type: Humanoid
   - Collect Objects: All
   - Include Layers: Default, Ground
4. Click **Bake** to generate the NavMesh

---

## Step 5: Create ScriptableObject Assets

### TowerData Assets
1. Right-click in `Assets/_Project/Data`
2. Select **Create > Tower Defense > Tower Data**
3. Create two assets:
   - `WallTowerData.asset`
   - `ArcherTowerData.asset`

**Wall Tower Settings:**
- Tower Name: Wall
- Tower Type: Wall
- Cost: 25
- Sell Refund Percent: 0.5
- Model Prefab: SM_Bld_Castle_Wall_01 (from PolygonFantasyKingdom)
- Model Scale: 0.1 (fits within one grid tile)

**Archer Tower Settings:**
- Tower Name: Archer Tower
- Tower Type: Archer
- Cost: 50
- Sell Refund Percent: 0.75
- Damage: 15
- Range: 2.5
- Attack Speed: 1.5
- Projectile Speed: 8
- Model Prefab: SM_Bld_Castle_Wall_01 (wall base)
- Wall Scale: 0.1
- Unit Prefab: SM_Chr_Soldier_Male_01
- Unit Count: 2
- Unit Scale: 0.15
- Unit Offset: (0, 0.15, 0)

**Mage Tower Settings:**
- Tower Name: Mage Tower
- Tower Type: Mage
- Cost: 100
- Damage: 40
- Range: 3.5
- Attack Speed: 0.8
- Model Prefab: SM_Bld_Castle_Wall_01 (wall base)
- Wall Scale: 0.1
- Unit Prefab: SM_Chr_Mage_01
- Unit Count: 1
- Unit Scale: 0.15
- Unit Offset: (0, 0.15, 0)

**Cannon Tower Settings:**
- Tower Name: Cannon Tower
- Tower Type: Cannon
- Cost: 150
- Damage: 60
- Range: 4
- Attack Speed: 0.5
- Model Prefab: SM_Bld_Castle_Wall_01 (wall base)
- Wall Scale: 0.1
- Unit Prefab: SM_Wep_Cannon_01
- Unit Count: 1
- Unit Scale: 0.18
- Unit Offset: (0, 0.15, 0)

### EnemyData Assets
1. Create **Create > Tower Defense > Enemy Data**
2. Create two assets:
   - `SoldierData.asset`
   - `BossData.asset`

**Soldier Settings:**
- Enemy Name: Soldier
- Base Health: 60
- Base Speed: 2
- Base Kill Reward: 10
- Health Scale Per Wave: 1.15
- Speed Scale Per Wave: 1.02
- Kill Reward Per Wave: 1
- Walk Sprites: Assign sliced sprites from `enemy_soldier.png`

**Boss Settings:**
- Enemy Name: Boss
- Base Health: 300
- Base Speed: 1.2
- Base Kill Reward: 50
- Health Scale Per Wave: 1.2
- Speed Scale Per Wave: 1.01
- Kill Reward Per Wave: 5
- Walk Sprites: Assign sliced sprites from `enemy_boss.png`

### WaveConfig Asset
1. Create **Create > Tower Defense > Wave Config**
2. Name it `DefaultWaveConfig.asset`

**Settings:**
- Total Waves: 15
- Build Phase Duration: 30
- Max Early Start Bonus: 50
- Round Survival Bonus Base: 25
- Round Survival Bonus Per Wave: 5

**Difficulty Settings:**
- Easy: HP 0.75, Speed 0.9, Count 0.8
- Normal: HP 1.0, Speed 1.0, Count 1.0
- Hard: HP 1.3, Speed 1.1, Count 1.2

---

## Tower Placement Mechanics

### Ground vs Protected Placement
Attack towers (Archer, Mage, Cannon) can be placed in two ways:

1. **Ground Placement** (on empty tile):
   - Tower is vulnerable with 30 HP
   - Enemies can attack and destroy it
   - Shows unit(s) only, no wall base

2. **Protected Placement** (on existing wall OR add wall to ground tower):
   - Tower is invulnerable
   - Shows wall base with unit(s) on top
   - Two ways to achieve:
     - Place attack tower on existing wall
     - Place wall on existing ground attack tower (upgrades it)

### Tower Costs
- Wall: 25 BTC
- Archer: 50 BTC
- Mage: 100 BTC
- Cannon: 150 BTC
- Starting Currency: 500 BTC

---

## Step 6: Import Sprite Sheets

### Sprite Sheet Standard
All sprite sheets in this project use a **5 columns x 6 rows** grid layout (30 frames total).
- Art style: 3D isometric poly (pre-rendered)
- Format: PNG with transparency
- Import as: Sprite (2D and UI), Multiple mode

### IMPORTANT: Sprite Transparency Issue
The `enemy_soldier.png` sprite sheet currently has a **BLACK background** instead of transparency. Before using in Unity:
1. Open `enemy_soldier.png` in an image editor (Photoshop, GIMP, etc.)
2. Select the black background color
3. Delete or make it transparent
4. Save as PNG with transparency

All other sprites (`tower_archer.png`, `tower_wall.png`, `enemy_boss.png`) already have proper transparency.

### Pre-configured Sprite Meta Files
The sprite sheets have pre-configured `.meta` files with the correct import settings:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple
- Pixels Per Unit: 100
- Alpha Is Transparency: Enabled
- Pre-sliced into 30 frames (5x6 grid)

When you open the project in Unity, these settings should be applied automatically. If sprites don't appear sliced:
1. Select the sprite in Unity
2. Check that Sprite Mode is "Multiple" in Inspector
3. Open Sprite Editor to verify slicing

### Manual Slicing (if needed)

**Tower Sprites** (`tower_archer.png`, `tower_wall.png`):
- Dimensions: 880 x 1168 pixels
- Grid: 5 columns x 6 rows
- Cell Size: 176 x 195 pixels (approximately)
- Use "Grid By Cell Count" in Sprite Editor

**Enemy Sprites** (`enemy_soldier.png`, `enemy_boss.png`):
- Dimensions: 832 x 1248 pixels
- Grid: 5 columns x 6 rows
- Cell Size: 166 x 208 pixels
- Use "Grid By Cell Count" in Sprite Editor

### Sprite Slicing Steps (if meta files don't work)
1. Select the sprite sheet in `Assets/_Project/Art/Sprites`
2. In Inspector:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Multiple
   - Pixels Per Unit: 100
   - Filter Mode: Bilinear
3. Click **Apply**
4. Click **Sprite Editor**
5. Click **Slice** dropdown > **Grid By Cell Count**
   - Columns: 5
   - Rows: 6
6. Click **Slice**, then **Apply** in the Sprite Editor

**Note:** These are pre-rendered isometric sprites. The Billboard component ensures sprites face the camera properly in the 3D scene.

---

## Step 7: Create Prefabs

### Tower Prefab Structure
```
ArcherTower (ArcherTower.cs, NavMeshObstacle)
├── Visual (SpriteRenderer, Billboard.cs)
│   └── SpriteRenderer with archer sprite
├── RangeIndicator (disabled by default)
│   └── Sprite circle or projector
└── FirePoint (empty transform for projectile spawn)
```

**NavMeshObstacle Settings:**
- Shape: Box
- Size: (1, 2, 1)
- Carving: Enabled

### Wall Prefab
```
WallTower (WallTower.cs, NavMeshObstacle)
└── Visual (Cube or sprite)
```

### Enemy Prefab
```
Enemy (Enemy.cs, NavMeshAgent)
├── Visual (SpriteRenderer, Billboard.cs)
├── EnemyMovement.cs
├── EnemyHealth.cs
└── HealthBar (optional)
```

**NavMeshAgent Settings:**
- Speed: 2 (will be overridden by code)
- Angular Speed: 120
- Acceleration: 8
- Stopping Distance: 0.1
- Auto Braking: false

### Projectile Prefab
```
Arrow (Projectile.cs)
├── Visual (small sprite or particle)
└── TrailRenderer (optional)
```

---

## Step 8: UI Setup

Create a Canvas with these elements:

### HUD Panel (top of screen)
- Currency Text (TextMeshPro)
- Lives Text (TextMeshPro)
- Wave Text (TextMeshPro)

### Timer Panel (center, shown during build phase)
- Timer Text
- Start Early Button
- Early Bonus Text

### Tower Panel (bottom of screen)
- Wall Button (with cost text)
- Archer Button (with cost text)

### Tower Info Panel (shows when tower selected)
- Tower Name Text
- Tower Stats Text
- Sell Button
- Sell Value Text

### Pause Panel (overlay)
- Resume Button
- Restart Button
- Main Menu Button

### Game Over Panel (overlay)
- Result Text
- Stats Texts
- Restart Button
- Main Menu Button

---

## Step 9: Wire Up References

### GameManager
- Wave Config: DefaultWaveConfig
- Grid Manager: GridManager object
- Wave Manager: WaveManager object
- Economy Manager: EconomyManager object

### WaveManager
- Wave Config: DefaultWaveConfig
- Default Enemy Data: SoldierData
- Boss Enemy Data: BossData (for boss waves)
- Spawn Points: [SpawnPoint_1, SpawnPoint_2]
- Enemy Prefab: Enemy prefab

### PlacementSystem
- Wall Prefab: WallTower prefab
- Archer Prefab: ArcherTower prefab
- Wall Data: WallTowerData
- Archer Data: ArcherTowerData
- Ground Layer: Ground layer mask
- Validator: PlacementValidator component

### PlacementValidator
- Spawn Points: [SpawnPoint_1, SpawnPoint_2]
- Safe Zone: SafeZone transform

### HUDController
- All UI text/button references

### TowerPanel
- Tower buttons and data references

---

## Step 10: Build Settings

1. File > Build Settings
2. Add scenes:
   - MainMenu (index 0)
   - Game (index 1)
3. Platform: WebGL
4. Player Settings:
   - Resolution: 720 x 1280 (9:16 portrait)
   - WebGL Template: Default or custom

---

## Testing Checklist

- [ ] Can place archer tower on valid grid cell
- [ ] Cannot place tower if it blocks all paths (error shown)
- [ ] Enemies spawn and walk toward safe zone
- [ ] Archer targets and shoots nearest enemy
- [ ] Enemy takes damage and dies
- [ ] Currency increases on kill
- [ ] Life decreases when enemy reaches safe zone
- [ ] Game over triggers at 0 lives
- [ ] Victory triggers after wave 15
- [ ] Start with 200 currency
- [ ] Archer costs 75, Wall costs 25
- [ ] Selling tower refunds correct %
- [ ] Early start grants bonus based on time remaining
- [ ] 30-second timer between waves
- [ ] Can place/sell towers during build phase

---

## Troubleshooting

### Sprites showing white/black background
- Check the source PNG has transparency (not black/white background)
- In Unity Inspector, ensure **Alpha Is Transparency** is checked
- Make sure the SpriteRenderer material uses **Sprites-Default** shader
- For enemy_soldier.png specifically: this sprite has a black background that needs to be removed in an image editor

### Sprites appear flat or not rendering correctly
- Ensure the SpriteRenderer has a valid sprite assigned
- Check that the material is Sprites-Default (not a 3D material)
- Verify the sorting layer and order are correct
- If using Billboard component, check it's enabled and configured

### All enemies/towers showing same sprite
- Check that the ScriptableObject assets have sprites assigned
- Verify WaveManager.defaultEnemyData is set
- Ensure enemy.SetSprites() is being called with the correct data
- Check the walkSprites array in EnemyData is populated

### Enemies not moving
- Check NavMesh is baked
- Verify SafeZone has correct tag
- Check NavMeshAgent is on enemy prefab

### Towers not blocking paths
- Ensure NavMeshObstacle has Carving enabled
- Rebake NavMesh after scene changes

### UI not working
- Check EventSystem exists in scene
- Verify button OnClick events are set
- Check Canvas render mode

### Sprites not facing camera
- Add Billboard component to sprite objects
- Set mode to CameraForward

### ScriptableObject assets not loading
- Recreate the assets using Create menu in Unity
- Ensure the script GUID in .asset files matches the actual script
- Delete and recreate the asset if it shows "Missing Script"

---

## File Structure Reference

```
Assets/_Project/
├── Scenes/
│   ├── MainMenu.unity
│   └── Game.unity
├── Scripts/
│   ├── Core/ (GameManager, GridManager, etc.)
│   ├── Towers/ (Tower, ArcherTower, etc.)
│   ├── Enemies/ (Enemy, EnemyMovement, etc.)
│   ├── Placement/ (PlacementSystem, etc.)
│   ├── UI/ (HUDController, etc.)
│   ├── Camera/ (IsometricCamera, Billboard)
│   └── Editor/ (GameSetupEditor)
├── Data/
│   ├── WallTowerData.asset
│   ├── ArcherTowerData.asset
│   ├── GoblinData.asset
│   └── DefaultWaveConfig.asset
├── Prefabs/
│   ├── Towers/
│   ├── Enemies/
│   └── Projectiles/
├── Art/
│   └── Sprites/
└── Audio/
```
