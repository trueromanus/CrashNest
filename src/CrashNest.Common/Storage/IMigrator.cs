namespace CrashNest.Storage.Migrator {

    /// <summary>
    /// API for working with Migrator.
    /// </summary>
    public interface IMigrator : IDisposable {

        /// <summary>
        /// Apply all not existing migrations.
        /// </summary>
        Task ApplyMigrations ();

        /// <summary>
        /// Revert all existing migrations.
        /// </summary>
        Task RevertMigrations ();

        /// <summary>
        /// Revert for migration specified in id parameter.
        /// </summary>
        /// <param name="id">Migration Identifier.</param>
        Task RevertToMigration ( string id );

        /// <summary>
        /// Revert only single migration.
        /// </summary>
        /// <param name="id">Migration Identifier.</param>
        Task RevertSingleMigration ( string id );

    }

}
