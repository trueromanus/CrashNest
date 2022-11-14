namespace CrashNest.Common.Domain.Notifications {

    /// <summary>
    /// Settings for sending email notifications.
    /// </summary>
    public record EmailNotificationSettings : NotificationSettings {

        public string Host { get; init; } = "";

        public int Port { get; init; }

        public string? Login { get; init; }

        public string? Password { get; init; }

        public int Timeout { get; init; }

    }

}
