using CrashNest.Common.Models;
using CrashNest.Common.Services;
using Microsoft.Extensions.Options;

namespace CrashNest.Services {

    public class ConfigurationService : IConfigurationService {

        private const string DatabaseField = "Database";

        private readonly IOptions<ApplicationSettingsModel> m_options;

        public ConfigurationService (IOptions<ApplicationSettingsModel> options) => m_options = options ?? throw new ArgumentNullException ( nameof ( options ) );

        public string DatabaseConnectionString () {
            var variable = Environment.GetEnvironmentVariable ( DatabaseField );
            if ( !string.IsNullOrEmpty ( variable ) ) return variable;

            if ( !string.IsNullOrEmpty ( m_options.Value.Database ) ) return m_options.Value.Database;

            //TODO: support external configuration storages???

            throw new NotSupportedException ( "All conditions met but database connection string not found!" );
        }

    }

}
