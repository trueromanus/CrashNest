using CrashNest.Common.Attributes;
using CrashNest.Common.Storage;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System.Data;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using NpgsqlTypes;

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

    public record JsonData (string Json);

    /// <summary>
    /// Storage context for postgres database.
    /// </summary>
    public class StorageContext : IStorageContext {

        private static readonly PostgresCompilerWithoutBraces m_compilerWithoutBraces = new ();

        private readonly string m_connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=test";

        private NpgsqlTransaction? m_transaction;

        private NpgsqlConnection? m_connection;

        private bool m_groupedTransaction;

        private readonly ILogger<StorageContext> m_logger;

        public StorageContext ( ILogger<StorageContext> logger ) => m_logger = logger;

        private static async Task OpenConnection ( NpgsqlConnection connection ) {
            await connection.OpenAsync ();

            if ( connection.FullState != ConnectionState.Open ) throw new Exception ( "Can't connecting to postgres database." );
        }

        private static void FillParameters ( IDictionary<string, object> parameters, NpgsqlCommand cmd ) {
            if ( parameters != null ) {
                foreach ( var parameter in parameters ) {
                    if (parameter.Value is JsonData ) {
                        cmd.Parameters.AddWithValue (
                            parameter.Key,
                            NpgsqlDbType.Jsonb,
                            ((JsonData) parameter.Value).Json
                        );
                        continue;
                    }
                    cmd.Parameters.AddWithValue (
                        parameter.Key,
                        parameter.Value is Enum ? Convert.ToInt32 ( parameter.Value ) : parameter.Value
                    );
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
                    if ( property.PropertyType?.IsClass == true && property.PropertyType?.FullName?.StartsWith( "CrashNest." ) == true ) {
                        values[key] = new JsonData (
                            JsonSerializer.Serialize (
                                value,
                                property.PropertyType,
                                new JsonSerializerOptions {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                }
                            )
                        );
                    } else {
                        values[key] = value is Enum ? Convert.ToInt32 ( value ) : value;
                    }
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

        private async Task<NpgsqlConnection> GetConnectionAsync () {
            NpgsqlConnection? connection;
            if ( m_connection == null ) {
                connection = new NpgsqlConnection ( m_connectionString );
                await OpenConnection ( connection );
            } else {
                connection = m_connection;
            }

            return connection;
        }

        private NpgsqlConnection GetConnection () {
            NpgsqlConnection? connection;
            if ( m_connection == null ) {
                connection = new NpgsqlConnection ( m_connectionString );
                connection.Open ();
                if ( connection.FullState != ConnectionState.Open ) throw new Exception ( "Can't connecting to postgres database." );
            } else {
                connection = m_connection;
            }

            return connection;
        }

        public async Task ExecuteNonResult ( string command, IDictionary<string, object> parameters ) {
            var connection = await GetConnectionAsync ();

            m_logger.LogInformation ( $"SQL: {command}\n{string.Join ( ", ", parameters.Select ( a => a.Key + "=" + a.Value ) )}" );

            await using var cmd = m_transaction != null ? new NpgsqlCommand ( command, connection, m_transaction ) : new NpgsqlCommand ( command, connection );
            FillParameters ( parameters, cmd );

            await cmd.ExecuteNonQueryAsync ();
        }


        private async Task<Guid> ExecuteWithSingleResultAsGuid ( string command, IDictionary<string, object> parameters ) {
            var connection = await GetConnectionAsync ();

            m_logger.LogInformation ( $"SQL: {command}\n{string.Join ( ", ", parameters.Select ( a => a.Key + "=" + a.Value ) )}" );

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

        private static void SetPropertyInItem<T> ( T item, ref string property, ref object? value, bool isJsonb = false ) {
            if ( item == null ) throw new ArgumentNullException ( nameof ( item ) );

            var valueProperty = item.GetType ().GetProperty ( property, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public );
            if ( valueProperty == null ) throw new ArgumentException ( $"Method Get for {property} property is incorrect in {item.GetType ().Name}!" );

            if ( value == DBNull.Value ) value = null;

            try {
                if ( isJsonb ) {
                    var json = value == null ? "null" : (value.ToString () ?? "null");
                    var jsonObject = JsonSerializer.Deserialize (
                        json,
                        valueProperty.PropertyType,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                    );
                    valueProperty.GetSetMethod ()?.Invoke ( item, new object?[] { jsonObject } );
                    return;
                }

                valueProperty.GetSetMethod ()?.Invoke ( item, new object?[] { value } );
            } catch {
                throw new ArgumentException ( $"Property {property} doesn't match types with value {value}!" );
            }
        }

        private static bool IsJsonb ( NpgsqlDataReader reader, int index ) => reader.GetDataTypeName ( index ) == "jsonb";

        private async Task<IEnumerable<T>> ExecuteWithResultAsCollectionAsync<T> ( string command, IDictionary<string, object> parameters ) where T : new() {
            var connection = await GetConnectionAsync ();

            m_logger.LogInformation ( $"SQL: {command}\n{string.Join ( ", ", parameters.Select ( a => a.Key + "=" + a.Value ) )}" );

            await using var cmd = new NpgsqlCommand ( command, connection );
            FillParameters ( parameters, cmd );

            using var reader = await cmd.ExecuteReaderAsync ();
            var result = new List<T> ();
            while ( await reader.ReadAsync () ) {
                var item = new T ();
                var fieldsCount = reader.FieldCount;
                for ( int i = 0; i < fieldsCount; i++ ) {
                    var fieldName = reader.GetName ( i );
                    var value = reader.GetValue ( i );
                    SetPropertyInItem ( item, ref fieldName, ref value, IsJsonb ( reader, i ) );
                }
                result.Add ( item );
            }
            await reader.CloseAsync ();

            return result;
        }

        private IEnumerable<T> ExecuteWithResultAsCollection<T> ( string command, IDictionary<string, object> parameters ) where T : new() {
            var connection = GetConnection ();

            m_logger.LogInformation ( $"SQL: {command}\n{string.Join ( ", ", parameters.Select ( a => a.Key + "=" + a.Value ) )}" );

            using var cmd = new NpgsqlCommand ( command, connection );
            FillParameters ( parameters, cmd );

            using var reader = cmd.ExecuteReader ();
            var result = new List<T> ();
            while ( reader.Read () ) {
                var item = new T ();
                var fieldsCount = reader.FieldCount;
                for ( int i = 0; i < fieldsCount; i++ ) {
                    var fieldName = reader.GetName ( i );
                    var value = reader.GetValue ( i );
                    SetPropertyInItem ( item, ref fieldName, ref value, IsJsonb ( reader, i ) );
                }
                result.Add ( item );
            }
            reader.Close ();

            return result;
        }


        private async Task BeginTransaction ( bool groupedTransaction = false ) {
            if ( !( m_connection == null && m_transaction == null ) ) return; // if transaction already started not need make new transaction

            m_connection = new NpgsqlConnection ( m_connectionString );
            await OpenConnection ( m_connection );

            m_transaction = await m_connection.BeginTransactionAsync ();
            m_groupedTransaction = groupedTransaction;
        }

        private async Task CommitTransation ( bool groupedTransaction = false ) {
            if ( m_transaction == null ) return;
            if ( m_groupedTransaction == true && !groupedTransaction ) return;

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

        public async Task<IEnumerable<T>> GetAsync<T> ( Query query ) where T : new() {
            if ( query == null ) throw new ArgumentNullException ( nameof ( query ) );

            var compiledQuery = m_compilerWithoutBraces.Compile ( query );
            return await ExecuteWithResultAsCollectionAsync<T> ( compiledQuery.Sql, compiledQuery.NamedBindings );
        }

        public IEnumerable<T> Get<T> ( Query query ) where T : new() {
            if ( query == null ) throw new ArgumentNullException ( nameof ( query ) );

            var compiledQuery = m_compilerWithoutBraces.Compile ( query );
            return ExecuteWithResultAsCollection<T> ( compiledQuery.Sql, compiledQuery.NamedBindings );
        }

        public async Task MakeInTransaction ( Func<Task> action ) {
            await BeginTransaction ( groupedTransaction: true );

            await action ();

            await CommitTransation ( groupedTransaction: true );
        }

    }

}
