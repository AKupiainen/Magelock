using UnityEngine;

namespace MageLock.DependencyInjection
{
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        public abstract void InstallBindings(DIContainer container);
    }
}