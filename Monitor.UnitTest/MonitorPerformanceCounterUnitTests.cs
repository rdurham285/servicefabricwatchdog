using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monitor.PerformanceCounter;

namespace Monitor.UnitTest
{
    [TestClass]
    public class MonitorPerformanceCounterUnitTests
    {
        [TestMethod]
        [TestCategory("Monitor.PerformanceCounter")]
        public void TestParsingOfPerformanceCounter()
        {
            string[] counters = {
                "Processor(_Total)\\% Processor Time|% Processor Time",
                "PhysicalDisk(_Total)\\% Disk Time|% Disk Time",
                "Memory\\% Committed Bytes In Use|% Committed Bytes In Use",
                ".NET CLR Exceptions(_Global_)\\# of Exceps Thrown / Sec|# of Exceps Thrown / Sec" };
            
            foreach (var counter in counters)
            {
                var pc = MonitorPerformanceCounter.Parse(counter);
                var val = pc.NextValue();
                pc.Dispose();
            }
        }
    }
}
