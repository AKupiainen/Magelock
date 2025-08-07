using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MageLock.Utilies
{
    [CreateAssetMenu(fileName = "SceneLoadStep", menuName = "Load Step/Scene Load Step")]
    public class SceneLoadStep : LoadStep
    {
        [SerializeField]
        private List<string> sceneNames;

        public override async Task LoadTaskAsync()
        {
            float sceneProgressIncrement = 1f / sceneNames.Count;
            float currentSceneProgress = 0f;

            foreach (string sceneName in sceneNames)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

                while (asyncLoad is { isDone: false })
                {
                    Progress = currentSceneProgress + (asyncLoad.progress / sceneNames.Count);
                    await Task.Yield();
                }

                currentSceneProgress += sceneProgressIncrement;
            }
            
            Progress = 1f;
        }
    }
}