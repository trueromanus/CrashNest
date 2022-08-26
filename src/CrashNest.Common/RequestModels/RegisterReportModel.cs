using CrashNest.Common.Domain;

namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Model contains fields reuired for registration error report.
    /// </summary>
    public record RegisterReportModel {

        public ErrorReport Report { get; init; } = new ErrorReport();

        public IEnumerable<ErrorReportMetadata> Metadata { get; init; } = Enumerable.Empty<ErrorReportMetadata>();

    }

}
