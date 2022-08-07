namespace CrashNest.Storage.Migrator {

    public class Migration {

        private readonly List<string> m_commands = new();

        protected void ExecuteCommand(string command) => m_commands.Add(command);

        /// <summary>
        /// Get all sql as single command.
        /// </summary>
        public string GetSql () => string.Join(";", m_commands);

        protected virtual void Up() {
        }

        protected virtual void Down () {
        }

        public void Apply () => Up ();

        public void Revert () => Down ();

    }

}
