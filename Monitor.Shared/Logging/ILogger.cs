using System;
using System.Collections.Generic;
using Monitor.Shared.Logging;

namespace Monitor.Shared.Logging
{
    public interface ILogger: IDisposable
    {
        void TrackException(Exception e, IDictionary<string, string> otherMetaData = null);
        void TrackMetric(string metricName, double value, IDictionary<string, string> otherMetaData = null);
        void TrackTrace(string message, TraceSeverity severity, IDictionary<string, string> otherMetaData = null);
    }
}