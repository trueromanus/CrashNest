using CrashNest.Common.Domain.Notifications;

namespace CrashNest.Common.Domain.NotificationRuleConditions {

    /// <summary>
    /// Send settings model.
    /// </summary>
    public record SendSettings {

        /// <summary>
        /// Collection options for sending notification.
        /// </summary>
        public IEnumerable<NotificationOptions> Options { get; init; } = Enumerable.Empty<NotificationOptions> ();

        /// <summary>
        /// Message.
        /// </summary>
        public string Message { get; init; } = ""; // Error is happend: [ErrorCode] with message "[Message]" \n Additional metadata:\n[Metadata:Value]

    }

}
