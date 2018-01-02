using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.PerformanceCounter
{
    [DataContract]
    public class PerformanceCounterServiceConfiguration
    {
        [DataMember]
        public IEnumerable<PerformanceCounterConfiguration> PerformanceCounters { get; set; }
    }

    [DataContract]
    public class PerformanceCounterConfiguration
    {
        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string Instance { get; set; }

        [DataMember]
        public string Metric { get; set; }

        [DataMember]
        public string ReportingName { get; set; }
    }
}
