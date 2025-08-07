namespace MageLock.DependencyInjection
{
    public abstract class Installer : IInstaller
    {
        public abstract void InstallBindings(DIContainer container);
    }
}