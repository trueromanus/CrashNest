using CrashNest.Common.Attributes;

namespace CrashNest.Common.Domain {

    [TableName ( nameof ( ErrorReport ) )]
    public class ErrorReport {

        /// <summary>
        /// Identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// Type of error.
        /// </summary>
        public ErrorReportType ErrorType { get; set; } = ErrorReportType.Unknown;

        /// <summary>
        /// Short message containing few information about error.
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Error as number.
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// Content of stack trace.
        /// </summary>
        public string StackTrace { get; set; } = "";

        /// <summary>
        /// Created timestamp.
        /// </summary>
        public DateTime Created { get; set; }

    }

}
