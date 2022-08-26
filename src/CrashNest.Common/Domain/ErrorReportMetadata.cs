using CrashNest.Common.Attributes;

namespace CrashNest.Common.Domain {

    /// <summary>
    /// Information about environment and so on.
    /// </summary>
    [TableName ( nameof( ErrorReportMetadata ) )]
    public class ErrorReportMetadata {

        /// <summary>
        /// Identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of metadata item.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// String value.
        /// </summary>
        public string? StringValue { get; set; }

        /// <summary>
        /// Int value.
        /// </summary>
        public int? IntValue { get; set; }

        /// <summary>
        /// Error report identifier.
        /// </summary>
        public Guid? ErrorReportId { get; set; }

    }

}