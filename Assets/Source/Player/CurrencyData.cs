using System;
using System.Collections.Generic;

namespace MageLock.Player
{
    [Serializable]
    public class CurrencyData
    {
        public Dictionary<CurrencyType, int> Currencies = new();

        public int GetCurrency(CurrencyType type)
        {
            if (Currencies.TryGetValue(type, out int value))
            {
                return value;
            }

            return 0;
        }

        public void SetCurrency(CurrencyType type, int amount)
        {
            Currencies[type] = Math.Max(0, amount);
        }

        public void AddCurrency(CurrencyType type, int amount)
        {
            Currencies.TryAdd(type, 0);

            Currencies[type] += amount;
        }

        public bool CanAfford(CurrencyType type, int amount)
        {
            return GetCurrency(type) >= amount;
        }

        public bool SpendCurrency(CurrencyType type, int amount)
        {
            if (!CanAfford(type, amount))
            {
                return false;
            }

            Currencies[type] -= amount;
            return true;
        }

        public Dictionary<CurrencyType, int> GetAllCurrencies()
        {
            return new Dictionary<CurrencyType, int>(Currencies);
        }
    }

    public enum CurrencyType
    {
        Coins,
        Gems
    }
}