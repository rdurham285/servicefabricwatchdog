using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitor.PerformanceCounter
{
    /// <summary>
    /// Encapulation of configuration that generates the actual performance counter and a friendly name for it
    /// </summary>
    public class MonitorPerformanceCounter : IDisposable
    {
        private static Regex InstanceRegex = new Regex(@"\w*\(\w*\)");

        public readonly string FriendlyName;

        public MonitorPerformanceCounter(string Category, string Metric, string Instance = "", string ReportingName = null)
        {
            FriendlyName = string.IsNullOrWhiteSpace(ReportingName) ? Metric : ReportingName;

            _performanceCounter = new System.Diagnostics.PerformanceCounter(Category, Metric, Instance);
            _performanceCounter.NextValue(); // always grab one sample on creation to ensure validity
        }

        public MonitorPerformanceCounter(PerformanceCounterConfiguration configuration)
            : this(configuration.Category, configuration.Metric, configuration.Instance, configuration.ReportingName)
        { }

        public static MonitorPerformanceCounter Parse(string configurationString)
        {
            var configurationStringParts = configurationString.Split('|');

            var perfcounterString = configurationStringParts.First();
            var friendlyName = configurationStringParts.Last();
            if (InstanceRegex.IsMatch(perfcounterString))
            {
                var category = perfcounterString.Split('\\').First().Split('(').First();
                var instance = perfcounterString.Split('\\').First().Split('(').Last().TrimEnd(')');
                var metric = perfcounterString.Split('\\').Last();

                return new MonitorPerformanceCounter(category, metric, instance, friendlyName);
            }
            else
            {
                var category = perfcounterString.Split('\\').First();
                var metric = perfcounterString.Split('\\').Last();

                return new MonitorPerformanceCounter(category, metric, "", friendlyName);
            }
        }

        private System.Diagnostics.PerformanceCounter _performanceCounter;

        public float NextValue()
        {
            //Perform any transformations here
            return _performanceCounter.NextValue();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
                disposed = true;
            else
                return;

            if (_performanceCounter != null)
                _performanceCounter.Dispose();
        }
    }
}
