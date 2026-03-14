using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Av.Agent.StartupInspection;

public static class StartupInspectionServiceCollectionExtensions
{
    public static IServiceCollection AddStartupInspection(this IServiceCollection services)
    {
        services.AddSingleton<IStartupEntriesSource, WindowsStartupSources>();
        services.AddSingleton<IExecutableResolver, DefaultExecutableResolver>();
        services.AddSingleton<IHashingService, Sha256HashingService>();
        services.AddSingleton<ISignerReputationService, AuthenticodeSignerReputationService>();
        services.AddSingleton<IQuarantineService>(_ =>
            new FileQuarantineService(Path.Combine(Path.GetTempPath(), "av-agent-quarantine")));
        services.AddSingleton<IBackupService>(_ =>
            new FileBackupRestoreService(Path.Combine(Path.GetTempPath(), "av-agent-backups")));
        services.AddSingleton<IStartupInspectionService, StartupInspectionService>();
        return services;
    }
}
