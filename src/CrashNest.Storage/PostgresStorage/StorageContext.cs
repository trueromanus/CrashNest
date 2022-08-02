using CrashNest.Common.Attributes;
using CrashNest.Common.Storage;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System.Data;
using System.Reflection;

namespace CrashNest.Storage.PostgresStorage {

    /// <summary>
    /// This class need for CU operations where model mapped to sql values.
    /// </summary>
    public class PostgresCompilerWithoutBraces : PostgresCompiler {

        /// <summary>
        /// This method overrides for stay all identifiers as is (test can't be turn on to "test").
        /// </summary>
        /// <param name="value">Identifier.</param>
        public override string WrapValue ( string value ) => value;

    }

    /// <summary>
    /// Storage context for postgres database.
    /// </summary>
    public class StorageContext : IStorageContext {

        private static readonly PostgresCompilerWithoutBraces m_compilerWithoutBraces = new ();

        private readonly string m_connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=test";

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

            var idValue = GetIdValueForItem ( item, idProperty );

            var isNew = idValue == Guid.Empty;

            var values = new Dictionary<string, object> ();
            var properties = itemType.GetProperties ( BindingFlags.Public | BindingFlags.Instance );
            if ( isNew ) properties = properties.Where ( a => a.Name != "Id" ).ToArray ();

            var iterator = 0;
            foreach ( var property in properties ) {
                var value = property.GetGetMethod ()?.Invoke ( item, null ) ?? "NULL";
                values[$"@_param{iterator}"] = value is Enum ? Convert.ToInt32(value) : value;
                iterator++;
            }

            var valueAliases = string.Join(
                ", ",
                Enumerable
                    .Repeat ( "", properties.Count () )
                    .Select ( ( _, i ) => $"@_param{i}" )
                    .ToArray ()
            );

            if ( isNew ) {
                await ExecuteNonResult (
                    $"INSERT INTO {tableName} ({string.Join(", ",properties.Select(a => a.Name))}) VALUES ({valueAliases})",
                    values
                );
            } else {
                await ExecuteNonResult (
                    $"UPDATE {tableName} SET {string.Join ( ", ", properties.Select ( (a, i) => a.Name + $" = @_param{i}" ) )} WHERE Id = '{idValue}'",
                    values
                );
            }
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
