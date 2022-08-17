using Npgsql;
using System.Data;

namespace CrashNest.Storage.Migrator {

    /// <summary>
    /// PosgtresSQL migrator.
    /// </summary>
    public class Migrator : IMigrator {

        private readonly string m_connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=test";

        private NpgsqlConnection? m_connection;

        private NpgsqlTransaction? m_transaction;

        private async Task InitiateConnection () {
            m_connection = new NpgsqlConnection ( m_connectionString );
            await m_connection.OpenAsync ();

            if ( m_connection.FullState != ConnectionState.Open ) throw new Exception ( "Can't connecting to postgres database." );

            m_transaction = await m_connection.BeginTransactionAsync ();
            await CreateMigrationTable ();
        }

        private async Task CloseConnection () {
            if ( m_transaction != null ) {
                await m_transaction.CommitAsync ();
                await m_transaction.DisposeAsync ();
            }
            if ( m_connection != null ) await m_connection.DisposeAsync ();
        }

        private const string m_createMigrationTable =
@"
CREATE TABLE IF NOT EXISTS migrations(
    timestamp int4 NOT NULL PRIMARY KEY,
    issue int4,
    description text NOT NULL,
    created timestamp(6) NOT NULL DEFAULT now()
);
";

        private async Task CreateMigrationTable () {
            await using var cmd = new NpgsqlCommand ( m_createMigrationTable, m_connection, m_transaction );

            await cmd.ExecuteNonQueryAsync ();
        }

        private async Task CreateMigrationRecord (int migrationId, string description, int? issue) {
            await using var cmd = new NpgsqlCommand ( "INSERT INTO migrations (timestamp, description, issue) VALUES (@_param1, @_param2, @_param3)", m_connection, m_transaction );

            cmd.Parameters.AddWithValue ( "@_param1", migrationId );
            cmd.Parameters.AddWithValue ( "@_param2", description );
            cmd.Parameters.AddWithValue ( "@_param3", issue.HasValue ? issue.Value : DBNull.Value );

            await cmd.ExecuteNonQueryAsync ();
        }

        private async Task DeleteMigrationRecord ( int migrationId ) {
            await using var cmd = new NpgsqlCommand ( "DELETE FROM migrations WHERE timestamp = @_param1", m_connection, m_transaction );

            cmd.Parameters.AddWithValue ( "@_param1", migrationId );

            await cmd.ExecuteNonQueryAsync ();
        }

        private async Task<HashSet<int>> GetAppliedMigrations() {
            await using var cmd = new NpgsqlCommand ( "SELECT timestamp FROM migrations ORDER BY timestamp ASC", m_connection, m_transaction );
            using var reader = await cmd.ExecuteReaderAsync ();
            HashSet<int> appliedMigration = new ();
            while ( await reader.ReadAsync () ) {
                appliedMigration.Add ( reader.GetInt32 ( 0 ) );
            }
            await reader.CloseAsync ();
            return appliedMigration;
        }

        private static IEnumerable<MigrationClassModel> GetMigrationClasses() {
            return typeof ( Migration ).Assembly.GetTypes ()
                .Where ( a => a.IsSubclassOf ( typeof ( Migration ) ) )
                .Select (
                    a => {
                        var arguments = a.GetCustomAttributesData ().First ()?.ConstructorArguments ?? null;
                        if ( arguments == null || !arguments.Any () || arguments.Count () < 2 ) throw new NotSupportedException ( "Migration without attribute MigrationAttribute not supported!" );

                        var migrationId = arguments.First ();
                        var description = arguments.ElementAt ( 1 );
                        var issue = arguments.Count () > 2 ? (int?) arguments.ElementAt ( 2 ).Value : null;

                        return new MigrationClassModel {
                            Migration = a,
                            MigrationId = migrationId.Value != null ? (int) migrationId.Value : -1,
                            Description = description.Value != null ? (string) description.Value : "",
                            Issue = issue
                        };
                    }
                )
                .ToList ();
        }

        public async Task ApplyMigrations () {
            await InitiateConnection ();
            var appliedMigration = await GetAppliedMigrations ();
            var migrations = GetMigrationClasses ()
                .OrderBy ( a => a.MigrationId )
                .ToList ();

            foreach ( var migrationEntry in migrations ) {
                if ( appliedMigration.Contains ( migrationEntry.MigrationId ) ) continue;

                await using var migrationCommand = new NpgsqlCommand ( migrationEntry.GetUpSql (), m_connection, m_transaction );
                await migrationCommand.ExecuteNonQueryAsync ();
                await CreateMigrationRecord ( migrationEntry.MigrationId, migrationEntry.Description, migrationEntry.Issue );
            }

            await CloseConnection ();
        }

        public async Task RevertMigrations () {
            await InitiateConnection ();
            var appliedMigration = await GetAppliedMigrations ();
            var migrations = GetMigrationClasses ()
                .OrderByDescending ( a => a.MigrationId )
                .ToList ();

            foreach ( var migrationEntry in migrations ) {
                if ( !appliedMigration.Contains ( migrationEntry.MigrationId ) ) continue;

                await using var migrationCommand = new NpgsqlCommand ( migrationEntry.GetDownSql (), m_connection, m_transaction );
                await migrationCommand.ExecuteNonQueryAsync ();
                await DeleteMigrationRecord ( migrationEntry.MigrationId );
            }

            await CloseConnection ();
        }

        public async Task RevertSingleMigration ( int id ) {
            await InitiateConnection ();
            var appliedMigration = await GetAppliedMigrations ();
            var migrations = GetMigrationClasses ()
                .OrderByDescending ( a => a.MigrationId )
                .ToList ();

            var migration = migrations.FirstOrDefault ( a => a.MigrationId == id );
            if ( migration == null ) throw new ArgumentException ( $"Migration witn number {id} doesn't exists!" );

            if ( appliedMigration.Contains ( id ) ) {
                await using var migrationDownCommand = new NpgsqlCommand ( migration.GetDownSql (), m_connection, m_transaction );
                await migrationDownCommand.ExecuteNonQueryAsync ();
                await DeleteMigrationRecord ( migration.MigrationId );
            }

            await using var migrationUpCommand = new NpgsqlCommand ( migration.GetUpSql (), m_connection, m_transaction );
            await migrationUpCommand.ExecuteNonQueryAsync ();
            await CreateMigrationRecord ( migration.MigrationId, migration.Description, migration.Issue );

            await CloseConnection ();
        }

        public async Task RevertToMigration ( int id ) {
            await InitiateConnection ();
            var appliedMigration = await GetAppliedMigrations ();
            var migrations = GetMigrationClasses ()
                .OrderByDescending ( a => a.MigrationId )
                .ToList ();

            foreach ( var migrationEntry in migrations ) {
                if ( migrationEntry.MigrationId < id ) break;
                if ( !appliedMigration.Contains ( migrationEntry.MigrationId )) continue;

                await using var migrationCommand = new NpgsqlCommand ( migrationEntry.GetDownSql (), m_connection, m_transaction );
                await migrationCommand.ExecuteNonQueryAsync ();
                await DeleteMigrationRecord ( migrationEntry.MigrationId );
            }

            await CloseConnection ();
        }

        public void Dispose () {
            if ( m_connection != null ) m_connection.Dispose ();
        }

    }

}
