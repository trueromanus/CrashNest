
using CrashNest.Common.Storage;
using CrashNest.Storage.PostgresStorage;

public static class ServiceRegistration {

    /// <summary>
    /// Registrate all services for DI injections.
    /// </summary>
    /// <param name="services">Services content.</param>
    public static void RegistrateServices ( IServiceCollection services ) {
        services.AddScoped<IStorageContext, StorageContext> ();
    }

}

