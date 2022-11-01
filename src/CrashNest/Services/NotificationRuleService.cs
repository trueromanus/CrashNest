using CrashNest.Common.Domain;
using CrashNest.Common.Services;
using CrashNest.Common.Storage;
using SqlKata;
using System.Diagnostics;
using System.Text.Json;

namespace CrashNest.Services {

    public class NotificationRuleService : INotificationRuleService {
        
        private readonly IStorageContext m_storageContext;

        public NotificationRuleService ( IStorageContext storageContext ) => m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );

        public async Task SendNotificationIfMetConditions ( ErrorReport report, IEnumerable<ErrorReportMetadata> metadata ) {
            var rules = await m_storageContext.GetAsync<NotificationRule> ( new Query ( nameof ( NotificationRule ) ) );

            foreach ( var rule in rules ) {
                if ( rule.Conditions == null ) continue;

                var isMet = false;
                foreach ( var condition in rule.Conditions.Conditions ) {
                    if ( condition == null) continue;

                    switch (condition.Operation) {
                        case "=":
                            var metadataField = metadata.FirstOrDefault ( a => a.Name == condition.Field );
                            if ( metadataField != null && condition.Value != null) {
                                var jsonElement = (JsonElement) condition.Value;
                                isMet = jsonElement.ValueKind == JsonValueKind.Number ? metadataField.IntValue == jsonElement.GetInt32() : metadataField.StringValue == condition.Value?.ToString ();
                            }
                            break;
                        default: throw new NotSupportedException ( $"Condition operation not supported {condition.Operation}" );
                    }

                    if ( isMet ) break;
                }

                if ( isMet ) {
                    //TODO: start real sending notification
                }
            }
        }

    }

}
