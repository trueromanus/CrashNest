using CrashNest.Common.Domain.Notifications;

namespace CrashNest.Common.Services {

    /// <summary>
    /// Setup and prepare to send notification service.
    /// </summary>
    public interface INotificationService {

        /// <summary>
        /// Send notification.
        /// </summary>
        /// <param name="content">Content for notification.</param>
        /// <param name="options">Options for sending.</param>
        /// <param name="urgent">Is urgent notification.</param>
        Task SendNotification (string content, IEnumerable<NotificationOptions> options, bool urgent);

    }

}
