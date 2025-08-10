using UnityEngine;
using Unity.Netcode;
using MageLock.StatusEffects;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Frostbolt", menuName = "MageLock/Spells/Abilities/Frostbolt")]
    public class Frostbolt : ProjectileSpell
    {
        [Header("Frost Effects")]
        [SerializeField] private SlowEffect frostSlowEffect;
        
        protected override void InitializeProjectile(GameObject projectileObj, GameObject caster)
        {
            var frostProjectile = projectileObj.GetComponent<FrostboltProjectile>();
            
            if (frostProjectile != null)
            {
                frostProjectile.Initialize(caster, damage, projectileSpeed);
                frostProjectile.SetSlowEffect(frostSlowEffect);
            }
        }
    }
}