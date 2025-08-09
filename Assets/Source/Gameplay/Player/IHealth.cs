namespace MageLock.Spells
{
    public interface IHealth
    {
        void TakeDamage(float damage);
        void Heal(float amount);
        float GetHealth();
        float GetMaxHealth();
    }
}