using CrashNest.Common.Domain.Notifications;
using CrashNest.Common.Services;

namespace CrashNest.Services {

    public class NotificationService : INotificationService {

        public Task SendNotification ( string content, IEnumerable<NotificationOptions> options ) {
            return Task.CompletedTask;
        }

    }

}
