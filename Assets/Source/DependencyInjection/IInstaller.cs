namespace MageLock.DependencyInjection
{
    public interface IInstaller
    {
        void InstallBindings(DIContainer container);
    }
}