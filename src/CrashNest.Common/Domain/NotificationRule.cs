using CrashNest.Common.Attributes;
using CrashNest.Common.Domain.NotificationRuleConditions;

namespace CrashNest.Common.Domain {

    [TableName ( nameof ( NotificationRule ) )]
    public class NotificationRule {

        public Guid Id { get; set; } = Guid.Empty;

        public string Name { get; set; } = "";

        public RuleConditions? Conditions { get; set; }

        public SendSettings? SendSettings { get; set; }

    }

}
