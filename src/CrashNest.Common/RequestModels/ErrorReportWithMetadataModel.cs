using CrashNest.Common.Domain;

namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Error report model containing metadata.
    /// </summary>
    public class ErrorReportWithMetadataModel : ErrorReport {

        /// <summary>
        /// Metadata fields.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object> ();

    }

}
