namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Report with metadata filters.
    /// </summary>
    public record ReportFilterListWithMetadataModel : ReportFilterListModel {

        /// <summary>
        /// Included fields from metadata.
        /// </summary>
        public IEnumerable<string> IncludedFields { get; set; } = Enumerable.Empty<string>();

    }

}
