namespace CrashNest.Common.Attributes {


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute {

        /// <summary>
        /// Unique Indetifier.
        /// </summary>
        public int Timestamp { get; private set; }

        /// <summary>
        /// Issue number in bugtracker.
        /// </summary>
        public int? Issue { get; private set; }

        /// <summary>
        /// Short description of changes for database structure in migration.
        /// </summary>
        public string Description { get; private set; } = "";

        public MigrationAttribute (int timestamp, string description) {
            Timestamp = timestamp;
            Description = description;
        }

        public MigrationAttribute ( int timestamp, string description, int issue ) {
            Timestamp = timestamp;
            Description = description;
            Issue = issue;
        }

    }

}
