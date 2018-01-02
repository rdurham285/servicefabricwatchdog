using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("Monitor.UnitTest")]

namespace Monitor.EventLogSender
{
    internal struct RetrievedLog
    {
        public string Source;
        public DateTime? DateTime;
        public int EventId;
        public string Description;
    }


    /// <summary>
    /// Notifies the caller when an error hits the event log.
    /// </summary>
    /// <exception cref="EventLogReadingException"></exception>
    internal sealed class EventLogListener : IDisposable
    {
        private EventLogWatcher _watcher = null;

        public delegate void LogHandler(RetrievedLog log);

        public event LogHandler LogWritten;

        public EventLogListener()
        {
            // Subscribe to receive event notifications
            // in the Application log. The query specifies
            // that only level 1 events will be returned.
            EventLogQuery subscriptionQuery = new EventLogQuery(
                "Application", PathType.LogName, "*[System/Level=2]");

            _watcher = new EventLogWatcher(subscriptionQuery);

            // Set watcher to listen for the EventRecordWritten
            // event.  When this event happens, the callback method
            // (EventLogEventRead) will be called.
            _watcher.EventRecordWritten +=
                new EventHandler<EventRecordWrittenEventArgs>(
                    EventLogEventRead);

            // Begin subscribing to events the events
            _watcher.Enabled = true;
        }

        /// <summary>
        /// Callback method that gets executed when an event is
        /// reported to the subscription.
        /// </summary>
        private void EventLogEventRead(object obj,
            EventRecordWrittenEventArgs arg)
        {
            // Make sure there was no error reading the event.
            if (arg.EventRecord != null)
            {
                if(LogWritten != null)
                {
                    var log = new RetrievedLog
                    {
                        Source = arg.EventRecord.ProviderName,
                        DateTime = arg.EventRecord.TimeCreated,
                        EventId = arg.EventRecord.Id,
                        Description = arg.EventRecord.FormatDescription()
                    };

                    LogWritten(log);
                }
            }
            else
            {
                //"The event instance was null.";
            }
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
                disposed = true;
            else
                return;

            if (_watcher != null)
            {
                _watcher.Enabled = false;
                _watcher.Dispose();
            }
        }
    }
}
