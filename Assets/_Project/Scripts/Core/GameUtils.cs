using UnityEngine;
using System.Collections.Generic;

namespace TowerDefense.Core
{
    public static class GameUtils
    {
        /// <summary>
        /// Find the closest enemy to a position within a given range
        /// </summary>
        public static Enemy FindClosestEnemy(Vector3 position, float range, List<Enemy> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;

            Enemy closest = null;
            float closestDistance = range;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        /// <summary>
        /// Find all enemies within range
        /// </summary>
        public static List<Enemy> FindEnemiesInRange(Vector3 position, float range, List<Enemy> enemies)
        {
            var result = new List<Enemy>();

            if (enemies == null) return result;

            float rangeSqr = range * range;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                float distSqr = (position - enemy.transform.position).sqrMagnitude;
                if (distSqr <= rangeSqr)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate predicted position for a moving target
        /// </summary>
        public static Vector3 PredictTargetPosition(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVelocity, float projectileSpeed)
        {
            Vector3 toTarget = targetPos - shooterPos;
            float distance = toTarget.magnitude;
            float timeToHit = distance / projectileSpeed;

            return targetPos + targetVelocity * timeToHit;
        }

        /// <summary>
        /// Check if a position is within the playable grid
        /// </summary>
        public static bool IsWithinGrid(Vector2Int pos, int width, int height)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        /// <summary>
        /// Get neighbors of a grid cell (4-directional)
        /// </summary>
        public static List<Vector2Int> GetNeighbors(Vector2Int cell, int width, int height)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = cell + dir;
                if (IsWithinGrid(neighbor, width, height))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Format currency for display
        /// </summary>
        public static string FormatCurrency(int amount)
        {
            if (amount >= 1000)
            {
                return $"{amount / 1000f:F1}K";
            }
            return amount.ToString();
        }

        /// <summary>
        /// Format time for display
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            if (mins > 0)
            {
                return $"{mins}:{secs:D2}";
            }
            return $"{secs}s";
        }
    }
}
