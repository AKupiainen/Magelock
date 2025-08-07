using System.Threading.Tasks;
using UnityEngine;

namespace MageLock.Utilies
{
    public class LoadStep : ScriptableObject
    {
        protected float Progress;

        public virtual async Task LoadTaskAsync()
        {
            await Task.CompletedTask;
        }
    }
}