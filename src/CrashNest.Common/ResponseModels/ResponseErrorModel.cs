namespace CrashNest.Common.ResponseModels {

    /// <summary>
    /// Common model for describing crash in API endpoints.
    /// </summary>
    public record ResponseErrorModel (string Message, string ErrorCodeSerilog, string CrashId);

}
