using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Heal", menuName = "MageLock/Spells/Abilities/Heal")]
    public class Heal : Spell
    {
        [Header("Healing")]
        [SerializeField] private float healAmount = 50f;
        [SerializeField] private GameObject healEffect;
        
        public override void Cast(GameObject caster, Vector3 direction)
        {
            caster.GetComponent<IHealth>()?.Heal(healAmount);
            
            if (healEffect)
                Instantiate(healEffect, caster.transform.position, Quaternion.identity);
        }
    }
}