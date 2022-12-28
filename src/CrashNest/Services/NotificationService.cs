using CrashNest.Common.Domain.Notifications;
using CrashNest.Common.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CrashNest.Services {

    public class NotificationService : INotificationService {

        public Task SendNotification ( string content, IEnumerable<NotificationOptions> options, bool urgent ) {
            foreach ( var option in options ) {
                switch ( option.Provider ) {
                    case NotificationOptionsProvider.Email:
                        return SendEmailMessage ( content, option, urgent );
                }
            }
            return Task.CompletedTask;
        }

        private static async Task SendEmailMessage ( string content, NotificationOptions option, bool urgent ) {
            var emailSettings = option.Settings as EmailNotificationSettings;
            if ( emailSettings == null ) throw new ArgumentException ( "Sending Email must be with type EmailNotificationSettings!" );

            var message = new MimeMessage ();
            message.From.Add ( new MailboxAddress ( "CrashNest Notification", emailSettings.FromAddress ) ); ;
            message.To.AddRange ( option.Senders.Select ( a => new MailboxAddress ( "Recipient", a ) ) );
            message.Subject = urgent ? "Urgent CrashNest Notification" : "CrashNest Notification";
            message.Priority = urgent ? MessagePriority.Urgent : MessagePriority.Normal;
            message.Body = new TextPart ( "plain" ) { Text = content };

            using var client = new SmtpClient ();

            try {
                client.Connect ( emailSettings.Host, emailSettings.Port, emailSettings.Secure ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None );

                client.Authenticate ( emailSettings.Login, emailSettings.Password );

                await client.SendAsync ( message );

                client.Disconnect ( true );
            } catch ( Exception e ) {
                Console.WriteLine ( e.Message );
            }
        }

    }

}
