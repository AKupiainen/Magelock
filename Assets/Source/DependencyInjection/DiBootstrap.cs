using UnityEngine;

namespace MageLock.DependencyInjection
{
    public class DIBootstrap : MonoBehaviour
    {
        [SerializeField] private MonoInstaller[] installers;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (installers != null)
            {
                foreach (var installer in installers)
                {
                    if (installer != null)
                    {
                        installer.InstallBindings(DIContainer.Instance);
                    }
                }
            }

            Debug.Log("DI Container initialized!");
        }
    }
}