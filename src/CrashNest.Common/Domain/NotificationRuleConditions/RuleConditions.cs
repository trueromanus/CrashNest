namespace CrashNest.Common.Domain.NotificationRuleConditions {

    /// <summary>
    /// Rule conditions.
    /// </summary>
    public class RuleConditions {

        public IEnumerable<RuleCondition> Conditions { get; set; } = Enumerable.Empty<RuleCondition> ();

    }

}
