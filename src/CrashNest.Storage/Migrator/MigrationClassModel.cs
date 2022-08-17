namespace CrashNest.Storage.Migrator {

    /// <summary>
    /// Model for describing migration class.
    /// </summary>
    public sealed class MigrationClassModel {

        /// <summary>
        /// Type of migration class.
        /// </summary>
        public Type Migration { get; set; } = typeof ( object );

        /// <summary>
        /// Migration Identifier.
        /// </summary>
        public int MigrationId { get; set; }

        /// <summary>
        /// Short description.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Issue number.
        /// </summary>
        public int? Issue { get; set; }

        public string GetUpSql () {
            var migration = GetMigrationInstance ();
            migration.Apply ();

            return migration.GetSql ();
        }

        public string GetDownSql () {
            var migration = GetMigrationInstance ();
            migration.Revert ();

            return migration.GetSql ();
        }

        private Migration GetMigrationInstance () {
            var migrationInstance = Activator.CreateInstance ( Migration );
            if ( migrationInstance == null ) throw new NotSupportedException ( $"Class {Migration.FullName} must be have default constructor or issue in constructor!" );

            return (Migration) migrationInstance;
        }
    }

}