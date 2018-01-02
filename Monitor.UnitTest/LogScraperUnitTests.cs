using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monitor.EventLogSender;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.UnitTest
{
    [TestClass]
    public class LogSenderUnitTests
    {
        bool thrown = false;
        const string _UnitTestSource = "UnitTest";
        const string _Message = "TestMessage";

        [TestMethod]
        public void EventLogListenerTest_PositiveTest()
        {
            thrown = false;

            var evl = new EventLogListener();

            evl.LogWritten += Evl_LogWritten;

            EventLog.WriteEntry(_UnitTestSource, _Message, EventLogEntryType.Error);

            // Wait for events to occur. 
            System.Threading.Thread.Sleep(1000);

            Debug.Assert(thrown);
        }

        private void Evl_LogWritten(RetrievedLog log)
        {
            //Ensure we've got the event log we're looking for
            if(log.Source == _UnitTestSource && log.Description == _Message)
                thrown = true;
        }
        
        [TestMethod]
        public void EventLogListenerTest_NegativeTest()
        {
            thrown = false;
            var evl = new EventLogListener();

            evl.LogWritten += Evl_LogWritten;

            EventLog.WriteEntry(_UnitTestSource, "", EventLogEntryType.Information);

            Debug.Assert(!thrown);
        }
    }
}
