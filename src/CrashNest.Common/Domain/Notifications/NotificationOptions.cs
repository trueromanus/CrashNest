namespace CrashNest.Common.Domain.Notifications {

    /// <summary>
    /// To whom and how to send a notification.
    /// </summary>
    public record NotificationOptions {

        public NotificationOptionsProvider Provider { get; init; }

        public IEnumerable<string> Senders { get; init; } = Enumerable.Empty<string>();

        public NotificationSettings Settings { get; init; } = new NotificationSettings();

    }

}
