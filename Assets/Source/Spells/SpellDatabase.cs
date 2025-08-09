using System.Collections.Generic;
using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "SpellDatabase", menuName = "MageLock/Database/Spell Database")]
    public class SpellDatabase : ScriptableObject
    {
        [SerializeField] private List<Spell> spells = new();
        
        private Dictionary<int, Spell> _spellCache;
        
        public int SpellCount => spells.Count;
        
        private void OnEnable()
        {
            BuildCache();
        }
        
        private void BuildCache()
        {
            _spellCache = new Dictionary<int, Spell>();
            
            foreach (var spell in spells)
            {
                if (spell != null)
                {
                    _spellCache.TryAdd(spell.SpellId, spell);
                }
            }
        }
        
        public Spell GetSpell(int spellId)
        {
            return _spellCache.GetValueOrDefault(spellId);
        }
        
        public bool HasSpell(int spellId)
        {
            return _spellCache.ContainsKey(spellId);
        }
    }
}