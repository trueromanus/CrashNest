using CrashNest.Common.Domain;

namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Model contains fields reuired for registration error report.
    /// </summary>
    public record RegisterReportModel {

        public ErrorReport Report { get; init; } = new ErrorReport();

        public ErrorReportMetadata Metadata { get; init; } = new ErrorReportMetadata();

    }

}
