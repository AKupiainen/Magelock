using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace BrawlLine.Utilies
{
    public class LoadingSystem : MonoBehaviour
    {
        [SerializeField] private List<LoadStep> loadSteps = new();

        private void Start()
        {  
            LoadTasks();
        }
        
        private async void LoadTasks()
        {
            int totalTasks = loadSteps.Count;

            for (int i = 0; i < totalTasks; i++)
            {
                var loadStep = loadSteps[i];

                Task stepTask = loadStep.LoadTaskAsync();

                while (!stepTask.IsCompleted)
                {
                    await Task.Yield();
                }
            }
        }
    }
}