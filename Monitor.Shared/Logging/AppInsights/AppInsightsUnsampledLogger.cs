using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Monitor.Shared.Logging;
using Monitor.Shared.Logging.AppInsights;
using System;
using System.Collections.Generic;
using System.Fabric.Health;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Shared.Logging.AppInsights
{
    public sealed class AppInsightsUnsampledLogger : IDisposable, ILogger
    {
        private readonly TelemetryClient _client;

        public AppInsightsUnsampledLogger(string instrumentationKey)
        {
            var config = new TelemetryConfiguration(instrumentationKey);
            _client = new TelemetryClient(config);
            _client.InstrumentationKey = instrumentationKey;
        }

        /// <summary>
        /// Gets an indicator if the telemetry is enabled or not.
        /// </summary>
        public bool IsEnabled => this._client?.IsEnabled() ?? false;

        /// <summary>
        /// Calls AI to track the availability.
        /// </summary>
        /// <param name="applicationName">Application name.</param>
        /// <param name="instance">Instance identifier.</param>
        /// <param name="testName">Availability test name.</param>
        /// <param name="captured">The time when the availability was captured.</param>
        ///  <param name="location">The location of the test.</param>
        /// <param name="success">True if the availability test ran successfully.</param>
        /// <param name="message">Error message on availability test run failure.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        public Task ReportApplicationAvailabilityAsync(
            string applicationName,
            string testName,
            DateTimeOffset captured,
            string location,
            bool success,
            CancellationToken cancellationToken,
            string message = null)
        {
            if (IsEnabled)
            {
                AvailabilityTelemetry at = new AvailabilityTelemetry(testName, captured, TimeSpan.FromSeconds(0), location, success, message);

                //App insights has a set of "Canned" properties - using a property not in that list seems to not work
                at.Properties.Add("Service", applicationName);
                this._client.TrackAvailability(at);

                _client.TrackAvailability(at);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Calls AI to report health.
        /// </summary>
        /// <param name="applicationName">Application name.</param>
        /// <param name="serviceName">Service name.</param>
        /// <param name="instance">Instance identifier.</param>
        /// <param name="source">Name of the health source.</param>
        /// <param name="property">Name of the health property.</param>
        /// <param name="state">HealthState.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        public Task ReportServiceHealthAsync(
            string applicationName,
            string serviceName,
            string instance,
            string source,
            string property,
            HealthState state,
            CancellationToken cancellationToken)
        {
            if (this.IsEnabled)
            {
                SeverityLevel sev = (HealthState.Error == state)
                    ? SeverityLevel.Error
                    : (HealthState.Warning == state) ? SeverityLevel.Warning : SeverityLevel.Information;
                TraceTelemetry tt = new TraceTelemetry($"Health report: {source}:{property} is {Enum.GetName(typeof(HealthState), state)}", sev);
                tt.Context.Cloud.RoleName = serviceName;
                tt.Context.Cloud.RoleInstance = instance;
                this._client.TrackTrace(tt);
            }

            return Task.FromResult(0);
        }

        public Task ReportTraceAsync(string message, TraceSeverity severity, IDictionary<string, string> otherMetaData = null)
        {
            if (IsEnabled)
            {
                _client.TrackTrace(message, severity.ToAppInsightsSeverity(), otherMetaData);
            }

            return Task.FromResult(0);
        }

        public Task ReportMetricAsync(string metricName, double value, IDictionary<string,string> otherMetaData = null)
        {
            if (IsEnabled)
            {
                _client.TrackMetric(metricName, value, otherMetaData);
            }

            return Task.FromResult(0);
        }

        public Task ReportExceptionAsync(Exception e, IDictionary<string, string> otherMetaData = null)
        {
            if (IsEnabled)
            {
                _client.TrackException(e, otherMetaData);
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Flush();
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
