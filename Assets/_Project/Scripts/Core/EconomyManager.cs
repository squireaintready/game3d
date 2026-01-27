using UnityEngine;

namespace TowerDefense.Core
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private int startingCurrency = 500;

        public int CurrentCurrency { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CurrentCurrency = startingCurrency;
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(Enemy enemy)
        {
            AddCurrency(enemy.KillReward);
        }

        public void SetCurrency(int amount)
        {
            CurrentCurrency = Mathf.Max(0, amount);
            GameEvents.InvokeCurrencyChanged(CurrentCurrency);
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;

            CurrentCurrency += amount;
            GameEvents.InvokeCurrencyChanged(CurrentCurrency);
            GameEvents.InvokeCurrencyEarned(amount);
        }

        public bool SpendCurrency(int amount)
        {
            if (amount <= 0) return false;
            if (CurrentCurrency < amount) return false;

            CurrentCurrency -= amount;
            GameEvents.InvokeCurrencyChanged(CurrentCurrency);
            GameEvents.InvokeCurrencySpent(amount);
            return true;
        }

        public bool CanAfford(int amount)
        {
            return CurrentCurrency >= amount;
        }

        public void RefundTower(int originalCost, float refundPercent)
        {
            int refund = Mathf.FloorToInt(originalCost * refundPercent);
            AddCurrency(refund);
        }
    }
}
