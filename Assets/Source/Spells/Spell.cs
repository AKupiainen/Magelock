namespace MageLock.Spells
{
    using UnityEngine;
    public abstract class Spell : ScriptableObject
    {
        [SerializeField] private int spellId;
        [SerializeField] private float cooldown = 1f;
        [SerializeField] private float manaCost = 10f;
        [SerializeField] private float range = 10f;
        
        public int SpellId => spellId;
        public float Cooldown => cooldown;
        public float ManaCost => manaCost;
        public float Range => range;
        
        public abstract void Cast(GameObject caster, Vector3 direction);
    }
}
    