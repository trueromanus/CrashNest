using CrashNest.Common.Domain;
using CrashNest.Common.Domain.NotificationRuleConditions;
using CrashNest.Common.Domain.Notifications;
using CrashNest.Common.Services;
using CrashNest.Common.Storage;
using SqlKata;
using System.Text.Json;

namespace CrashNest.Services {

    public class NotificationRuleService : INotificationRuleService {

        private readonly IStorageContext m_storageContext;

        private readonly INotificationService m_notificationService;

        private const string EqualOperator = "=";

        private const string GreatherOperator = ">";

        private const string LesserOperator = "<";

        private const string GreatherOrEqualOperator = ">=";

        private const string LesserOrEqualOperator = "<=";

        private const string StartWithOperator = "startWith";

        private const string EndsWithOperator = "endsWith";

        private const string ContainsOperator = "contains";

        public NotificationRuleService ( IStorageContext storageContext, INotificationService notificationService ) {
            m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );
            m_notificationService = notificationService ?? throw new ArgumentNullException ( nameof ( notificationService ) );
        }

        public async Task SendNotificationIfMetConditions ( ErrorReport report, IEnumerable<ErrorReportMetadata> metadata ) {
            var rules = await m_storageContext.GetAsync<NotificationRule> ( new Query ( nameof ( NotificationRule ) ) );

            foreach ( var rule in rules ) {
                if ( rule.Conditions == null ) continue;

                var isMet = false;
                foreach ( var condition in rule.Conditions.Conditions ) {
                    if ( condition == null ) continue;

                    if ( CompareFields ( metadata, condition ) ) {
                        isMet = true;
                        break;
                    }
                }

                // rule.SendSettings > NotificationOptions

                if ( isMet ) await m_notificationService.SendNotification ( "", new List<NotificationOptions> () );
            }
        }

        private static bool CompareFields ( IEnumerable<ErrorReportMetadata> metadata, RuleCondition condition ) {
            if ( condition.Value == null ) throw new ArgumentNullException ( nameof ( condition.Value ) );

            JsonElement jsonElement = (JsonElement) condition.Value;
            switch ( jsonElement.ValueKind ) {
                case JsonValueKind.String:
                    return CompareStrings ( metadata, jsonElement.GetString () ?? "", condition );
                case JsonValueKind.Number:
                    return CompareIntegers ( metadata, jsonElement.GetInt32 (), condition );
                default: throw new NotSupportedException ( "Not supported type for notification rule condition!" );
            }
        }

        private static bool CompareIntegers ( IEnumerable<ErrorReportMetadata> metadata, int conditionValue, RuleCondition condition ) {
            var metadataField = metadata.FirstOrDefault ( a => a.Name == condition.Field );
            if ( metadataField == null ) return false;

            switch ( condition.Operation ) {
                case EqualOperator: {
                    return metadataField.IntValue == conditionValue;
                }
                case GreatherOperator: {
                    return metadataField.IntValue > conditionValue;
                }
                case LesserOperator: {
                    return metadataField.IntValue < conditionValue;
                }
                case GreatherOrEqualOperator: {
                    return metadataField.IntValue >= conditionValue;
                }
                case LesserOrEqualOperator: {
                    return metadataField.IntValue <= conditionValue;
                }
                default: throw new NotSupportedException ( $"CompareIntegers operation {condition.Operation} not supported!" );
            }
        }

        private static bool CompareStrings ( IEnumerable<ErrorReportMetadata> metadata, string conditionValue, RuleCondition condition ) {
            var metadataField = metadata.FirstOrDefault ( a => a.Name == condition.Field );
            if ( metadataField == null ) return false;

            switch ( condition.Operation ) {
                case EqualOperator: {
                    return metadataField.StringValue == conditionValue;
                }
                case StartWithOperator: {
                    return metadataField.StringValue?.StartsWith(conditionValue) ?? false;
                }
                case EndsWithOperator: {
                    return metadataField.StringValue?.EndsWith ( conditionValue ) ?? false;
                }
                case ContainsOperator: {
                    return metadataField.StringValue?.Contains ( conditionValue, StringComparison.InvariantCultureIgnoreCase ) ?? false;
                }
                default: throw new NotSupportedException ( $"CompareStrings operation {condition.Operation} not supported!" );
            }
        }

    }

}
