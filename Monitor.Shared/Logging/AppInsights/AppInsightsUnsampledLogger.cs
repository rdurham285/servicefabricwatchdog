using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Monitor.Shared.Logging;
using Monitor.Shared.Logging.AppInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.Shared.Logging.AppInsights
{
    public sealed class AppInsightsUnsampledLogger : IDisposable, ILogger
    {
        private readonly TelemetryClient _client;

        public AppInsightsUnsampledLogger(string instrumentationKey)
        {
            //"71a8a0db-8073-4c7e-a3a2-8689bc401b7b"
            var config = new TelemetryConfiguration(instrumentationKey);
            _client = new TelemetryClient(config);
            _client.InstrumentationKey = instrumentationKey;
        }
        
        public void TrackTrace(string message, TraceSeverity severity, IDictionary<string, string> otherMetaData = null)
        {
            _client.TrackTrace(message, severity.ToAppInsightsSeverity(), otherMetaData);
        }

        public void TrackMetric(string metricName, double value, IDictionary<string,string> otherMetaData = null)
        {
            _client.TrackMetric(metricName, value, otherMetaData);
        }

        public void TrackException(Exception e, IDictionary<string, string> otherMetaData = null)
        {
            _client.TrackException(e, otherMetaData);
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
