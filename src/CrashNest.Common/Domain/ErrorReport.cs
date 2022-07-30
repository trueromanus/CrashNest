﻿namespace CrashNest.Common.Domain {

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
        /// Information about environment and so on.
        /// </summary>
        public ErrorReportMetadata? Metadata { get; set; }

    }

}
