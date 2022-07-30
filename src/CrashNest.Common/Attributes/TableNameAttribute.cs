namespace CrashNest.Common.Attributes {

    /// <summary>
    /// Attributes for decоrating entity classes and declare real table name in database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute {

        public TableNameAttribute (string tableName) => TableName = tableName;

        public string TableName { get; private set; }

    }

}
