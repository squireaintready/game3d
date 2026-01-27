# Tower Defense Game

## MVP (Minimum Viable Product)

### Core Gameplay
- Single map with tile-based layout
- Path-based enemy movement from spawn to goal
- Basic tower placement on valid tiles
- Wave-based enemy spawning

### Tiles
- **tile_path**: Ground tiles where enemies walk
- **tile_spawn**: Enemy spawn point(s)
- **tile_goal**: Civilian area (enemy destination)

### Towers
| Tower | Cost | Function |
|-------|------|----------|
| Archer Tower | 75 gold | Ranged attack, fires arrows at enemies |
| Wall Tower | 25 gold | Blocks paths, forces enemy rerouting |

### Enemies
| Enemy | HP | Speed | Reward |
|-------|-----|-------|--------|
| Soldier | 60 | 2.0 | 10 gold |
| Boss | 300 | 1.2 | 50 gold |

### Wave System
- 15 waves total
- Boss waves: 5, 10, 15
- Progressive difficulty scaling

### MVP Features
- Start/pause game controls
- Gold economy (earn from kills, spend on towers)
- Lives system (lose lives when enemies reach goal)
- Basic UI showing gold, lives, wave number

---

## Final Version

### Additional Towers
- Magic Tower (area damage)
- Cannon Tower (slow, heavy damage)
- Support Tower (buffs nearby towers)

### Additional Enemies
- Fast Runner (low HP, high speed)
- Tank (high HP, slow, resistant)
- Flying (ignores walls)
- Healer (heals nearby enemies)

### Map Features
- Multiple maps with varying difficulty
- Map editor for custom levels
- Environmental hazards

### Progression
- Tower upgrades (3 tiers per tower)
- Achievement system
- Unlockable content
- Endless mode after completing waves

### Polish
- Particle effects for attacks and deaths
- Sound effects and background music
- Animated sprites for all units
- Tutorial system
- Save/load game state

---

## Technical Notes

### Sprite Sheets
All sprite sheets use 5 columns x 6 rows (30 frames):
- Towers: 176x194 per cell
- Enemies: 166x208 per cell
- Tiles: varies by asset

### Pathfinding
- NavMesh-based enemy movement
- NavMeshObstacle on Wall Towers
- Dynamic path recalculation when towers placed

### Projectiles
- Arrow: placeholder sprite (to be replaced)
- Travels toward target, deals damage on hit
