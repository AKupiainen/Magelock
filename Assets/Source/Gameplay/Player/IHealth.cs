namespace MageLock.Gameplay
{
    public interface IHealth
    {
        void TakeDamage(float damage);
        void Heal(float amount);
        float GetHealth();
        float GetMaxHealth();
    }
    
    public interface IMovement
    {
        /// <summary>
        /// Sets the current movement speed
        /// </summary>
        /// <param name="speed">The new movement speed</param>
        void SetSpeed(float speed);
        
        /// <summary>
        /// Gets the base movement speed (unmodified)
        /// </summary>
        /// <returns>The base movement speed</returns>
        float GetBaseSpeed();
    }
}