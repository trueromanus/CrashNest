using CrashNest.Common.Attributes;
using CrashNest.Common.Storage;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Transactions;

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

        private NpgsqlTransaction? m_transaction;

        private NpgsqlConnection? m_connection;

        private static async Task OpenConnection ( NpgsqlConnection connection ) {
            await connection.OpenAsync ();

            if ( connection.FullState != ConnectionState.Open ) throw new Exception ( "Can't connecting to postgres database." );
        }
        private static void FillParameters ( IDictionary<string, object> parameters, NpgsqlCommand cmd ) {
            if ( parameters != null ) {
                foreach ( var parameter in parameters ) {
                    cmd.Parameters.AddWithValue ( parameter.Key, parameter.Value );
                }
            }
        }
        private static void GetItemCollections<T> ( T item, Type itemType, bool isNew, out Dictionary<string, object> values, out PropertyInfo[] properties, out string valueAliases ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            values = new Dictionary<string, object> ();
            properties = itemType.GetProperties ( BindingFlags.Public | BindingFlags.Instance );
            if ( isNew ) properties = properties.Where ( a => a.Name != "Id" ).ToArray ();

            var iterator = 0;
            foreach ( var property in properties ) {
                var value = property.GetGetMethod ()?.Invoke ( item, null ) ?? null;
                var key = $"@_param{iterator}";
                if ( value == null ) {
                    values[key] = DBNull.Value;
                } else {
                    values[key] = value is Enum ? Convert.ToInt32 ( value ) : value;
                }
                iterator++;
            }

            valueAliases = string.Join (
                ", ",
                Enumerable
                    .Repeat ( "", properties.Count () )
                    .Select ( ( _, i ) => $"@_param{i}" )
                    .ToArray ()
            );
        }

        private static PropertyInfo GetIdProperty<T> ( T item, Type itemType ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            var idProperty = itemType.GetProperty ( "Id" );
            if ( idProperty == null ) throw new ArgumentNullException ( $"The element {item.GetType ().Name} does not have a Id property!" );
            return idProperty;
        }

        private static void GetTableName<T> ( T item, out Type itemType, out string tableName ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            itemType = item.GetType ();
            var tableNameAttibute = itemType
                .GetCustomAttributes ( false )
                .Where ( a => a is TableNameAttribute )
                .OfType<TableNameAttribute> ()
                .FirstOrDefault ();
            if ( tableNameAttibute == null ) {
                throw new ArgumentNullException ( $"The element {item.GetType ().Name} does not have a TableName attribute. Please add an attribute to understand which table name to use." );
            }
            tableName = tableNameAttibute.TableName;
        }

        private static Guid GetIdValueForItem<T> ( T item, PropertyInfo idProperty ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            var getMethod = idProperty.GetGetMethod ();
            if ( getMethod == null ) throw new ArgumentException ( $"Method Get for Id property is incorrect in {item.GetType ().Name}!" );
            var propertyValue = getMethod.Invoke ( item, null );
            if ( propertyValue == null ) throw new ArgumentException ( $"Method Get for Id property returned null value which is incorrect for {item.GetType ().Name}!" );

            return (Guid) propertyValue;
        }
        private async Task<NpgsqlConnection> GetConnection () {
            NpgsqlConnection? connection = null;
            if ( m_connection == null ) {
                connection = new NpgsqlConnection ( m_connectionString );
                await OpenConnection ( connection );
            } else {
                connection = m_connection;
            }

            return connection;
        }

        public async Task ExecuteNonResult ( string command, IDictionary<string, object> parameters ) {
            var connection = await GetConnection ();

            await using var cmd = m_transaction != null ? new NpgsqlCommand ( command, connection, m_transaction ) : new NpgsqlCommand ( command, connection );
            FillParameters ( parameters, cmd );

            await cmd.ExecuteNonQueryAsync ();
        }


        private async Task<Guid> ExecuteWithSingleResultAsGuid ( string command, IDictionary<string, object> parameters ) {
            var connection = await GetConnection ();

            await using var cmd = new NpgsqlCommand ( command, connection );
            FillParameters ( parameters, cmd );

            using var reader = await cmd.ExecuteReaderAsync ();
            Guid result = Guid.Empty;
            if ( await reader.ReadAsync () ) {
                result = reader.GetGuid ( 0 );
            }
            await reader.CloseAsync ();

            return result;
        }

        private async Task BeginTransaction () {
            m_connection = new NpgsqlConnection ( m_connectionString );
            await OpenConnection ( m_connection );

            m_transaction = await m_connection.BeginTransactionAsync ();
        }

        private async Task CommitTransation () {
            if ( m_transaction == null ) return;

            await m_transaction.CommitAsync ();
            await m_transaction.DisposeAsync ();
        }

        public async Task AddOrUpdate<T> ( T item ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            GetTableName ( item, out Type itemType, out string tableName );

            var idProperty = GetIdProperty ( item, itemType );
            var idValue = GetIdValueForItem ( item, idProperty );

            var isNew = idValue == Guid.Empty;
            GetItemCollections ( item, itemType, isNew, out Dictionary<string, object> values, out PropertyInfo[] properties, out string valueAliases );

            if ( isNew ) {
                var id = await ExecuteWithSingleResultAsGuid (
                    $"INSERT INTO {tableName} ({string.Join ( ", ", properties.Select ( a => a.Name ) )}) VALUES ({valueAliases}) RETURNING Id",
                    values
                );
                idProperty.GetSetMethod ()?.Invoke ( item, new object[] { id } );
            } else {
                await ExecuteNonResult (
                    $"UPDATE {tableName} SET {string.Join ( ", ", properties.Select ( ( a, i ) => a.Name + $" = @_param{i}" ) )} WHERE Id = '{idValue}'",
                    values
                );
            }
        }

        public async Task MultiAddOrUpdate<T> ( IEnumerable<T> items ) {
            if ( items == null ) throw new ArgumentNullException ( nameof ( items ) );
            if ( !items.Any () ) throw new ArgumentException ( "Collection in parameter items is empty." );

            await BeginTransaction ();

            foreach ( var item in items ) {
                GetTableName ( item, out Type itemType, out string tableName );
                var idProperty = GetIdProperty ( item, itemType );
                var idValue = GetIdValueForItem ( item, GetIdProperty ( item, itemType ) );

                var isNew = idValue == Guid.Empty;
                GetItemCollections ( item, itemType, isNew, out Dictionary<string, object> values, out PropertyInfo[] properties, out string valueAliases );


                if ( isNew ) {
                    var id = await ExecuteWithSingleResultAsGuid (
                        $"INSERT INTO {tableName} ({string.Join ( ", ", properties.Select ( a => a.Name ) )}) VALUES ({valueAliases}) RETURNING Id",
                        values
                    );
                    idProperty.GetSetMethod ()?.Invoke ( item, new object[] { id } );
                } else {
                    await ExecuteNonResult (
                        $"UPDATE {tableName} SET {string.Join ( ", ", properties.Select ( ( a, i ) => a.Name + $" = @_param{i}" ) )} WHERE Id = '{idValue}'",
                        values
                    );
                }
            }

            await CommitTransation ();
        }

    }

}
