using CrashNest.Common.Domain;

namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Report filters.
    /// </summary>
    public record ReportFilterListModel {

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ReportFilterListMode ListMode { get; set; }

        public IEnumerable<ErrorReportType> ErrorTypes { get; set; } = Enumerable.Empty<ErrorReportType>();

        public string Message { get; set; } = "";

        public IEnumerable<int> Codes { get; set; } = Enumerable.Empty<int> ();

        public IEnumerable<MetadataStringFilterModel> MetadataStringFilters { get; set; } = Enumerable.Empty<MetadataStringFilterModel> ();

        public IEnumerable<MetadataNumberFilterModel> MetadataNumberFilters { get; set; } = Enumerable.Empty<MetadataNumberFilterModel> ();

    }

}
