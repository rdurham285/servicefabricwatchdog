using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.Shared.Logging.AppInsights
{
    // Define an extension method in a non-nested static class.
    public static class AppInsightsExtensions
    {
        public static SeverityLevel ToAppInsightsSeverity(this TraceSeverity severity)
        {
            switch (severity)
            {
                case TraceSeverity.Critical:
                    return SeverityLevel.Critical;
                case TraceSeverity.Error:
                    return SeverityLevel.Error;
                case TraceSeverity.Warning:
                    return SeverityLevel.Warning;
                case TraceSeverity.Informational:
                default:
                    return SeverityLevel.Information;
            }
        }
    }
}
