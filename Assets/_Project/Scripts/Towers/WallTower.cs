using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense
{
    public class WallTower : Tower
    {
        // Wall towers have no combat functionality
        // They simply block enemy paths via NavMesh obstacle

        protected override void Update()
        {
            // Walls don't animate by default, but keep base for potential future use
            base.Update();
        }
    }
}
