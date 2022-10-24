namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Filter only for number value in metadata.
    /// </summary>
    public class MetadataNumberFilterModel {

        public string Name { get; set; } = "";

        public int? Value { get; set; }

        public int? Start { get; set; }

        public int? End { get; set; }

    }

}