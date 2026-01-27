using UnityEngine;

namespace TowerDefense
{
    public class EnemyHealth : MonoBehaviour
    {
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

        public void Initialize(float maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }

        public void Heal(float amount)
        {
            if (amount <= 0) return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }

        public bool IsDead()
        {
            return CurrentHealth <= 0;
        }
    }
}
