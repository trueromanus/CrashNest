namespace CrashNest.Common.Storage {

    /// <summary>
    /// Storage context.
    /// </summary>
    public interface IStorageContext {

        /// <summary>
        /// Execute non result command.
        /// </summary>
        /// <param name="command">Command as string.</param>
        /// <param name="parameters">Parameters.</param>
        public Task ExecuteNonResult ( string command, IDictionary<string, object> parameters );

        /// <summary>
        /// Add or update item declared in generic type.
        /// </summary>
        /// <param name="item">Item for saving process.</param>
        public Task AddOrUpdate<T> ( T item );

        /// <summary>
        /// Add or update items declared in generic type.
        /// </summary>
        /// <param name="item">Item collection for saving process.</param>
        public Task MultiAddOrUpdate<T> ( IEnumerable<T> items );

    }

}
