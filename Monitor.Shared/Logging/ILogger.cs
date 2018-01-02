using System;
using System.Collections.Generic;
using Monitor.Shared.Logging;
using System.Threading.Tasks;
using System.Threading;
using System.Fabric.Health;

namespace Monitor.Shared.Logging
{
    public interface ILogger: IDisposable
    {
        /// <summary>
        /// Calls telemetry provider to track the availability.
        /// </summary>
        /// <param name="applicationName">Application name.</param>
        /// <param name="instance">Instance identifier.</param>
        /// <param name="testName">Availability test name.</param>
        /// <param name="captured">The time when the availability was captured.</param>
        /// <param name="success">True if the availability test ran successfully.</param>
        /// <param name="message">Error message on availability test run failure.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        Task ReportApplicationAvailabilityAsync(
            string applicationName,
            string testName,
            DateTimeOffset captured,
            bool success,
            CancellationToken cancellationToken,
            string message = null);

        /// <summary>
        /// Calls telemetry provider to report health.
        /// </summary>
        /// <param name="applicationName">Application name.</param>
        /// <param name="serviceName">Service name.</param>
        /// <param name="instance">Instance identifier.</param>
        /// <param name="source">Name of the health source.</param>
        /// <param name="property">Name of the health property.</param>
        /// <param name="state">HealthState.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        Task ReportServiceHealthAsync(
            string applicationName,
            string serviceName,
            string instance,
            string source,
            string property,
            HealthState state,
            CancellationToken cancellationToken);

        /// <summary>
        /// Calls telemetry provider to report an exception
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="otherMetaData">Extra data</param>
        Task ReportExceptionAsync(Exception e, IDictionary<string, string> otherMetaData = null);

        /// <summary>
        /// Calls telemetry provider to report a metric
        /// </summary>
        /// <param name="metricName"></param>
        /// <param name="value"></param>
        /// <param name="otherMetaData"></param>
        Task ReportMetricAsync(string metricName, double value, IDictionary<string, string> otherMetaData = null);

        /// <summary>
        /// Calls telemetry provider to report a trace
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        /// <param name="otherMetaData"></param>
        Task ReportTraceAsync(string message, TraceSeverity severity, IDictionary<string, string> otherMetaData = null);
    }
}