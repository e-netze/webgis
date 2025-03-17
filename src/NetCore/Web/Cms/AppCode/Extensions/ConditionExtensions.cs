using System;

namespace Cms.AppCode.Extensions;

static public class ConditionExtensions
{
    static public bool CheckCondition(this string condition, object instance)
    {
        if (String.IsNullOrEmpty(condition) || instance == null)
        {
            return true;
        }

        if (condition.Contains("="))
        {
            string propertyName = condition.Substring(0, condition.IndexOf("=")).Trim();
            string propertyValue = condition.Substring(condition.IndexOf("=") + 1).Trim();

            var propertyInfo = instance.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                return propertyValue.Equals(propertyInfo.GetValue(instance)?.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        return true;
    }
}
