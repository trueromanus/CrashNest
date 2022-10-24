namespace CrashNest.Common.RequestModels {

    /// <summary>
    /// Filter only for string value in metadata.
    /// </summary>
    public class MetadataStringFilterModel {

        public string Name { get; set; } = "";

        public string Value { get; set; } = "";

    }

}