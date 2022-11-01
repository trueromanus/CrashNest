using CrashNest.Common.Domain;

namespace CrashNest.Common.Services {

    /// <summary>
    /// Service for working with <see cref="NotificationRule"/>.
    /// </summary>
    public interface INotificationRuleService {

        /// <summary>
        /// Send notification is is met conditions.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <param name="metadata">Metadata.</param>
        public Task SendNotificationIfMetConditions ( ErrorReport report, IEnumerable<ErrorReportMetadata> metadata );

    }

}
