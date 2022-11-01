namespace CrashNest.Common.Domain.NotificationRuleConditions {

    public class RuleCondition {

        public string Operation { get; set; } = "";

        public string? Field { get; set; }

        public object? Value { get; set; }

        public object? SecondValue { get; set; }

    }

}