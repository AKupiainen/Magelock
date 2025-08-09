using System.Collections.Generic;
using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "SpellDatabase", menuName = "MageLock/Database/Spell Database")]
    public class SpellDatabase : ScriptableObject
    {
        [SerializeField] private List<Spell> spells = new List<Spell>();
        
        private Dictionary<int, Spell> spellCache;
        
        public int SpellCount => spells.Count;
        
        private void OnEnable()
        {
            BuildCache();
        }
        
        private void BuildCache()
        {
            spellCache = new Dictionary<int, Spell>();
            
            foreach (var spell in spells)
            {
                if (spell != null && !spellCache.ContainsKey(spell.SpellId))
                {
                    spellCache[spell.SpellId] = spell;
                }
            }
        }
        
        public Spell GetSpell(int spellId)
        {
            if (spellCache.TryGetValue(spellId, out Spell spell))
            {
                return spell;
            }
            
            return null;
        }
        
        public bool HasSpell(int spellId)
        {
            return spellCache.ContainsKey(spellId);
        }
    }
}