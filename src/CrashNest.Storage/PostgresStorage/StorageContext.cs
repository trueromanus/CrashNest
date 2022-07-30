using CrashNest.Common.Attributes;
using CrashNest.Common.Storage;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System.Data;
using System.Reflection;

namespace CrashNest.Storage.PostgresStorage {

    /// <summary>
    /// Storage context for postgres database.
    /// </summary>
    public class StorageContext : IStorageContext {

        private static readonly PostgresCompiler m_compiler = new ();

        private readonly string m_connectionString = "";

        public async Task ExecuteNonResult ( string command, IDictionary<string, object> parameters ) {
            await using var connection = new NpgsqlConnection ( m_connectionString );
            await connection.OpenAsync ();

            if ( connection.FullState != ConnectionState.Open ) throw new Exception ( "Can't connecting to postgres database." );

            await using var cmd = new NpgsqlCommand ( command, connection );
            if (parameters != null) {
                foreach ( var parameter in parameters ) {
                    cmd.Parameters.AddWithValue ( parameter.Key, parameter.Value );
                }
            }

            await cmd.ExecuteNonQueryAsync ();
        }

        public async Task AddOrUpdate<T> ( T item ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            var itemType = item.GetType ();
            var tableNameAttibute = itemType
                .GetCustomAttributes ( false )
                .Where ( a => a is TableNameAttribute )
                .OfType<TableNameAttribute> ()
                .FirstOrDefault ();
            if ( tableNameAttibute == null ) {
                throw new ArgumentNullException ( $"The element {item.GetType ().Name} does not have a TableName attribute. Please add an attribute to understand which table name to use." );
            }
            var tableName = tableNameAttibute.TableName;

            var idProperty = itemType.GetProperty ( "Id" );
            if (idProperty == null) throw new ArgumentNullException ( $"The element {item.GetType ().Name} does not have a Id property!" );

            var query = new Query ( tableName );

            var idValue = GetIdValueForItem ( item, idProperty );
            var isNew = idValue == Guid.Empty;
            if ( isNew ) {
                query.AsInsert ( item );
            } else {
                query.Where ( "id", idValue ).AsUpdate ( item );
            }

            var result = m_compiler.Compile ( query );

            await ExecuteNonResult ( result.Sql, result.NamedBindings );
        }

        private static Guid GetIdValueForItem<T> ( T item, PropertyInfo idProperty ) {
            var getMethod = idProperty.GetGetMethod ();
            if ( getMethod == null ) throw new ArgumentException ( $"Method Get for Id property is incorrect in {item.GetType ().Name}!" );
            var propertyValue = getMethod.Invoke ( item, null );
            if ( propertyValue == null ) throw new ArgumentException ( $"Method Get for Id property returned null value which is incorrect for {item.GetType ().Name}!" );

            return (Guid) propertyValue;
        }

    }

}
